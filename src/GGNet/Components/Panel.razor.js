// GGNet's continuous-gesture module (the library's first JS asset). The wheel
// is captured here and converted to svg units against the rendered size (the
// svg stays responsive); drag-pan previews as a transform on the marks group
// at frame rate and crosses to .NET exactly once per gesture; the cursor-glued
// tooltip is positioned and edge-flipped here at frame rate while its content
// stays server-rendered.
const ON_WHEEL = 'OnWheelAsync';
const ON_PAN_END = 'OnPanEndAsync';
const TOOLTIP_OFFSET = 12;
const EDGE_MARGIN = 4;

class PanelInteractivity {
    #capture;
    #target;
    #svg;
    #dotNetRef;
    #options;
    #abort;
    #panActive = false;
    #startX = 0;
    #startY = 0;
    #scaleX = 1;
    #scaleY = 1;
    #pointerX = 0;
    #pointerY = 0;
    #tooltip = null;

    constructor(capture, target, dotNetRef, options) {
        this.#capture = capture;
        this.#target = target;
        this.#svg = capture.ownerSVGElement;
        this.#dotNetRef = dotNetRef;
        this.#options = options;
        this.#abort = new AbortController();
    }

    start() {
        const options = { signal: this.#abort.signal };

        this.#capture.addEventListener('wheel', async e => {
            e.preventDefault();

            const { x, y } = this.#toSvg(e.clientX, e.clientY);

            try {
                await this.#dotNetRef.invokeMethodAsync(ON_WHEEL, x, y, e.deltaY);
            } catch {
                // Circuit disconnected.
            }
        }, { signal: this.#abort.signal, passive: false });

        this.#capture.addEventListener('pointermove', e => {
            this.#pointerX = e.clientX;
            this.#pointerY = e.clientY;

            if (this.#panActive) {
                const { dx, dy } = this.#delta(e);
                this.#target.setAttribute('transform', `translate(${dx}, ${dy})`);
            } else if (this.#options.tooltip && this.#tooltip?.isConnected) {
                this.#place(this.#tooltip);
            }
        }, options);

        if (!this.#options.pan) {
            return;
        }

        this.#capture.addEventListener('pointerdown', e => {
            if (e.button !== 0) {
                return;
            }

            e.preventDefault();

            this.#panActive = true;
            this.#startX = e.clientX;
            this.#startY = e.clientY;
            this.#measure();
            this.#capture.setPointerCapture(e.pointerId);
        }, options);

        this.#capture.addEventListener('pointerup', async e => {
            if (!this.#panActive) {
                return;
            }

            this.#panActive = false;
            this.#target.removeAttribute('transform');

            const { dx, dy } = this.#delta(e);

            if (dx === 0 && dy === 0) {
                return;
            }

            try {
                await this.#dotNetRef.invokeMethodAsync(ON_PAN_END, dx, dy);
            } catch {
                // Circuit disconnected; the preview is already cleared.
            }
        }, options);

        this.#capture.addEventListener('pointercancel', () => {
            this.#panActive = false;
            this.#target.removeAttribute('transform');
        }, options);
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

    // Rendered-size scale, sampled fresh per gesture (per wheel event, and at
    // pan start) so a responsive svg always maps CSS pixels to svg units exactly.
    #measure() {
        const rect = this.#svg.getBoundingClientRect();
        const viewBox = this.#svg.viewBox.baseVal;

        this.#scaleX = rect.width > 0 ? viewBox.width / rect.width : 1;
        this.#scaleY = rect.height > 0 ? viewBox.height / rect.height : 1;

        return rect;
    }

    #toSvg(clientX, clientY) {
        const rect = this.#measure();

        return {
            x: (clientX - rect.left) * this.#scaleX,
            y: (clientY - rect.top) * this.#scaleY
        };
    }

    #delta(e) {
        return {
            dx: this.#options.panX ? (e.clientX - this.#startX) * this.#scaleX : 0,
            dy: this.#options.panY ? (e.clientY - this.#startY) * this.#scaleY : 0
        };
    }

    dispose() {
        this.#abort.abort();
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
