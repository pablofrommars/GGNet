const ON_WHEEL = 'OnWheelAsync';
const ON_PAN_END = 'OnPanEndAsync';
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

        if (!this.#options.pan || (!this.#options.panX && !this.#options.panY)) {
            return;
        }

        this.#capture.addEventListener('pointerdown', e => {
            if (e.button !== 0 || !e.isPrimary) {
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
                    this.#clickTarget = mark
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

        const point = new DOMPoint(x, y).matrixTransform(inverse);
        const area = panel.getBBox();
        return point.x >= area.x && point.x <= area.x + area.width
            && point.y >= area.y && point.y <= area.y + area.height;
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

        if (this.#options.crosshair) {
            const box = mark?.getBoundingClientRect();
            const strip = this.#strip(box ? box.x + box.width / 2 : x);
            if (strip) {
                this.#dispatch(strip, 'mouseover', x, y);
            }
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

        if (this.#options.crosshair) {
            this.#dispatch(this.#capture, 'mouseleave', this.#pointerX, this.#pointerY);
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

    #strip(x) {
        let nearest = null;
        let gap = Infinity;

        for (const strip of this.#capture.querySelectorAll('[data-ggnet-strip]')) {
            const box = strip.getBoundingClientRect();
            const distance = x < box.left ? box.left - x : x >= box.right ? x - box.right : 0;
            if (distance < gap) {
                nearest = strip;
                gap = distance;
            }
        }

        return nearest;
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
        const target = this.#marks;
        if (delta.dx === 0 && delta.dy === 0) {
            target.removeAttribute('transform');
            return;
        }

        target.setAttribute('transform', `translate(${delta.dx}, ${delta.dy})`);

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
        this.#unsettle();
        this.#cancelPan();
    }

}

const panels = new Map();

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
