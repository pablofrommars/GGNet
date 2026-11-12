// Raw contact delivery, independent of GGNet's gesture module: what the browser
// hands the page, so a hardware or browser delivery problem can be told apart
// from a gesture-recognition one. Two layers are shown. Touch events list every
// finger the browser receives, even after it claims a native pinch or scroll;
// pointer events stop for fingers the browser has cancelled. Everything stays
// client-side at frame rate.
export function start(root) {
    const abort = new AbortController();
    const active = new Map();
    const counts = { down: 0, up: 0, cancel: 0 };
    let max = 0;
    let touches = 0;
    let touchMax = 0;
    let frame = 0;

    const field = name => root.querySelector(`[data-${name}]`);

    const render = () => {
        frame = 0;

        const viewport = window.visualViewport;

        field('touches').textContent = touches;
        field('touch-max').textContent = touchMax;
        field('contacts').textContent = active.size;
        field('max').textContent = max;
        field('down').textContent = counts.down;
        field('up').textContent = counts.up;
        field('cancel').textContent = counts.cancel;
        field('scale').textContent = (viewport?.scale ?? 1).toFixed(2);
        field('scroll').textContent = `${Math.round(window.scrollX)},${Math.round(window.scrollY)}`;
        field('pointers').textContent = [...active.values()]
            .map(p => `#${p.id} ${p.type}${p.primary ? '*' : ''} ${Math.round(p.x)},${Math.round(p.y)}`)
            .join('   ') || 'no active contacts';
    };

    const schedule = () => {
        if (!frame) {
            frame = requestAnimationFrame(render);
        }
    };

    const record = e => active.set(e.pointerId, {
        id: e.pointerId,
        type: e.pointerType,
        primary: e.isPrimary,
        x: e.clientX,
        y: e.clientY
    });

    const options = { capture: true, passive: true, signal: abort.signal };

    window.addEventListener('pointerdown', e => {
        counts.down++;
        record(e);
        max = Math.max(max, active.size);
        schedule();
    }, options);

    window.addEventListener('pointermove', e => {
        if (active.has(e.pointerId)) {
            record(e);
            schedule();
        }
    }, options);

    window.addEventListener('pointerup', e => {
        counts.up++;
        active.delete(e.pointerId);
        schedule();
    }, options);

    window.addEventListener('pointercancel', e => {
        counts.cancel++;
        active.delete(e.pointerId);
        schedule();
    }, options);

    for (const type of ['touchstart', 'touchmove', 'touchend', 'touchcancel']) {
        window.addEventListener(type, e => {
            touches = e.touches.length;
            touchMax = Math.max(touchMax, touches);
            schedule();
        }, options);
    }

    window.addEventListener('scroll', schedule, options);
    window.visualViewport?.addEventListener('resize', schedule, { signal: abort.signal });
    window.visualViewport?.addEventListener('scroll', schedule, { signal: abort.signal });

    field('reset').addEventListener('click', () => {
        active.clear();
        counts.down = counts.up = counts.cancel = 0;
        max = 0;
        touches = 0;
        touchMax = 0;
        schedule();
    }, { signal: abort.signal });

    render();

    return {
        stop() {
            abort.abort();
            cancelAnimationFrame(frame);
        }
    };
}

export function showModal(dialog) {
    if (!dialog.open) {
        dialog.showModal();
    }
}
