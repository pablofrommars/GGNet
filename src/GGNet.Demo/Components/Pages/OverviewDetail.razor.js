// Times a refresh end to end, from the request leaving the browser to the
// frame after the changed SVG is in the DOM: first with the visible overview
// alone, then with the hidden detail refreshed as well. The difference is what
// keeping the hidden plot current costs.
export async function measure(page, rounds) {
    const run = async hidden => {
        const targets = [document.querySelector('[data-qa=overview] svg')];
        if (hidden) {
            targets.push(document.querySelector('[data-qa=detail] svg'));
        }

        const times = [];
        for (let i = 0; i < rounds; i++) {
            const changed = targets.map(target => new Promise(resolve => {
                const observer = new MutationObserver(() => {
                    observer.disconnect();
                    resolve(true);
                });
                observer.observe(target, { subtree: true, childList: true, attributes: true, characterData: true });
                setTimeout(() => {
                    observer.disconnect();
                    resolve(false);
                }, 3000);
            }));

            const started = performance.now();
            await page.invokeMethodAsync('RefreshRoundAsync', hidden);
            const arrived = await Promise.all(changed);
            await new Promise(resolve => requestAnimationFrame(resolve));

            if (arrived.every(Boolean)) {
                times.push(performance.now() - started);
            }
        }

        return times;
    };

    const summary = times => times.length === 0
        ? { rounds: 0, mean: 0, max: 0 }
        : { rounds: times.length, mean: times.reduce((sum, time) => sum + time, 0) / times.length, max: Math.max(...times) };

    const visible = await run(false);
    const both = await run(true);

    return { visible: summary(visible), both: summary(both) };
}
