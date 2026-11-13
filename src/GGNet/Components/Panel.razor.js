const ON_WHEEL = 'OnWheelAsync';
const ON_PAN_END = 'OnPanEndAsync';
const ON_CURSOR = 'OnCursorAsync';
const ON_CURSOR_LEAVE = 'OnCursorLeaveAsync';
const ON_CURSOR_RESTORE = 'OnCursorRestoreAsync';
const ON_NAVIGATE_END = 'OnNavigateEndAsync';
const TOOLTIP_OFFSET = 12;
const EDGE_MARGIN = 4;
// Screen pixels a pointer must travel before a press becomes a pan. Below it
// the press stays an ordinary tap or click: no capture, no preview, no commit.
const DRAG_THRESHOLD = { mouse: 4, pen: 6, touch: 10 };
const CLICK_SUPPRESSION_MS = 500;
// How long a released preview waits for its committed frame before giving up.
const SETTLE_TIMEOUT_MS = 1500;
// Distances, in screen pixels from the contact point, searched outward for the
// mark a touch tap inspects. The nearest ring with a hit wins.
// TODO: calibration - 24 px passed physical QA on a 10-inch 1200x1920 panel at
// device scale 1 (about 2.7 mm there); other densities and mark sizes are untested.
const INSPECT_RINGS = [0, 8, 16, 24];
// Svg units within which the committed crosshair counts as having reached a
// previewed position, so the preview can be dropped without a visible jump.
const CURSOR_SETTLE = 0.5;
const EDGE_SLACK = 0.5;
// A two-finger gesture that neither moved nor pinched more than this commits nothing.
const NAVIGATE_MIN_SHIFT = 0.5;
const NAVIGATE_MIN_SCALE = 0.01;
const MARK_SELECTOR = 'circle, ellipse, rect, path, line, polygon, polyline';

class PanelInteractivity {
    #capture;
    #target;
    #dotNetRef;
    #options;
    #abort;
    #pointerId = null;
    #dragging = false;
    #panOrigin = null;
    #panMatrix = null;
    #panInverse = null;
    #panStart = null;
    #panViewport = '';
    #panFrame = 0;
    #linked = [];
    #suppressClickUntil = 0;
    #settling = null;
    #pointerX = 0;
    #pointerY = 0;
    #pointerType = 'mouse';
    #tooltip = null;
    #tooltipStale = false;
    #inspectedIndex = -1;
    #engaged = false;
    #dispatching = false;
    #clickTarget = null;
    #cursorObserver = null;
    #cursorObserved = null;
    #touchClickBlockedUntil = 0;

    constructor(capture, target, dotNetRef, options) {
        this.#capture = capture;
        this.#target = target;
        this.#dotNetRef = dotNetRef;
        this.#options = options;
        this.#abort = new AbortController();
    }

