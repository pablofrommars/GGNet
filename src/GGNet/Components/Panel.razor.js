const ON_WHEEL = 'OnWheelAsync';
const ON_PAN_END = 'OnPanEndAsync';
const ON_CURSOR = 'OnCursorAsync';
const ON_CURSOR_LEAVE = 'OnCursorLeaveAsync';
const ON_CURSOR_RESTORE = 'OnCursorRestoreAsync';
const ON_TOUCH_NAVIGATE = 'OnTouchNavigateAsync';
const ON_TOUCH_INSPECT = 'OnTouchInspectAsync';
const ON_TOUCH_ACTION = 'OnTouchActionAsync';
const DOUBLE_TAP_MS = 300;
const DOUBLE_TAP_DISTANCE = 24;
const TOOLTIP_OFFSET = 12;
const EDGE_MARGIN = 4;
// Screen pixels a pointer must travel before a press becomes a pan. Below it
// the press stays an ordinary tap or click: no capture, no preview, no commit.
const DRAG_THRESHOLD = { mouse: 4, pen: 6, touch: 10 };
const CLICK_SUPPRESSION_MS = 500;
// How long a preview or operation may stand without its acknowledging frame
// before it is abandoned. A recovery bound, never an acknowledgement.
const RECOVERY_TIMEOUT_MS = 10000;
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
    #panPreview = false;
    #panShift = null;
    #panObserver = null;
    #root = null;
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
    #touchCoordinator = null;
    #labelObserver = null;
    #labelResizeObserver = null;
    #labelKey = null;
    #contextPreview = null;

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

        // Held, because a disposed panel is already detached from the root
        // whose feedback it must still take off.
        this.#root = this.#capture.closest('.ggnet');
        this.#labelObserver = new MutationObserver(() => this.#measureLabel());
        this.#labelObserver.observe(this.#capture, { subtree: true, childList: true, attributes: true,
            attributeFilter: ['data-ggnet-label-key'] });
        this.#labelResizeObserver = new ResizeObserver(() => { this.#labelKey = null; this.#measureLabel(); });
        this.#labelResizeObserver.observe(this.#root ?? this.#capture.ownerSVGElement);
        this.#measureLabel();
        document.fonts?.addEventListener('loadingdone', () => { this.#labelKey = null; this.#measureLabel(); }, options);
        if (this.touchEnabled) {
            const key = this.#options.group ?? this;
            let coordinator = touchGroups.get(key);
            if (!coordinator) {
                coordinator = new TouchCoordinator(key);
                touchGroups.set(key, coordinator);
            }
            this.#touchCoordinator = coordinator;
            coordinator.add(this);
        }

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

            // Touch has one plot-scoped owner; this path handles mouse and pen.
            if (e.pointerType === 'touch') {
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

            // The frame that renders this commit names it by this number, so
            // it is recognized even when it arrives ahead of the answer.
            const token = ++panCommits;
            const held = [[this, this.#settle(delta, token)]];
            for (const link of linked) {
                held.push([link.panel, link.panel.#settle({ dx: delta.dx * link.sx, dy: delta.dy * link.sy }, token)]);
            }

            // The commit answers with the epoch it advanced the plot to. Only
            // the server knows it: a host can advance the epoch unseen.
            let epoch = 0;
            try {
                epoch = await this.#dotNetRef.invokeMethodAsync(ON_PAN_END, delta.dx, delta.dy, token);
            } catch {
                // Circuit disconnected: no committed frame will arrive.
            }

            for (const [panel, settling] of held) {
                panel.#expect(settling, epoch);
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
            this.#touchCoordinator?.yieldPreview();
            this.#feedback();
            if (e.pointerType !== 'mouse') {
                this.#dismissTooltip();
            }
        } else if (!this.#held()) {
            this.#cancelPan();
            return;
        }

        const delta = this.#delta(e);
        if (delta) {
            this.#paintPan(delta.dx, delta.dy);
            for (const link of this.#linked) {
                link.panel.#paintPan(delta.dx * link.sx, delta.dy * link.sy);
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
            if (e.pointerType !== 'touch' || this.touchEnabled) {
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

            if (e.pointerType === 'touch' || this.#fromTouch(e)) {
                e.stopPropagation();
                return;
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
        navigation.scale = Math.min(10, Math.max(0.1, Math.hypot(p.x - q.x, p.y - q.y) / navigation.distance));
        if (!Number.isFinite(navigation.to) || !(navigation.scale > 0)) {
            return;
        }

        this.#touchCoordinator.previewNavigation(navigation);
    }

    #cursorKey() {
        return (this.touchEnabled || this.#options.cursor === 'shared') && this.#options.group ? this.#options.group : this;
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
        return key === this ? [this] : [...panels.values()].filter(panel => panel.#cursorKey() === key);
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
        state.timer = setTimeout(() => this.#dropCursor(state), RECOVERY_TIMEOUT_MS);
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
        state.timer = setTimeout(() => this.#dropCursor(state), RECOVERY_TIMEOUT_MS);
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
        if (this.touchPaintInspection()) return;
        const element = this.#cursorElement();
        if (!element) {
            return;
        }

        this.#observeCursor(element);

        const line = element.querySelector('line');
        const area = this.#area();
        const markers = element.parentElement?.querySelector('[data-ggnet-cursor-markers]');
        if (markers) {
            markers.style.visibility = '';
        }

        const desired = this.#touchCoordinator?.navigationView;
        const rendered = parseTouchView(element.dataset.ggnetView);
        if (desired && rendered && line && area && area.width > 0) {
            const base = Number(line.getAttribute('x1'));
            const value = rendered.min + (base - area.x) / area.width * (rendered.max - rendered.min);
            const fraction = (value - desired.min) / (desired.max - desired.min);
            element.setAttribute('transform', `translate(${area.x + fraction * area.width - base}, 0)`);
            element.style.visibility = fraction < 0 || fraction > 1 ? 'hidden' : '';
            return;
        }

        const live = !!state.live && this.#options.cursorContinuous;
        const shift = line && area && area.width > 0 ? this.#cursorShift(element, line, area) : 0;
        if (!line || !area || (!live && shift === 0)) {
            element.removeAttribute('transform');
            element.style.visibility = state.concealed ? 'hidden' : '';
            return;
        }

        const base = Number(line.getAttribute('x1'));
        const target = (live ? area.x + state.live.fraction * area.width : base) + shift;
        element.setAttribute('transform', `translate(${target - base}, 0)`);
        element.style.visibility = shift !== 0 && (target < area.x || target > area.x + area.width) ? 'hidden'
            : live ? 'visible' : state.concealed ? 'hidden' : '';
    }

    // A pinned line is a data value: under a mouse or pen pan preview it
    // rides with the marks. Drawn against the window the marks show, that
    // is the preview's own shift, whatever frames arrive meanwhile. A
    // cursor rendered against another window is first placed where its
    // value falls in the marks' window, so the shift never lands on top of
    // a position that is already the committed one.
    #cursorShift(element, line, area) {
        const dx = this.#panShift;
        if (dx === null || dx === 0 || !this.#cursorPinned()) {
            return 0;
        }

        // The marks' own stamp, whether or not the panel's frame matches it
        // yet: it names the window the marks on screen were drawn in.
        const marks = parseTouchView(this.#marks.querySelector('[data-ggnet-marks-frame]')?.dataset.ggnetView);
        const own = parseTouchView(element.dataset.ggnetView);
        if (!marks || !own || (own.min === marks.min && own.max === marks.max)) {
            return dx;
        }

        const base = Number(line.getAttribute('x1'));
        const value = own.min + (base - area.x) / area.width * (own.max - own.min);
        return area.x + (value - marks.min) / (marks.max - marks.min) * area.width + dx - base;
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
            // The leave is confirmed by a frame without a cursor. A pinned one
            // was never this pointer's hover: it is the host's, and is shown.
            if (element.getAttribute('visibility') === 'hidden' || element.hasAttribute('data-ggnet-pinned')) {
                state.concealed = false;
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
    // position for the round trip to the server. It is held until a rendered
    // frame is the commit's own, which names the commit's number, or carries
    // the epoch the commit advanced to, as a later host command's does. The
    // number is known at release and the epoch only once the commit has
    // answered, so a frame rendered ahead of the answer still settles, and a
    // frame that only brings data never does.
    #settle(delta, token) {
        if (delta.dx === 0 && delta.dy === 0) {
            this.#clearPan();
            return null;
        }

        return this.#settleWith(delta.dx, delta.dy, token);
    }

    #settleWith(dx, dy, token) {
        this.#unsettle();
        const transform = this.#paintPan(dx, dy);

        const observer = new MutationObserver(() => this.#reconcilePan());
        observer.observe(this.#capture, { subtree: true, childList: true, attributes: true,
            attributeFilter: ['data-ggnet-frame', 'data-ggnet-marks-frame', 'data-ggnet-view'] });

        // No quarantine follows: the commit was relative and carries no state
        // the next gesture depends on, so the last rendered frame simply stands.
        const timer = setTimeout(() => this.#unsettle(), RECOVERY_TIMEOUT_MS);
        this.#settling = { dx, dy, transform, token, epoch: null, observer, timer };
        this.#feedback();
        return this.#settling;
    }

    // A newer pan or a cancellation may have replaced the preview this commit
    // held; its answer then concerns nothing. Without an epoch nothing was
    // committed, so no frame will arrive.
    #expect(settling, epoch) {
        if (!settling || this.#settling !== settling) {
            return;
        }

        if (!(epoch > 0)) {
            this.#unsettle();
            return;
        }

        settling.epoch = epoch;
        this.#reconcilePan();
    }

    #reconcilePan() {
        const settling = this.#settling;
        if (!settling) {
            return;
        }

        const view = this.touchView();
        const commit = Number(this.#marks.querySelector('[data-ggnet-marks-frame]')?.dataset.ggnetCommit);
        if (view && (commit === settling.token || (settling.epoch !== null && view.epoch >= settling.epoch))) {
            this.#unsettle();
            return;
        }

        // A frame that is not the commit's may have replaced the marks group.
        if (this.#marks.getAttribute('transform') !== settling.transform) {
            this.#paintPan(settling.dx, settling.dy);
        }
    }

    #unsettle() {
        const settling = this.#settling;
        if (!settling) {
            return;
        }

        this.#settling = null;
        settling.observer.disconnect();
        clearTimeout(settling.timer);
        this.#clearPan();
        this.#feedback();
    }

    #paintPan(dx, dy) {
        const transform = `translate(${dx}, ${dy})`;
        this.#panPreview = true;
        this.#marks.setAttribute('transform', transform);
        this.#panShift = dx;
        this.#previewContext(1, dx, dy);

        // The cursor is rendered ahead of the marks, in a frame of its own:
        // it is placed again when the marks' frame follows.
        if (!this.#panObserver) {
            this.#panObserver = new MutationObserver(() => {
                this.#shiftCursor();
                if (this.#panPreview && this.#contextPreview) this.#previewContext(...this.#contextPreview);
            });
            this.#panObserver.observe(this.#capture, { subtree: true, childList: true, attributes: true,
                attributeFilter: ['data-ggnet-frame', 'data-ggnet-marks-frame', 'data-ggnet-view'] });
        }

        this.#shiftCursor();
        return transform;
    }

    #clearPan() {
        this.#panPreview = false;
        this.#marks.removeAttribute('transform');
        this.#clearContext();
        this.#panObserver?.disconnect();
        this.#panObserver = null;
        if (this.#panShift !== null) {
            this.#panShift = null;
            this.#shiftCursor();
        }
    }

    #shiftCursor() {
        if (this.#options.cursor && !this.#abort.signal.aborted) {
            this.#placeCursor(this.#cursorState());
        }
    }

    // The plot root says what a mouse or pen pan is doing, as it does for
    // touch: dragging, or released and waiting for the frame of its commit.
    get feedbackRoot() { return this.#root; }
    get pointerFeedback() { return this.#dragging ? 'navigating' : this.#settling ? 'pending' : null; }

    #feedback() {
        paintFeedback(this.#root);
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
            this.#clearPan();
            for (const link of linked) {
                link.panel.#clearPan();
            }

            this.#feedback();
        }

        if (pointerId !== null && this.#capture.hasPointerCapture(pointerId)) {
            this.#capture.releasePointerCapture(pointerId);
        }
    }

    get touchEnabled() { return !!this.#options.touch?.inspect || this.#options.touch?.navigation > 0
        || !!this.#options.touch?.tapActivatesPanel || !!this.#options.touch?.doubleTapResetsView; }
    get touchOptions() { return this.#options.touch; }
    get touchRoot() { return this.#capture.closest('.ggnet'); }
    get touchCapture() { return this.#capture; }
    get touchAvailable() {
        const style = getComputedStyle(this.#capture);
        return this.#capture.isConnected && style.pointerEvents !== 'none' && style.visibility !== 'hidden'
            && this.#capture.getClientRects().length > 0;
    }
    touchContains(target) { return target instanceof Node && this.#capture.contains(target); }
    touchPoint(e, clamp = false) { return this.#cursorPoint(e, clamp); }
    touchMatrix() { return this.#matrix()?.matrix.toString(); }
    touchSnapshot() { return this.#touchCoordinator.snapshot(this); }
    touchScrub(at) { this.#dropCursor(this.#cursorState()); this.#touchCoordinator.inspect(this, at, false); }
    touchPin(at) { this.#dropCursor(this.#cursorState()); this.#touchCoordinator.inspect(this, at, true); }
    touchRestore(previous) { this.#dropCursor(this.#cursorState()); this.#touchCoordinator.restore(this, previous); }
    touchInspection() {
        const element = this.#capture.querySelector('[data-ggnet-inspection]');
        if (!element) return null;
        const [session, epoch, revision] = element.dataset.ggnetInspection.split(' ');
        const selection = element.getAttribute('data-ggnet-selection');
        const committed = element.getAttribute('data-ggnet-committed');
        return { session, epoch: Number(epoch), revision: Number(revision),
            selection: selection === null ? null : Number(selection), committed: committed === null ? null : Number(committed) };
    }
    touchSendInspection(message) {
        return this.#dotNetRef.invokeMethodAsync(ON_TOUCH_INSPECT, message.session, message.epoch,
            message.revision, message.mapped ?? 0, message.pinned, message.restore);
    }
    touchSendAction(message) {
        return this.#dotNetRef.invokeMethodAsync(ON_TOUCH_ACTION, message.session, message.epoch, message.revision, message.reset);
    }
    async #measureLabel() {
        const label = this.#capture.querySelector('[data-ggnet-label-key]');
        const key = label?.dataset.ggnetLabelKey;
        if (!key || key === this.#labelKey) return;
        this.#labelKey = key;
        await document.fonts?.ready;
        if (!label.isConnected || this.#abort.signal.aborted || label.dataset.ggnetLabelKey !== key) return;
        const size = label.getAttribute('font-size');
        const length = label.getAttribute('textLength');
        label.removeAttribute('textLength');
        label.setAttribute('font-size', label.dataset.ggnetLabelSize);
        let box;
        try {
            box = label.getBBox();
        } finally {
            label.setAttribute('font-size', size);
            if (length !== null) label.setAttribute('textLength', length);
        }
        if (!(box.height > 0)) { this.#labelKey = null; return; }
        try {
            await this.#dotNetRef.invokeMethodAsync('OnCursorLabelMeasuredAsync', key, box.width, box.height);
        } catch {
            this.#labelKey = null;
        }
    }
    touchPaintInspection() {
        const pending = this.#touchCoordinator?.pendingInspection;
        let line = this.#capture.querySelector('[data-ggnet-provisional]');
        if (!pending) {
            line?.remove();
            return false;
        }
        const area = this.#area();
        const view = this.#touchCoordinator.navigationView ?? this.touchView();
        if (!area || !view) return true;
        if (!line) {
            line = document.createElementNS('http://www.w3.org/2000/svg', 'line');
            line.setAttribute('data-ggnet-provisional', '');
            line.setAttribute('class', 'crosshair pointer-events-none');
            this.#capture.appendChild(line);
        }
        const fraction = pending.mapped === null ? -1 : (pending.mapped - view.min) / (view.max - view.min);
        const x = area.x + fraction * area.width;
        line.setAttribute('x1', x); line.setAttribute('x2', x);
        line.setAttribute('y1', area.y); line.setAttribute('y2', area.y + area.height);
        line.style.visibility = fraction < 0 || fraction > 1 ? 'hidden' : 'visible';
        const cursor = this.#cursorElement();
        if (cursor) cursor.style.visibility = 'hidden';
        return true;
    }
    touchDismiss() { this.#dismissTooltip(); this.#release(); }
    touchBegin(first, second) {
        this.#dropCursor(this.#cursorState());
        const navigation = this.#beginNavigation(first, second);
        return navigation ? this.#touchCoordinator.beginNavigation(this, navigation) : null;
    }
    touchMove(navigation, touches) { this.#moveNavigation(navigation, touches); }
    touchEnd(navigation, commit) { this.#touchCoordinator.endNavigation(this, navigation, commit); }
    touchView() {
        const marks = this.#marks.querySelector('[data-ggnet-marks-frame]');
        if (!marks || marks.dataset.ggnetMarksFrame !== this.#capture.dataset.ggnetFrame) return null;
        return parseTouchView(marks.dataset.ggnetView);
    }
    touchCommit(view) {
        return this.#dotNetRef.invokeMethodAsync(ON_TOUCH_NAVIGATE, view.session, view.epoch, view.revision, view.min, view.max);
    }
    touchPreview(view) {
        const rendered = this.touchView();
        const area = this.#area();
        if (!view || !rendered || !area) {
            if (!this.#panPreview) {
                this.#marks.removeAttribute('transform');
                this.#clearContext();
            }
            this.#placeCursor(this.#cursorState());
            return;
        }
        const scale = (rendered.max - rendered.min) / (view.max - view.min);
        const offset = (rendered.min - view.min) / (view.max - view.min);
        const dx = area.x * (1 - scale) + offset * area.width;
        this.#marks.setAttribute('transform', `translate(${dx}, 0) scale(${scale}, 1)`);
        this.#previewContext(scale, dx, 0);
        this.#placeCursor(this.#cursorState());
    }
    #previewContext(scale, dx, dy) {
        this.#contextPreview = [scale, dx, dy];
        const area = this.#area();
        if (!area || !this.touchView()) {
            this.#clearContext();
            this.#contextPreview = [scale, dx, dy];
            return;
        }
        for (const element of this.#capture.querySelectorAll('.x-break, .x-minor-break, .x-break-label, .x-break-title, .y-break, .y-minor-break, .y-break-label')) {
            const xAxis = element.classList[0].startsWith('x-');
            const position = Number(element.getAttribute(xAxis ? (element.tagName === 'line' ? 'x1' : 'x') : (element.tagName === 'line' ? 'y1' : 'y')));
            const delta = xAxis ? position * (scale - 1) + dx : dy;
            const moved = position + delta;
            const min = xAxis ? area.x : area.y;
            const max = min + (xAxis ? area.width : area.height);
            element.setAttribute('transform', xAxis ? `translate(${delta}, 0)` : `translate(0, ${delta})`);
            element.style.visibility = moved < min || moved > max ? 'hidden' : '';
        }
        this.#capture.setAttribute('data-ggnet-context-preview', '');
    }
    #clearContext() {
        this.#contextPreview = null;
        if (!this.#capture.hasAttribute('data-ggnet-context-preview')) return;
        for (const element of this.#capture.querySelectorAll('.x-break, .x-minor-break, .x-break-label, .x-break-title, .y-break, .y-minor-break, .y-break-label')) {
            element.removeAttribute('transform');
            element.style.removeProperty('visibility');
        }
        this.#capture.removeAttribute('data-ggnet-context-preview');
    }
    touchPan(navigation, e) {
        const point = new DOMPoint(e.clientX, e.clientY).matrixTransform(navigation.inverse);
        navigation.to = point.x;
        this.#touchCoordinator.previewNavigation(navigation);
    }

    // The pan is dropped first: it repaints the cursor and the root's
    // feedback, which need the cursor state and the coordinator still in place.
    dispose() {
        this.#labelObserver?.disconnect();
        this.#labelResizeObserver?.disconnect();
        this.#unsettle();
        this.#cancelPan();
        this.#touchCoordinator?.remove(this);
        this.#abort.abort();
        if (this.#options.cursor) {
            this.#disposeCursor();
        }
    }

}

const panels = new Map();
let panCommits = 0;
const cursors = new Map();
const touchGroups = new Map();

// One owner arbitrates all contacts in a plot, including contacts on different
// panels. Draining is a state, not a timer: the remaining finger cannot become
// a new tap after navigation, cancellation or a third contact.
class TouchCoordinator {
    constructor(key) {
        this.key = key;
        this.panels = new Set();
        this.contacts = new Map();
        this.gesture = null;
        this.pendingTap = null;
        this.pendingView = null;
        this.navigationView = null;
        this.pendingInspection = null;
        this.commands = [];
        this.inFlight = null;
        this.watchdogs = { navigate: 0, inspect: 0, action: 0 };
        this.acknowledged = { navigate: 0, inspect: 0 };
        this.committedPin = undefined;
        this.revision = 0;
        this.unavailable = false;
        this.observers = new Map();
        this.draining = false;
        this.abort = new AbortController();
        const options = { signal: this.abort.signal, capture: true };
        window.addEventListener('pointerdown', e => this.down(e), options);
        window.addEventListener('pointermove', e => this.move(e), options);
        window.addEventListener('pointerup', e => this.up(e, true), options);
        window.addEventListener('pointercancel', e => this.up(e, false), options);
        window.addEventListener('lostpointercapture', e => {
            const g = this.gesture;
            if (g && e.target === g.panel.touchCapture && this.contacts.has(e.pointerId)) this.cancel();
        }, options);
        for (const type of ['resize', 'orientationchange']) window.addEventListener(type, () => this.cancel(), options);
        window.visualViewport?.addEventListener('resize', () => this.cancel(), options);
        window.addEventListener('blur', e => {
            if (e.target !== window) return;
            this.cancel();
            this.contacts.clear();
            this.draining = false;
        }, options);
        document.addEventListener('visibilitychange', () => {
            if (document.hidden) { this.cancel(); this.contacts.clear(); this.draining = false; }
        }, options);
        for (const type of ['click', 'dblclick']) window.addEventListener(type, e => {
            if ((e.pointerType === 'touch' || e.sourceCapabilities?.firesTouchEvents || performance.now() < this.blockClickUntil)
                && [...this.panels].some(panel => panel.touchContains(e.target))) {
                e.preventDefault();
                e.stopImmediatePropagation();
            }
        }, options);
    }

    add(panel) {
        this.panels.add(panel);
        this.root = panel.touchRoot ?? this.root;
        const observer = new MutationObserver(() => this.reconcileNavigation());
        observer.observe(panel.touchCapture, { subtree: true, childList: true, attributes: true,
            attributeFilter: ['data-ggnet-frame', 'data-ggnet-marks-frame', 'data-ggnet-view', 'data-ggnet-inspection', 'data-ggnet-selection', 'data-ggnet-committed'] });
        this.observers.set(panel, observer);
    }

    beginNavigation(panel, navigation) {
        const base = this.pendingView ?? this.effectiveView(panel);
        if (!base || this.unavailable) return null;
        this.revision = Math.max(this.revision, base.revision);
        navigation.base = { ...base };
        return navigation;
    }

    previewNavigation(navigation) {
        const base = navigation.base;
        if (!base) return;
        const from = (navigation.from - navigation.area.x) / navigation.area.width;
        const to = (navigation.to - navigation.area.x) / navigation.area.width;
        const span = base.max - base.min;
        this.navigationView = { ...base,
            min: base.min + span * (from - to / navigation.scale),
            max: base.min + span * (from + (1 - to) / navigation.scale) };
        this.feedback();
        this.paintNavigation();
    }

    paintNavigation() {
        for (const panel of this.panels) panel.touchPreview(this.navigationView);
    }

    // A mouse or pen drag takes the marks transform: a pending touch preview
    // is dropped at once, and its frame or the drag's own supersedes it.
    yieldPreview() {
        if (!this.navigationView || this.gesture?.navigation) return;
        this.navigationView = null;
        this.paintNavigation();
        this.feedback();
    }

    endNavigation(panel, navigation, commit) {
        const moved = Math.abs(navigation.to - navigation.from) >= NAVIGATE_MIN_SHIFT
            || Math.abs(navigation.scale - 1) >= NAVIGATE_MIN_SCALE;
        if (!commit || !moved || !this.navigationView) {
            this.navigationView = this.pendingView;
            this.paintNavigation();
            this.feedback();
            return;
        }
        const view = { ...this.navigationView, revision: ++this.revision };
        this.pendingView = view;
        this.navigationView = view;
        this.feedback();
        this.enqueue({ kind: 'navigate', panel, ...view });
    }

    effectiveView(panel) {
        const view = panel.touchView();
        const inspection = panel.touchInspection() ?? this.pendingInspection ?? this.identity;
        if (inspection) {
            this.revision = Math.max(this.revision, inspection.revision);
            this.identity = inspection;
        }
        if (view && inspection && inspection.session === view.session && inspection.epoch > view.epoch) {
            return { ...view, epoch: inspection.epoch };
        }
        return view;
    }

    snapshot(panel) {
        const mapped = this.committedPin !== undefined ? this.committedPin : panel.touchInspection()?.committed;
        return mapped == null ? null : { mapped };
    }

    inspect(panel, at, pinned, capturedView = null) {
        if (this.unavailable) return;
        const view = capturedView ?? this.pendingView ?? this.effectiveView(panel);
        if (!view) return;
        const mapped = view.min + at.fraction * (view.max - view.min);
        this.revision = Math.max(this.revision, view.revision);
        const message = { kind: 'inspect', panel, session: view.session, epoch: view.epoch,
            revision: ++this.revision, mapped, pinned, restore: false, tap: capturedView !== null };
        if (pinned) this.committedPin = mapped;
        this.pendingInspection = message;
        this.paintNavigation();
        this.feedback();
        this.enqueue(message);
    }

    restore(panel, previous) {
        if (this.unavailable || this.superseding) return;
        const view = this.pendingView ?? this.effectiveView(panel);
        if (!view) return;
        this.revision = Math.max(this.revision, view.revision);
        const message = { kind: 'inspect', panel, session: view.session, epoch: view.epoch,
            revision: ++this.revision, mapped: previous?.mapped ?? null, pinned: !!previous, restore: true };
        this.pendingInspection = message;
        this.paintNavigation();
        this.feedback();
        this.enqueue(message);
    }

    enqueue(message) {
        const tail = this.commands.at(-1);
        if (message.kind === 'inspect' && !message.pinned && !message.restore
            && tail?.kind === 'inspect' && !tail.pinned && !tail.restore) this.commands.pop();
        this.commands.push(message);
        this.armWatchdog(message.kind);
        this.pump();
    }

    pump() {
        if (this.inFlight || this.unavailable || !this.commands.length) return;
        const message = this.commands.shift();
        if ((message.kind === 'action' || message.tap) && !message.panel.touchAvailable) {
            if (this.pendingInspection === message) {
                this.pendingInspection = null;
                this.committedPin = undefined;
                this.paintNavigation();
                this.feedback();
            }
            this.pump();
            this.finishWatchdog();
            return;
        }
        this.inFlight = message.kind;
        const call = message.kind === 'navigate' ? message.panel.touchCommit(message)
            : message.kind === 'action' ? message.panel.touchSendAction(message) : message.panel.touchSendInspection(message);
        call.catch(() => this.failNavigation()).finally(() => {
            this.inFlight = null;
            this.pump();
            this.finishWatchdog();
        });
    }

    // The bound covers one kind of operation making no acknowledged progress:
    // a rendered frame that advances its revision restarts it, so sustained
    // interaction under latency is not mistaken for an unresolved call.
    armWatchdog(kind) {
        if (!this.watchdogs[kind]) this.watchdogs[kind] = setTimeout(() => this.failNavigation(), RECOVERY_TIMEOUT_MS);
    }

    clearWatchdog(kind) {
        clearTimeout(this.watchdogs[kind]);
        this.watchdogs[kind] = 0;
    }

    outstanding(kind) {
        return !!(kind === 'navigate' ? this.pendingView : kind === 'inspect' ? this.pendingInspection : null)
            || this.inFlight === kind || this.commands.some(command => command.kind === kind);
    }

    finishWatchdog() {
        for (const kind of ['navigate', 'inspect', 'action']) {
            if (!this.outstanding(kind)) this.clearWatchdog(kind);
        }
    }

    acknowledge(kind, frames) {
        const revision = Math.max(...frames.map(frame => frame?.revision ?? 0));
        if (!(revision > this.acknowledged[kind])) return;
        this.acknowledged[kind] = revision;
        if (this.watchdogs[kind]) {
            this.clearWatchdog(kind);
            if (this.outstanding(kind)) this.armWatchdog(kind);
        }
    }

    reconcileNavigation() {
        if (this.unavailable) return;
        const intended = this.navigationView ?? this.pendingView ?? this.pendingInspection ?? this.pendingTap?.view ?? this.gesture?.view;
        const frames = [...this.panels].map(panel => panel.touchView());
        const inspections = [...this.panels].map(panel => panel.touchInspection());
        this.acknowledge('navigate', frames);
        this.acknowledge('inspect', inspections);
        if (!intended) {
            this.committedPin = undefined;
            return;
        }
        if ([...frames, ...inspections].some(frame => frame && (frame.session !== intended.session || frame.epoch > intended.epoch))) {
            this.pendingView = null;
            this.navigationView = null;
            this.pendingInspection = null;
            this.commands = [];
            this.committedPin = undefined;
            this.superseding = true;
            this.cancel();
            this.superseding = false;
            this.finishWatchdog();
            this.paintNavigation();
            this.feedback();
            return;
        }
        if (this.pendingView && frames.every(frame => frame && frame.session === this.pendingView.session
            && frame.epoch === this.pendingView.epoch && frame.revision === this.pendingView.revision
            && frame.min === this.pendingView.min && frame.max === this.pendingView.max)) {
            this.pendingView = null;
            if (!this.gesture?.navigation) this.navigationView = null;
        }
        if (this.pendingInspection && inspections.every(frame => frame && frame.session === this.pendingInspection.session
            && frame.epoch === this.pendingInspection.epoch && frame.revision === this.pendingInspection.revision)) {
            this.pendingInspection = null;
            this.committedPin = inspections[0].committed;
        }
        this.finishWatchdog();
        this.paintNavigation();
        this.feedback();
    }

    // The root's attributes have one writer per plot. A mouse or pen pan
    // reports through here too, so neither input clears the other's state.
    feedback() {
        const pointer = this.root ? pointerFeedback(this.root) : null;
        const value = this.unavailable ? 'unavailable' : this.gesture?.navigation ? 'navigating'
            : this.gesture?.state === 'scrub' ? 'inspecting' : pointer === 'navigating' ? pointer
            : this.pendingView || this.pendingInspection || pointer ? 'pending' : null;
        for (const panel of this.panels) {
            panel.touchRoot?.toggleAttribute('data-ggnet-navigation', !!this.navigationView || !!pointer);
            if (value) panel.touchRoot?.setAttribute('data-ggnet-interaction', value);
            else if (!this.gesture) panel.touchRoot?.removeAttribute('data-ggnet-interaction');
        }
    }

    failNavigation() {
        this.unavailable = true;
        this.pendingView = null;
        this.navigationView = null;
        this.pendingInspection = null;
        this.commands = [];
        this.committedPin = undefined;
        this.clearWatchdog('navigate');
        this.clearWatchdog('inspect');
        this.clearWatchdog('action');
        this.cancel();
        this.paintNavigation();
        this.feedback();
    }

    remove(panel) {
        if (this.pendingTap?.panel === panel) this.clearTap();
        if (this.gesture?.panel === panel) this.cancel();
        this.observers.get(panel)?.disconnect();
        this.observers.delete(panel);
        this.panels.delete(panel);
        if (!this.panels.size) {
            this.unavailable = true;
            this.commands = [];
            this.pendingInspection = null;
            this.pendingView = null;
            this.navigationView = null;
            panel.touchCapture.querySelector('[data-ggnet-provisional]')?.remove();
            this.clearWatchdog('navigate');
            this.clearWatchdog('inspect');
            this.clearWatchdog('action');
            this.abort.abort();
            touchGroups.delete(this.key);

            // The plot root outlives its panels when a new context replaces
            // them, and nothing else would take this owner's feedback off it.
            // A successor can only be created once this owner is gone from
            // the registry, so the attributes cleared here are never its own.
            const root = this.root ?? panel.touchRoot;
            root?.removeAttribute('data-ggnet-interaction');
            root?.removeAttribute('data-ggnet-navigation');
        }
    }

    down(e) {
        if (e.pointerType !== 'touch') {
            this.clearTap();
            if (e.pointerType === 'mouse' || e.pointerType === 'pen') this.blockClickUntil = 0;
            return;
        }
        const panel = [...this.panels].find(p => p.touchContains(e.target) && p.touchPoint(e));
        if (!panel) this.clearTap();
        this.contacts.set(e.pointerId, { x: e.clientX, y: e.clientY, panel });
        if (this.draining || this.unavailable) return;
        const g = this.gesture;
        if (g) {
            this.clearTap();
            clearTimeout(g.timer);
            if (this.contacts.size !== 2 || !panel || !g.panel.touchOptions.navigation) {
                this.cancel();
                return;
            }
            const previousShift = g.state === 'pan' ? g.navigation.to - g.navigation.from : 0;
            if (g.state === 'scrub') g.panel.touchRestore(g.previous);
            const [a, b] = [...this.contacts.entries()];
            g.navigation = g.panel.touchBegin(a, b);
            if (!g.navigation) { this.cancel(); return; }
            g.navigation.from -= previousShift;
            g.state = 'navigate';
            if (!this.capture(g, e.pointerId)) return;
            this.state(g, 'navigating');
            g.panel.touchMove(g.navigation, this.contacts);
            return;
        }
        if (this.contacts.size !== 1 || !panel || !e.isPrimary) return;
        const tap = this.pendingTap;
        const second = tap?.panel === panel && performance.now() - tap.time <= DOUBLE_TAP_MS
            && Math.hypot(e.clientX - tap.x, e.clientY - tap.y) <= DOUBLE_TAP_DISTANCE;
        this.clearTap();
        this.blockClickUntil = 0;
        const gesture = {
            panel, id: e.pointerId, start: { x: e.clientX, y: e.clientY },
            last: e, state: 'pending', previous: panel.touchSnapshot(), matrix: panel.touchMatrix(), captures: new Set(),
            view: this.pendingView ?? this.effectiveView(panel), second
        };
        this.gesture = gesture;
        if (panel.touchOptions.inspect) gesture.timer = setTimeout(() => {
            if (this.gesture !== gesture || gesture.state !== 'pending') return;
            const at = panel.touchPoint(gesture.last);
            if (!at) { this.cancel(); return; }
            if (!this.capture(gesture, gesture.id)) return;
            gesture.state = 'scrub';
            panel.touchDismiss();
            this.state(gesture, 'inspecting');
            panel.touchScrub(at);
        }, 400);
        this.watch(gesture);
    }

    watch(g) {
        g.frame = requestAnimationFrame(() => {
            if (this.gesture !== g) return;
            if (!g.panel.touchAvailable || g.matrix !== g.panel.touchMatrix()) { this.cancel(); return; }
            this.watch(g);
        });
    }

    // False when the browser refuses the capture: the gesture is then already
    // cancelled, and the caller must not preview, mark state or inspect.
    capture(g, id) {
        try {
            g.panel.touchCapture.setPointerCapture(id);
            g.captures.add(id);
            return true;
        } catch {
            this.cancel();
            return false;
        }
    }

    state(g, state) { g.panel.touchRoot?.setAttribute('data-ggnet-interaction', state); }

    move(e) {
        if (e.pointerType !== 'touch') return;
        const contact = this.contacts.get(e.pointerId);
        if (contact) { contact.x = e.clientX; contact.y = e.clientY; }
        const g = this.gesture;
        if (!g) return;
        if (g.state === 'navigate') { g.panel.touchMove(g.navigation, this.contacts); return; }
        if (e.pointerId !== g.id) return;
        g.last = e;
        if (g.state === 'pending') {
            const dx = e.clientX - g.start.x, dy = e.clientY - g.start.y;
            if (!g.panel.touchPoint(e)) { this.cancel(); return; }
            if (Math.hypot(dx, dy) < DRAG_THRESHOLD.touch) return;
            clearTimeout(g.timer);
            if (Math.abs(dx) < Math.abs(dy) || g.panel.touchOptions.navigation !== 2) { this.cancel(); return; }
            // A virtual second point supplies the same affine preview for a
            // one-finger pan; its distance is never used for pan updates.
            g.navigation = g.panel.touchBegin([g.id, g.start], [-1, { x: g.start.x + 1, y: g.start.y }]);
            if (!g.navigation) { this.cancel(); return; }
            g.navigation.from -= 0.5 * g.navigation.inverse.a;
            g.navigation.to = g.navigation.from;
            g.state = 'pan';
            if (!this.capture(g, g.id)) return;
            this.state(g, 'navigating');
        }
        if (g.state === 'pan') g.panel.touchPan(g.navigation, e);
        if (g.state === 'scrub') {
            const at = g.panel.touchPoint(e, true);
            if (at) g.panel.touchScrub(at);
        }
    }

    up(e, commit) {
        if (e.pointerType !== 'touch') return;
        const g = this.gesture;
        if (g && this.contacts.has(e.pointerId)) {
            if (!commit) this.cancel();
            else {
                this.move(e);
                if (this.gesture === g) {
                    this.finish(g);
                    if (g.state === 'pan' || g.state === 'navigate') {
                        g.panel.touchEnd(g.navigation, true);
                    } else if (g.state === 'pending') {
                        this.tap(g, e);
                    } else if (g.panel.touchOptions.inspect) {
                        const at = g.panel.touchPoint(e, g.state === 'scrub');
                        if (at) g.panel.touchPin(at);
                    }
                }
            }
        }
        this.contacts.delete(e.pointerId);
        if (!this.contacts.size) this.draining = false;
    }

    finish(g) {
        this.gesture = null;
        this.draining = true;
        this.blockClickUntil = performance.now() + CLICK_SUPPRESSION_MS;
        clearTimeout(g.timer);
        cancelAnimationFrame(g.frame);
        for (const id of g.captures) {
            if (g.panel.touchCapture.hasPointerCapture(id)) g.panel.touchCapture.releasePointerCapture(id);
        }
        this.feedback();
    }

    cancel() {
        this.clearTap();
        const g = this.gesture;
        if (!g) return;
        this.finish(g);
        if (g.state === 'scrub') g.panel.touchRestore(g.previous);
        if (g.navigation) g.panel.touchEnd(g.navigation, false);
    }

    clearTap() {
        clearTimeout(this.pendingTap?.timer);
        this.pendingTap = null;
    }

    action(panel, view, reset) {
        if (!view || this.unavailable || !panel.touchAvailable) return;
        this.revision = Math.max(this.revision, view.revision);
        this.enqueue({ kind: 'action', panel, ...view, revision: ++this.revision, reset });
    }

    tap(g, e) {
        const at = g.panel.touchPoint(e);
        if (!at || !g.view) return;
        if (g.second && g.panel.touchOptions.doubleTapResetsView) {
            this.action(g.panel, g.view, true);
            return;
        }
        const run = () => {
            if (!g.panel.touchAvailable || g.matrix !== g.panel.touchMatrix()) return;
            if (g.panel.touchOptions.tapActivatesPanel) this.action(g.panel, g.view, false);
            else if (g.panel.touchOptions.inspect) this.inspect(g.panel, at, true, g.view);
        };
        if (!g.panel.touchOptions.doubleTapResetsView) { run(); return; }
        const pending = { panel: g.panel, view: g.view, time: performance.now(), x: e.clientX, y: e.clientY };
        pending.timer = setTimeout(() => {
            if (this.pendingTap !== pending) return;
            this.pendingTap = null;
            run();
        }, DOUBLE_TAP_MS);
        this.pendingTap = pending;
    }
}

function parseTouchView(value) {
    if (!value) return null;
    const [session, epoch, revision, min, max] = value.split(' ');
    const view = { session, epoch: Number(epoch), revision: Number(revision), min: Number(min), max: Number(max) };
    return [view.epoch, view.revision, view.min, view.max].every(Number.isFinite) && view.max > view.min ? view : null;
}

// The mouse and pen previews of one plot: a drag in progress on any of its
// panels, else a released pan still waiting for its frame.
function pointerFeedback(root) {
    let value = null;
    for (const panel of panels.values()) {
        if (panel.feedbackRoot !== root) {
            continue;
        }

        const own = panel.pointerFeedback;
        if (own === 'navigating') {
            return own;
        }

        value ??= own;
    }

    return value;
}

// A plot with touch has a coordinator that owns the root's attributes and
// merges the mouse and pen state into its own; without one they are written here.
function paintFeedback(root) {
    if (!root) {
        return;
    }

    for (const coordinator of touchGroups.values()) {
        if (coordinator.root === root) {
            coordinator.feedback();
            return;
        }
    }

    const value = pointerFeedback(root);
    root.toggleAttribute('data-ggnet-navigation', !!value);
    if (value) {
        root.setAttribute('data-ggnet-interaction', value);
    } else {
        root.removeAttribute('data-ggnet-interaction');
    }
}

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
