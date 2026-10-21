const ON_WHEEL = 'OnWheelAsync';
const ON_PAN_END = 'OnPanEndAsync';
const TOOLTIP_OFFSET = 12;
const EDGE_MARGIN = 4;

class PanelInteractivity {
    #capture;
    #target;
    #dotNetRef;
    #options;
    #abort;
    #pointerId = null;
    #panMatrix = null;
    #panInverse = null;
    #panStart = null;
    #panFrame = 0;
    #pointerX = 0;
    #pointerY = 0;
    #tooltip = null;

    constructor(capture, target, dotNetRef, options) {
        this.#capture = capture;
        this.#target = target;
        this.#dotNetRef = dotNetRef;
        this.#options = options;
        this.#abort = new AbortController();
    }

    start() {
        const options = { signal: this.#abort.signal };

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
        }, { signal: this.#abort.signal, passive: false });

        this.#capture.addEventListener('pointermove', e => {
            if (this.#pointerId !== null && e.pointerId !== this.#pointerId) {
                return;
            }

            this.#pointerX = e.clientX;
            this.#pointerY = e.clientY;

            if (this.#pointerId !== null) {
                const delta = this.#delta(e);
                if (delta) {
                    this.#target.setAttribute('transform', `translate(${delta.dx}, ${delta.dy})`);
                }
            } else if (this.#options.tooltip && this.#tooltip?.isConnected) {
                this.#place(this.#tooltip);
            }
        }, options);

        if (!this.#options.pan || (!this.#options.panX && !this.#options.panY)) {
            return;
        }

        this.#capture.addEventListener('pointerdown', e => {
            if (e.button !== 0 || !e.isPrimary || this.#pointerId !== null) {
                return;
            }

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

            e.preventDefault();
            this.#pointerId = e.pointerId;
            this.#panMatrix = transform.matrix;
            this.#panInverse = transform.inverse;
            this.#panStart = point;

            try {
                this.#capture.setPointerCapture(e.pointerId);
                this.#watchTransform();
            } catch {
                this.#cancelPan();
            }
        }, options);

        this.#capture.addEventListener('pointerup', async e => {
            if (e.pointerId !== this.#pointerId) {
                return;
            }

            const delta = this.#delta(e);
            this.#cancelPan();
            if (!delta || (delta.dx === 0 && delta.dy === 0)) {
                return;
            }

            try {
                await this.#dotNetRef.invokeMethodAsync(ON_PAN_END, delta.dx, delta.dy);
            } catch {
                // Circuit disconnected; the preview is already cleared.
            }
        }, options);

        for (const type of ['pointercancel', 'lostpointercapture']) {
            this.#capture.addEventListener(type, e => {
                if (e.pointerId === this.#pointerId) {
                    this.#cancelPan();
                }
            }, options);
        }
    }

    showTooltip(element) {
        this.#tooltip = element;

        if (!element.matches(':popover-open')) {
            try {
                element.showPopover();
            } catch {
                // Already open, or the Popover API is unavailable.
            }
        }

        this.#place(element);
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

        element.style.left = `${x}px`;
        element.style.top = `${y}px`;
    }

    #matrix() {
        if (!this.#capture.isConnected || !this.#target.isConnected) {
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
            && ['a', 'b', 'c', 'd', 'e', 'f'].every(key => current[key] === this.#panMatrix[key]);
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

    #cancelPan() {
        const pointerId = this.#pointerId;
        this.#pointerId = null;
        this.#panMatrix = null;
        this.#panInverse = null;
        this.#panStart = null;
        cancelAnimationFrame(this.#panFrame);
        this.#panFrame = 0;
        this.#target.removeAttribute('transform');

        if (pointerId !== null && this.#capture.hasPointerCapture(pointerId)) {
            this.#capture.releasePointerCapture(pointerId);
        }
    }

    dispose() {
        this.#abort.abort();
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