    start() {
        const signal = this.#abort.signal;
        const options = { signal };
        const early = { signal, capture: true };

        this.#capture.addEventListener('wheel', async e => {
            const point = this.#point(e, this.#matrix()?.inverse);
            if (!point) {
                return;
            }

            e.preventDefault();

            try {
                await this.#dotNetRef.invokeMethodAsync(ON_WHEEL, point.x, point.y, e.deltaY);
            } catch {
                // Circuit disconnected.
            }
        }, { signal, passive: false });

        // Coordinates are tracked ahead of every other handler, and from the
        // events a stationary touch does deliver: a tap never sends pointermove.
        for (const type of ['pointerover', 'pointerdown', 'pointermove']) {
            this.#capture.addEventListener(type, e => this.#track(e), early);
        }

        this.#capture.addEventListener('pointermove', e => this.#move(e), options);

        if (this.#options.tooltip || this.#options.crosshair) {
            this.#startInspection(early);

            // A touch tooltip has no hover to end it: scrolling or pinching
            // would leave it floating over whatever moved underneath.
            const dismiss = () => {
                if (this.#pointerType !== 'mouse') {
                    this.#dismissTooltip();
                }
            };

            window.addEventListener('scroll', dismiss, { signal, capture: true, passive: true });
            window.visualViewport?.addEventListener('scroll', dismiss, options);
            window.visualViewport?.addEventListener('resize', dismiss, options);
        }

        if (this.#options.cursor) {
            this.#startCursor(options);
        }

        if (!this.#options.pan || (!this.#options.panX && !this.#options.panY)) {
            return;
        }

        this.#capture.addEventListener('pointerdown', e => {
            if (e.button !== 0 || !e.isPrimary) {
                return;
            }

            // Under a shared crosshair one finger scrubs; it never pans.
            if (e.pointerType === 'touch' && this.#options.cursor === 'shared') {
                return;
            }

            if (this.#pointerId !== null) {
                if (this.#dragging) {
                    return;
                }

                // A press whose release never arrived.
                this.#cancelPan();
            }

            this.#unsettle();

            const transform = this.#matrix();
            const point = this.#point(e, transform?.inverse);
            const panel = this.#capture.querySelector('rect.panel');
            if (!point || !panel) {
                return;
            }

            const area = panel.getBBox();
            if (point.x < area.x || point.x > area.x + area.width
                || point.y < area.y || point.y > area.y + area.height) {
                return;
            }

            // Stops a mouse or pen drag from selecting text. A touch keeps its
            // default so the compatibility mouse events behind tap-to-hover
            // (tooltip, crosshair) still arrive; touch-action owns scrolling.
            if (e.pointerType !== 'touch') {
                e.preventDefault();

                // Preventing the default also prevents focus, and the
                // crosshair's keys are read from the focused plot.
                if (this.#options.cursor) {
                    this.#capture.closest('[tabindex]')?.focus({ preventScroll: true });
                }
            }

            this.#pointerId = e.pointerId;
            this.#dragging = false;
            this.#panOrigin = { x: e.clientX, y: e.clientY };
            this.#panMatrix = transform.matrix;
            this.#panInverse = transform.inverse;
            this.#panStart = point;
            this.#panViewport = this.#viewport();
            this.#watchTransform();
        }, options);

        // The release is observed on the window: before the threshold the
        // pointer is not captured, so it can end outside the capture element.
        window.addEventListener('pointerup', async e => {
            if (e.pointerId !== this.#pointerId) {
                return;
            }

            const dragging = this.#dragging;
            const delta = dragging && this.#held() ? this.#delta(e) : null;
            const commit = delta && (delta.dx !== 0 || delta.dy !== 0);
            const linked = this.#linked;
            this.#cancelPan(commit);
            if (!dragging) {
                return;
            }

            this.#suppressClickUntil = performance.now() + CLICK_SUPPRESSION_MS;
            if (!commit) {
                return;
            }

            this.#settle(delta);
            for (const link of linked) {
                link.panel.#settle({ dx: delta.dx * link.sx, dy: delta.dy * link.sy });
            }

            try {
                await this.#dotNetRef.invokeMethodAsync(ON_PAN_END, delta.dx, delta.dy);
            } catch {
                // Circuit disconnected: no committed frame will arrive.
                this.#unsettle();
                for (const link of linked) {
                    link.panel.#unsettle();
                }
            }
        }, early);

        window.addEventListener('pointercancel', e => {
            if (e.pointerId === this.#pointerId) {
                this.#cancelPan();
            }
        }, early);

        // Taking capture mid-gesture releases a touch's implicit capture on
        // the pressed mark; only losing our own capture ends the pan.
        this.#capture.addEventListener('lostpointercapture', e => {
            if (e.target === this.#capture && e.pointerId === this.#pointerId) {
                this.#cancelPan();
            }
        }, options);

        // The click that trails a recognized drag belongs to the pan: it must
        // reach neither a mark nor an ancestor handler.
        window.addEventListener('click', e => {
            if (performance.now() >= this.#suppressClickUntil) {
                return;
            }

            this.#suppressClickUntil = 0;
            if (e.target instanceof Node && (this.#capture.contains(e.target) || e.target.contains(this.#capture))) {
                e.stopPropagation();
                e.preventDefault();
            }
        }, early);
    }

    // Blazor can replace the marks group when a re-render changes the grid
    // around it; the capture group survives, so the target is found again.
    get #marks() {
        if (!this.#target.isConnected) {
            this.#target = this.#capture.querySelector('[data-ggnet-pan]') ?? this.#target;
        }

        return this.#target;
    }

    #track(e) {
        if (!e.isPrimary || (this.#pointerId !== null && e.pointerId !== this.#pointerId)) {
            return;
        }

        this.#pointerX = e.clientX;
        this.#pointerY = e.clientY;
        this.#pointerType = e.pointerType;

        if (e.type === 'pointerdown') {
            this.#suppressClickUntil = 0;
        }

        if (e.type === 'pointerdown' || e.pointerType === 'mouse') {
            this.#tooltipStale = false;
        }
    }

    #move(e) {
        if (this.#pointerId === null) {
            this.#follow();
            return;
        }

        if (e.pointerId !== this.#pointerId) {
            return;
        }

        if (!this.#dragging) {
            if (e.pointerType === 'mouse' && e.buttons === 0) {
                this.#cancelPan();
                return;
            }

            const threshold = DRAG_THRESHOLD[e.pointerType] ?? DRAG_THRESHOLD.touch;
            if (Math.hypot(e.clientX - this.#panOrigin.x, e.clientY - this.#panOrigin.y) < threshold) {
                this.#follow();
                return;
            }

            try {
                this.#capture.setPointerCapture(e.pointerId);
            } catch {
                this.#cancelPan();
                return;
            }

            this.#dragging = true;
            this.#link();
            if (e.pointerType !== 'mouse') {
                this.#dismissTooltip();
            }
        } else if (!this.#held()) {
            this.#cancelPan();
            return;
        }

        const delta = this.#delta(e);
        if (delta) {
            this.#marks.setAttribute('transform', `translate(${delta.dx}, ${delta.dy})`);
            for (const link of this.#linked) {
                link.panel.#marks.setAttribute('transform', `translate(${delta.dx * link.sx}, ${delta.dy * link.sy})`);
            }
        }
    }

    // The other panels of the same plot that share a panned axis preview the
    // drag too, scaled to their own plot area: the committed window moves
    // them all, so a preview on the dragged panel alone makes the rest jump
    // at release. A panel with its own scale on that axis does not follow.
    #link() {
        this.#linked = [];

        const group = this.#options.group;
        const own = this.#area();
        if (!group || !own) {
            return;
        }

        for (const panel of panels.values()) {
            if (panel === this || panel.#options.group !== group || panel.#pointerId !== null) {
                continue;
            }

            const area = panel.#area();
            if (!area || !panel.#marks.isConnected) {
                continue;
            }

            const sx = this.#options.panX && this.#options.sharedX && own.width > 0 ? area.width / own.width : 0;
            const sy = this.#options.panY && this.#options.sharedY && own.height > 0 ? area.height / own.height : 0;
            if (sx === 0 && sy === 0) {
                continue;
            }

            panel.#unsettle();
            this.#linked.push({ panel, sx, sy });
        }
    }

    #area() {
        return this.#capture.querySelector('rect.panel')?.getBBox() ?? null;
    }

    // A capture released before the browser activated it fires no
    // lostpointercapture, so ownership is also checked directly.
    #held() {
        return this.#pointerId !== null && this.#capture.hasPointerCapture(this.#pointerId);
    }

    #follow() {
        if (this.#options.tooltip && !this.#tooltipStale && this.#tooltip?.isConnected) {
            this.#place(this.#tooltip);
        }
    }

    showTooltip(element) {
        this.#tooltip = element;

        // A re-render after a touch pan or scroll must not resurrect the
        // tooltip at the release point; the next press shows it again.
        if (this.#tooltipStale) {
            this.#hide(element);
            return;
        }

        if (!element.matches(':popover-open')) {
            try {
                element.showPopover();
            } catch {
                // Already open, or the Popover API is unavailable.
            }
        }

        this.#place(element);
    }

    #dismissTooltip() {
        this.#tooltipStale = true;

        if (this.#tooltip?.isConnected) {
            this.#hide(this.#tooltip);
        }

        this.#release();
    }

    // Touch inspection replaces the browser's emulated hover inside this
    // panel: a tap selects the nearest mark within reach, and the tooltip and
    // crosshair are driven from that one choice. Mouse and pen keep native
    // hover; clicks keep native targeting.
    #startInspection(early) {
        const touches = new Set();
        let tap = null;

        window.addEventListener('pointerdown', e => {
            if (e.pointerType !== 'touch') {
                return;
            }

            touches.add(e.pointerId);
            tap = touches.size === 1 && e.isPrimary
                ? { id: e.pointerId, x: e.clientX, y: e.clientY, inside: e.target instanceof Node && this.#capture.contains(e.target) }
                : null;
        }, early);

        window.addEventListener('pointermove', e => {
            if (tap && e.pointerId === tap.id
                && Math.hypot(e.clientX - tap.x, e.clientY - tap.y) >= DRAG_THRESHOLD.touch) {
                tap = null;
            }
        }, early);

        for (const type of ['pointerup', 'pointercancel']) {
            window.addEventListener(type, e => {
                if (e.pointerType !== 'touch') {
                    return;
                }

                touches.delete(e.pointerId);
                const ended = tap;
                tap = null;
                if (type === 'pointercancel' || ended?.id !== e.pointerId) {
                    return;
                }

                if (ended.inside && this.#inPanel(ended.x, ended.y)) {
                    const mark = this.#inspect(ended.x, ended.y);
                    this.#clickTarget = mark && this.#options.cursor !== 'shared'
                        ? { mark, x: ended.x, y: ended.y, until: performance.now() + CLICK_SUPPRESSION_MS }
                        : null;
                } else {
                    this.#release();
                }
            }, early);
        }

        // The click a tap produces follows the inspected mark, so a tap that
        // shows a mark's tooltip also reaches that mark's click handler, once,
        // and bubbles to ancestors from there.
        window.addEventListener('click', e => {
            const pending = this.#clickTarget;
            this.#clickTarget = null;

            if (!pending || performance.now() >= pending.until
                || !(e.pointerType === 'touch' || this.#fromTouch(e))
                || !(e.target instanceof Node) || !this.#capture.contains(e.target)
                || pending.mark.contains(e.target) || !pending.mark.isConnected) {
                return;
            }

            e.stopPropagation();
            e.preventDefault();
            pending.mark.dispatchEvent(new MouseEvent('click', {
                bubbles: true,
                cancelable: true,
                composed: true,
                view: window,
                detail: 1,
                clientX: pending.x,
                clientY: pending.y
            }));
        }, early);

        // The compatibility hover a touch produces would contradict the
        // inspected mark; it never reaches the panel's handlers.
        for (const type of ['mouseover', 'mouseout', 'mouseenter', 'mouseleave']) {
            window.addEventListener(type, e => {
                if (!this.#dispatching && this.#fromTouch(e)
                    && e.target instanceof Node && this.#capture.contains(e.target)) {
                    e.stopPropagation();
                }
            }, early);
        }
    }

    #fromTouch(e) {
        return e.sourceCapabilities ? e.sourceCapabilities.firesTouchEvents : this.#pointerType === 'touch';
    }

    #inPanel(x, y) {
        const inverse = this.#matrix()?.inverse;
        const panel = this.#capture.querySelector('rect.panel');
        if (!inverse || !panel) {
            return false;
        }

        // A mark centred on the edge of the plot area is half outside it; the
        // contact that hits its centre must still count as inside.
        const point = new DOMPoint(x, y).matrixTransform(inverse);
        const area = panel.getBBox();
        return point.x >= area.x - EDGE_SLACK && point.x <= area.x + area.width + EDGE_SLACK
            && point.y >= area.y - EDGE_SLACK && point.y <= area.y + area.height + EDGE_SLACK;
    }

    #inspect(x, y) {
        this.#tooltipStale = false;

        const mark = this.#pick(x, y);
        const previous = this.#inspected();

        if (previous && previous !== mark) {
            this.#dispatch(previous, 'mouseout', x, y, mark);
        }

        this.#inspectedIndex = mark ? this.#markElements().indexOf(mark) : -1;
        this.#engaged = true;

        if (mark) {
            this.#dispatch(mark, 'mouseover', x, y, previous);
        }

        return mark;
    }

    #release() {
        if (!this.#engaged) {
            return;
        }

        this.#engaged = false;

        const mark = this.#inspected();
        this.#inspectedIndex = -1;

        if (mark) {
            this.#dispatch(mark, 'mouseout', this.#pointerX, this.#pointerY);
        }
    }

    #markElements() {
        return [...this.#marks.querySelectorAll(MARK_SELECTOR)];
    }

    // Re-rendering can replace mark elements; the inspected one is held by
    // position so its release still reaches the live element.
    #inspected() {
        return this.#inspectedIndex < 0 ? null : this.#markElements()[this.#inspectedIndex] ?? null;
    }

    #pick(x, y) {
        const marks = this.#marks;

        for (const radius of INSPECT_RINGS) {
            const found = new Set();
            const samples = radius === 0 ? 1 : 8;

            for (let i = 0; i < samples; i++) {
                const angle = 2 * Math.PI * i / samples;
                const topmost = document.elementsFromPoint(x + radius * Math.cos(angle), y + radius * Math.sin(angle))
                    .find(element => element !== marks && marks.contains(element) && element.matches(MARK_SELECTOR));

                if (topmost) {
                    found.add(topmost);
                }
            }

            if (found.size > 0) {
                const distance = element => {
                    const box = element.getBoundingClientRect();
                    return Math.hypot(box.x + box.width / 2 - x, box.y + box.height / 2 - y);
                };

                return [...found].reduce((best, element) => distance(element) < distance(best) ? element : best);
            }
        }

        return null;
    }

    // The crosshair. The server owns its position (a data value, drawn in
    // every panel that shares the x scale) and renders the line, its label
    // and the sample markers. This module only keeps the line under the
    // pointer between two server frames, by translating the cursor group, and
    // asks for readings latest-wins: one call in flight per plot, the newest
    // position replacing any that is still waiting.
    #startCursor(options) {
        this.#capture.addEventListener('pointermove', e => {
            if (e.pointerType !== 'touch' && e.buttons === 0 && this.#pointerId === null) {
                this.#hoverCursor(e);
            }
        }, options);

        this.#capture.addEventListener('pointerleave', e => {
            if (e.pointerType !== 'touch') {
                this.#leaveCursor();
            }
        }, options);

        // Click handling stays on the window: a click listener on the chart
        // itself makes Chromium's touch adjustment treat the chart as a
        // target and move taps that land near its edge. A click that trails
        // a drag belongs to the pan and is skipped. A touch tap arrives as
        // its click; under a shared crosshair
        // that tap is inspection input and goes no further, to a mark's
        // handler or to an ancestor's.
        const early = { signal: options.signal, capture: true };
        const shared = this.#options.cursor === 'shared';

        window.addEventListener('click', e => {
            if (e.defaultPrevented || performance.now() < this.#suppressClickUntil
                || !(e.target instanceof Node) || !this.#capture.contains(e.target)) {
                return;
            }

            if (shared && (e.pointerType === 'touch' || this.#fromTouch(e))) {
                e.stopPropagation();
                if (performance.now() < this.#touchClickBlockedUntil) {
                    return;
                }
            }

            this.#pinCursor(e);
        }, early);

        if (!shared) {
            return;
        }

        // Two quick taps are two selections, not a reset.
        window.addEventListener('dblclick', e => {
            if (this.#fromTouch(e) && e.target instanceof Node && this.#capture.contains(e.target)) {
                e.stopPropagation();
            }
        }, early);

        this.#startScrub(early);
    }

    // One finger moving sideways scrubs the crosshair and pins it on release.
    // A vertical start belongs to the page, which scrolls and cancels the
    // pointer. A second finger abandons the scrub and puts the previous pin
    // back; nothing new starts until every finger has lifted.
    #startScrub(early) {
        const touches = new Map();
        let scrub = null;
        let crowded = false;
        let navigation = null;

        const inPlot = target => target instanceof Node && this.#cursorPanels().some(panel => panel.#capture.contains(target));

        window.addEventListener('pointerdown', e => {
            if (e.pointerType !== 'touch') {
                return;
            }

            touches.set(e.pointerId, { x: e.clientX, y: e.clientY, own: e.target instanceof Node && this.#capture.contains(e.target), plot: inPlot(e.target) });
            if (touches.size > 1) {
                crowded = true;
                const abandoned = scrub;
                scrub = null;
                this.#abandonScrub(abandoned);

                // The panel under the first finger owns the gesture; the
                // second finger may rest on any panel of the same plot.
                const [first, second] = [...touches.entries()];
                if (touches.size === 2 && this.#options.twoFingerX && first[1].own && second[1].plot) {
                    navigation = this.#beginNavigation(first, second);
                }

                return;
            }

            // The block on touch clicks belongs to the gesture that just
            // ended; a new contact is a new gesture and may be a tap.
            if (!crowded) {
                this.#touchClickBlockedUntil = 0;
            }

            if (crowded || !e.isPrimary || !(e.target instanceof Node) || !this.#capture.contains(e.target)
                || !this.#cursorPoint(e)) {
                return;
            }

            scrub = { id: e.pointerId, x: e.clientX, y: e.clientY, active: false, last: null, previous: this.#cursorSnapshot() };
        }, early);

        window.addEventListener('pointermove', e => {
            const touch = touches.get(e.pointerId);
            if (touch && e.pointerType === 'touch') {
                touch.x = e.clientX;
                touch.y = e.clientY;
            }

            if (navigation && (e.pointerId === navigation.a || e.pointerId === navigation.b)) {
                this.#moveNavigation(navigation, touches);
                return;
            }

            if (!scrub || e.pointerId !== scrub.id) {
                return;
            }

            if (!scrub.active) {
                const dx = e.clientX - scrub.x;
                const dy = e.clientY - scrub.y;
                if (Math.hypot(dx, dy) < DRAG_THRESHOLD.touch) {
                    return;
                }

                if (Math.abs(dx) < Math.abs(dy)) {
                    scrub = null;
                    return;
                }

                scrub.active = true;
                this.#dismissTooltip();
                this.#release();
            }

            const at = this.#cursorPoint(e, true);
            if (at) {
                scrub.last = at;
                this.#scrubCursor(at);
            }
        }, early);

        for (const type of ['pointerup', 'pointercancel']) {
            window.addEventListener(type, e => {
                if (e.pointerType !== 'touch') {
                    return;
                }

                if (navigation && (e.pointerId === navigation.a || e.pointerId === navigation.b)) {
                    const ended = navigation;
                    navigation = null;
                    this.#endNavigation(ended, type === 'pointerup');
                }

                touches.delete(e.pointerId);
                if (touches.size === 0 && crowded) {
                    crowded = false;
                    this.#touchClickBlockedUntil = performance.now() + CLICK_SUPPRESSION_MS;
                }

                if (!scrub || e.pointerId !== scrub.id) {
                    return;
                }

                const ended = scrub;
                scrub = null;
                if (!ended.active) {
                    return;
                }

                this.#touchClickBlockedUntil = performance.now() + CLICK_SUPPRESSION_MS;
                const at = this.#cursorPoint(e, true) ?? ended.last;
                if (type === 'pointercancel' || !at) {
                    this.#abandonScrub(ended);
                    return;
                }

                this.#holdCursor(at, true);
            }, early);
        }
    }

    // Two fingers pan and pinch the x window. Like the one-finger pan of a
    // plot without a crosshair, the gesture is previewed as a transform on the
    // marks of every panel sharing the x scale and committed once, when the
    // first finger lifts. The pinned line is a data value: it rides along.
    #beginNavigation([a, first], [b, second]) {
        const transform = this.#matrix();
        const area = this.#area();
        if (!transform || !area || area.width <= 0) {
            return null;
        }

        const p = new DOMPoint(first.x, first.y).matrixTransform(transform.inverse);
        const q = new DOMPoint(second.x, second.y).matrixTransform(transform.inverse);
        const distance = Math.hypot(p.x - q.x, p.y - q.y);
        if (!(distance > 0)) {
            return null;
        }

        this.#dismissTooltip();
        this.#release();
        for (const panel of this.#cursorPanels()) {
            panel.#unsettle();
        }

        return { a, b, inverse: transform.inverse, area, from: (p.x + q.x) / 2, to: (p.x + q.x) / 2, distance, scale: 1 };
    }

    #moveNavigation(navigation, touches) {
        const first = touches.get(navigation.a);
        const second = touches.get(navigation.b);
        if (!first || !second) {
            return;
        }

        const p = new DOMPoint(first.x, first.y).matrixTransform(navigation.inverse);
        const q = new DOMPoint(second.x, second.y).matrixTransform(navigation.inverse);
        navigation.to = (p.x + q.x) / 2;
        navigation.scale = Math.hypot(p.x - q.x, p.y - q.y) / navigation.distance;
        if (!Number.isFinite(navigation.to) || !(navigation.scale > 0)) {
            return;
        }

        const state = this.#cursorState();
        state.navigation = this.#navigationFractions(navigation);
        for (const panel of this.#cursorPanels()) {
            const transform = panel.#navigationTransform(state.navigation);
            if (transform) {
                panel.#marks.setAttribute('transform', transform);
            }

            panel.#placeCursor(state);
        }
    }

    #navigationFractions(navigation) {
        return {
            from: (navigation.from - navigation.area.x) / navigation.area.width,
            to: (navigation.to - navigation.area.x) / navigation.area.width,
            scale: navigation.scale,
            released: false
        };
    }

    #navigationTransform(fractions) {
        const area = this.#area();
        if (!area) {
            return null;
        }

        const from = area.x + fractions.from * area.width;
        const to = area.x + fractions.to * area.width;
        return `translate(${to - fractions.scale * from}, 0) scale(${fractions.scale}, 1)`;
    }

    async #endNavigation(navigation, commit) {
        const state = this.#cursorState();
        const panels = this.#cursorPanels();
        const moved = Math.abs(navigation.to - navigation.from) >= NAVIGATE_MIN_SHIFT
            || Math.abs(navigation.scale - 1) >= NAVIGATE_MIN_SCALE;

        if (!commit || !moved || !state.navigation) {
            state.navigation = null;
            for (const panel of panels) {
                panel.#marks.removeAttribute('transform');
                panel.#placeCursor(state);
            }

            return;
        }

        // Held until each panel's committed frame arrives, as a pan is.
        const fractions = state.navigation;
        fractions.released = true;
        for (const panel of panels) {
            const transform = panel.#navigationTransform(fractions);
            if (transform) {
                panel.#settleWith(transform);
            }
        }

        clearTimeout(state.navigationTimer);
        state.navigationTimer = setTimeout(() => {
            if (state.navigation === fractions) {
                state.navigation = null;
                this.#applyCursor();
            }
        }, SETTLE_TIMEOUT_MS);

        try {
            await this.#dotNetRef.invokeMethodAsync(ON_NAVIGATE_END, navigation.from, navigation.to, navigation.scale);
        } catch {
            // Circuit disconnected: no committed frame will arrive.
            state.navigation = null;
            for (const panel of panels) {
                panel.#unsettle();
                panel.#placeCursor(state);
            }
        }
    }

    #cursorSnapshot() {
        const element = this.#cursorElement();
        const line = element?.querySelector('line');
        const area = this.#area();
        if (!line || !area || area.width <= 0 || element.getAttribute('visibility') === 'hidden'
            || !element.hasAttribute('data-ggnet-pinned')) {
            return null;
        }

        return { fraction: (Number(line.getAttribute('x1')) - area.x) / area.width };
    }

    #scrubCursor(at) {
        const state = this.#cursorState();
        clearTimeout(state.timer);
        state.timer = 0;
        state.hovering = false;
        state.concealed = false;
        state.settle = false;
        state.pinned = false;
        state.live = { fraction: at.fraction, panel: this };
        this.#applyCursor();
        this.#requestCursor({ x: at.x, pinned: false });
    }

    // The server puts back the pinned value it holds, so an abandoned scrub
    // costs the pin no precision. Until that frame arrives the line is held
    // where the pin was, or hidden when there was none.
    #abandonScrub(scrub) {
        if (!scrub?.active) {
            return;
        }

        const state = this.#cursorState();
        clearTimeout(state.timer);
        state.timer = setTimeout(() => this.#dropCursor(state), SETTLE_TIMEOUT_MS);
        state.hovering = false;
        if (scrub.previous) {
            state.concealed = false;
            state.pinned = true;
            state.settle = true;
            state.live = { fraction: scrub.previous.fraction, panel: this };
        } else {
            state.concealed = true;
            state.pinned = null;
            state.settle = false;
            state.live = null;
        }

        this.#applyCursor();
        this.#requestCursor({ restore: true });
    }

    #cursorKey() {
        return this.#options.cursor === 'shared' && this.#options.group ? this.#options.group : this;
    }

    #cursorState() {
        const key = this.#cursorKey();
        let state = cursors.get(key);
        if (!state) {
            state = { live: null, settle: false, pinned: null, hovering: false, concealed: false, timer: 0, inFlight: false, pending: null, sent: 0 };
            cursors.set(key, state);
        }

        return state;
    }

    #cursorPanels() {
        const key = this.#cursorKey();
        return key === this ? [this] : [...panels.values()].filter(panel => panel.#options.cursor && panel.#cursorKey() === key);
    }

    #cursorElement() {
        return this.#capture.querySelector('[data-ggnet-cursor]');
    }

    #cursorPinned() {
        return this.#cursorState().pinned ?? this.#cursorElement()?.hasAttribute('data-ggnet-pinned') ?? false;
    }

    // Clamped, a point outside the plot area reads as its nearest edge: a
    // finger that scrubs past the side keeps the line at that side.
    #cursorPoint(e, clamp = false) {
        const point = this.#point(e, this.#matrix()?.inverse);
        const area = this.#area();
        if (!point || !area || area.width <= 0) {
            return null;
        }

        if (!clamp && (point.x < area.x || point.x > area.x + area.width
            || point.y < area.y || point.y > area.y + area.height)) {
            return null;
        }

        const x = Math.min(Math.max(point.x, area.x), area.x + area.width);
        return { x, fraction: (x - area.x) / area.width };
    }

    #hoverCursor(e) {
        if (this.#cursorPinned()) {
            return;
        }

        const at = this.#cursorPoint(e);
        if (!at) {
            this.#leaveCursor();
            return;
        }

        const state = this.#cursorState();
        state.hovering = true;
        state.concealed = false;
        state.settle = false;
        state.live = { fraction: at.fraction, panel: this };
        this.#applyCursor();
        this.#requestCursor({ x: at.x, pinned: false });
    }

    #leaveCursor() {
        const state = this.#cursorState();
        if (!state.hovering || this.#cursorPinned()) {
            return;
        }

        state.hovering = false;
        state.live = null;
        state.concealed = true;
        clearTimeout(state.timer);
        state.timer = setTimeout(() => this.#dropCursor(state), SETTLE_TIMEOUT_MS);
        this.#applyCursor();
        this.#requestCursor({ leave: true });
    }

    #pinCursor(e) {
        const at = this.#cursorPoint(e);
        if (!at) {
            return;
        }

        // A panel with its own x scale shares no position to pin.
        if (this.#options.cursor !== 'shared') {
            const state = this.#cursorState();
            state.hovering = true;
            this.#requestCursor({ x: at.x, pinned: false });
            return;
        }

        this.#holdCursor(at, true);
    }

    // The preview stays until the committed line reaches it, so the line
    // never falls back to its previous position for the round trip.
    #holdCursor(at, pinned) {
        const state = this.#cursorState();
        state.hovering = false;
        state.concealed = false;
        state.pinned = pinned;
        state.settle = true;
        state.live = { fraction: at.fraction, panel: this };
        clearTimeout(state.timer);
        state.timer = setTimeout(() => this.#dropCursor(state), SETTLE_TIMEOUT_MS);
        this.#applyCursor();
        this.#requestCursor({ x: at.x, pinned });
    }

    #dropCursor(state) {
        clearTimeout(state.timer);
        state.timer = 0;
        state.live = null;
        state.settle = false;
        state.pinned = null;
        state.concealed = false;
        this.#applyCursor();
    }

    #applyCursor() {
        const state = this.#cursorState();
        for (const panel of this.#cursorPanels()) {
            panel.#placeCursor(state);
        }
    }

    #placeCursor(state) {
        const element = this.#cursorElement();
        if (!element) {
            return;
        }

        this.#observeCursor(element);

        const line = element.querySelector('line');
        const area = this.#area();
        const markers = element.parentElement?.querySelector('[data-ggnet-cursor-markers]');
        if (markers) {
            markers.style.visibility = state.navigation ? 'hidden' : '';
        }

        // During a two-finger gesture the committed line moves with the marks.
        if (state.navigation && line && area && area.width > 0) {
            const base = Number(line.getAttribute('x1'));
            const fraction = state.navigation.to + state.navigation.scale * ((base - area.x) / area.width - state.navigation.from);
            element.setAttribute('transform', `translate(${area.x + fraction * area.width - base}, 0)`);
            element.style.visibility = fraction < 0 || fraction > 1 ? 'hidden' : '';
            return;
        }

        if (!state.live || !line || !area || !this.#options.cursorContinuous) {
            element.removeAttribute('transform');
            element.style.visibility = state.concealed ? 'hidden' : '';
            return;
        }

        const target = area.x + state.live.fraction * area.width;
        element.setAttribute('transform', `translate(${target - Number(line.getAttribute('x1'))}, 0)`);
        element.style.visibility = 'visible';
    }

    // A committed frame rewrites the line; the preview is then measured
    // against the new position, in every panel of the plot.
    #observeCursor(element) {
        if (this.#cursorObserved === element) {
            return;
        }

        this.#cursorObserver?.disconnect();
        this.#cursorObserved = element;
        this.#cursorObserver = new MutationObserver(records => {
            if (records.every(record => record.target === element
                && (record.attributeName === 'transform' || record.attributeName === 'style'))) {
                return;
            }

            const state = this.#cursorState();
            if (element.getAttribute('visibility') === 'hidden') {
                state.concealed = false;
            }

            if (state.navigation?.released) {
                state.navigation = null;
                this.#applyCursor();
                return;
            }

            if (state.settle && state.live?.panel === this && this.#cursorSettled(element, state)) {
                this.#dropCursor(state);
                return;
            }

            this.#placeCursor(state);
        });

        this.#cursorObserver.observe(element, { subtree: true, attributes: true, childList: true, characterData: true });
    }

    #cursorSettled(element, state) {
        const line = element.querySelector('line');
        const area = this.#area();
        if (!line || !area || element.getAttribute('visibility') === 'hidden') {
            return false;
        }

        if (!this.#options.cursorContinuous) {
            return true;
        }

        return Math.abs(area.x + state.live.fraction * area.width - Number(line.getAttribute('x1'))) <= CURSOR_SETTLE
            && element.hasAttribute('data-ggnet-pinned') === (state.pinned ?? false);
    }

    #requestCursor(message) {
        const state = this.#cursorState();
        state.pending = { panel: this, ...message };
        pumpCursor(state);
    }

    #sendCursor(message) {
        if (message.leave) {
            return this.#dotNetRef.invokeMethodAsync(ON_CURSOR_LEAVE);
        }

        if (message.restore) {
            return this.#dotNetRef.invokeMethodAsync(ON_CURSOR_RESTORE);
        }

        return this.#dotNetRef.invokeMethodAsync(ON_CURSOR, message.x, message.pinned);
    }

    static send(message) {
        return message.panel.#sendCursor(message);
    }

    #disposeCursor() {
        this.#cursorObserver?.disconnect();
        this.#cursorObserver = null;
        this.#cursorObserved = null;

        const key = this.#cursorKey();
        const state = cursors.get(key);
        if (state && this.#cursorPanels().every(panel => panel === this)) {
            clearTimeout(state.timer);
            cursors.delete(key);
        }
    }

    #dispatch(element, type, x, y, related = null) {
        this.#dispatching = true;

        try {
            element.dispatchEvent(new MouseEvent(type, {
                bubbles: type === 'mouseover' || type === 'mouseout',
                cancelable: true,
                composed: true,
                view: window,
                clientX: x,
                clientY: y,
                relatedTarget: related
            }));
        } finally {
            this.#dispatching = false;
        }
    }

    #hide(element) {
        try {
            if (element.matches(':popover-open')) {
                element.hidePopover();
            }
        } catch {
            // The Popover API is unavailable.
        }
    }

    #place(element) {
        const offset = Number(element.dataset.offset) || TOOLTIP_OFFSET;
        const bubble = element.querySelector('.bubble') ?? element;
        const box = bubble.getBoundingClientRect();

        let x = this.#pointerX + offset;
        let y = this.#pointerY + offset;

        if (x + box.width > window.innerWidth - EDGE_MARGIN) {
            x = this.#pointerX - offset - box.width;
        }

        if (y + box.height > window.innerHeight - EDGE_MARGIN) {
            y = this.#pointerY - offset - box.height;
        }

        element.style.left = `${Math.max(EDGE_MARGIN, x)}px`;
        element.style.top = `${Math.max(EDGE_MARGIN, y)}px`;
    }

    #matrix() {
        if (!this.#capture.isConnected || !this.#marks.isConnected) {
            return null;
        }

        const matrix = this.#capture.getScreenCTM();
        if (!matrix || !this.#finiteMatrix(matrix)) {
            return null;
        }

        const inverse = new DOMMatrix([matrix.a, matrix.b, matrix.c, matrix.d, matrix.e, matrix.f]).inverse();
        return this.#finiteMatrix(inverse) ? { matrix, inverse } : null;
    }

    #finiteMatrix(matrix) {
        return [matrix.a, matrix.b, matrix.c, matrix.d, matrix.e, matrix.f].every(Number.isFinite);
    }

    #point(e, inverse) {
        if (!inverse) {
            return null;
        }

        const point = new DOMPoint(e.clientX, e.clientY).matrixTransform(inverse);
        return Number.isFinite(point.x) && Number.isFinite(point.y) ? point : null;
    }

    #sameTransform() {
        const current = this.#matrix()?.matrix;
        return current && this.#panMatrix
            && ['a', 'b', 'c', 'd', 'e', 'f'].every(key => current[key] === this.#panMatrix[key])
            && this.#panViewport === this.#viewport();
    }

    // A native pinch rescales the visual viewport without touching the screen
    // CTM and without cancelling a pointer the page already owns.
    #viewport() {
        const viewport = window.visualViewport;
        return viewport ? `${viewport.scale} ${viewport.offsetLeft} ${viewport.offsetTop}` : '';
    }

    #watchTransform() {
        this.#panFrame = requestAnimationFrame(() => {
            if (this.#pointerId === null) {
                return;
            }

            if (!this.#sameTransform()) {
                this.#cancelPan();
                return;
            }

            this.#watchTransform();
        });
    }

    #delta(e) {
        if (!this.#sameTransform()) {
            this.#cancelPan();
            return null;
        }

        const point = this.#point(e, this.#panInverse);
        if (!point) {
            this.#cancelPan();
            return null;
        }

        return {
            dx: this.#options.panX ? point.x - this.#panStart.x : 0,
            dy: this.#options.panY ? point.y - this.#panStart.y : 0
        };
    }

    // Clearing the preview at release would show the marks at their old
    // position for the round trip to the server. It is held until the
    // committed frame mutates the marks, and dropped in that same task.
    #settle(delta) {
        if (delta.dx === 0 && delta.dy === 0) {
            this.#marks.removeAttribute('transform');
            return;
        }

        this.#settleWith(`translate(${delta.dx}, ${delta.dy})`);
    }

    #settleWith(transform) {
        const target = this.#marks;
        target.setAttribute('transform', transform);

        const observer = new MutationObserver(records => {
            if (records.some(record => record.target !== target || record.attributeName !== 'transform')) {
                this.#unsettle();
            }
        });

        observer.observe(target, { subtree: true, childList: true, attributes: true, characterData: true });
        if (target.parentNode) {
            observer.observe(target.parentNode, { childList: true });
        }

        this.#settling = { target, observer, timer: setTimeout(() => this.#unsettle(), SETTLE_TIMEOUT_MS) };
    }

    #unsettle() {
        const settling = this.#settling;
        if (!settling) {
            return;
        }

        this.#settling = null;
        settling.observer.disconnect();
        clearTimeout(settling.timer);
        settling.target.removeAttribute('transform');

        // The committed frame also carries the line at its new position.
        if (this.#options.cursor) {
            const state = cursors.get(this.#cursorKey());
            if (state?.navigation?.released) {
                state.navigation = null;
                this.#applyCursor();
            }
        }
    }

    #cancelPan(keepPreview = false) {
        const pointerId = this.#pointerId;
        this.#pointerId = null;
        this.#dragging = false;
        this.#panOrigin = null;
        this.#panMatrix = null;
        this.#panInverse = null;
        this.#panStart = null;
        cancelAnimationFrame(this.#panFrame);
        this.#panFrame = 0;
        const linked = this.#linked;
        this.#linked = [];
        if (!keepPreview) {
            this.#marks.removeAttribute('transform');
            for (const link of linked) {
                link.panel.#marks.removeAttribute('transform');
            }
        }

        if (pointerId !== null && this.#capture.hasPointerCapture(pointerId)) {
            this.#capture.releasePointerCapture(pointerId);
        }
    }

    dispose() {
        this.#abort.abort();
        if (this.#options.cursor) {
            this.#disposeCursor();
        }

        this.#unsettle();
        this.#cancelPan();
    }

}

const panels = new Map();
const cursors = new Map();

function pumpCursor(state) {
    if (state.inFlight || !state.pending) {
        return;
    }

    const next = state.pending;
    state.pending = null;
    state.inFlight = true;
    state.sent++;

    let call;
    try {
        call = PanelInteractivity.send(next);
    } catch {
        call = Promise.resolve();
    }

    call.catch(() => {
        // Circuit disconnected.
    }).finally(() => {
        state.inFlight = false;
        pumpCursor(state);
    });
}

export function initialize(id, capture, target, dotNetRef, options) {
    dispose(id);

    const panel = new PanelInteractivity(capture, target, dotNetRef, options);
    panel.start();

    panels.set(id, panel);
}

export function showTooltip(id, element) {
    panels.get(id)?.showTooltip(element);
}

export function dispose(id) {
    panels.get(id)?.dispose();
    panels.delete(id);
}
