// Run through playwright-cli run-code against the real ASP.NET site-update article.
// This is a browser function, not a second renderer or an @playwright/test suite.
async page => {
    const require = (condition, message) => { if (!condition) throw new Error(message); };
    const transform = svg => svg.evaluate(s => {
        const viewport = s.querySelector('.svg-pan-zoom_viewport');
        return viewport?.style.transform || viewport?.getAttribute('transform');
    });
    const result = {diagrams: [], proxy: [], touch: []};
    await page.setViewportSize({width: 1440, height: 1100});
    require(await page.locator('iframe.article-explainer').count() === 2, 'Open the site-update article before running this check.');
    const setTheme = async dark => {
        if (await page.evaluate(() => document.documentElement.classList.contains('dark')) !== dark) {
            await page.getByRole('button', {name: dark ? 'Switch to dark theme' : 'Switch to light theme', exact: true}).click();
        }
        await page.waitForFunction(expected => Array.from(document.querySelectorAll('.mermaid')).every(d => d.dataset.diagramTheme === expected), dark ? 'dark' : 'default');
        await page.waitForTimeout(200);
    };
    const download = async (scope, name) => {
        for (const format of ['SVG', 'PNG']) {
            const pending = page.waitForEvent('download');
            await scope.getByRole('button', {name: `Export as ${format}`, exact: true}).click();
            const file = await pending;
            require(!await file.failure(), `${name}: ${format} download failed`);
            await file.saveAs(`output/playwright/${name}.${format.toLowerCase()}`);
        }
    };
    for (const dark of [false, true]) {
        await setTheme(dark);
        require(await page.locator('.mermaid-wrapper').count() === 3, 'Expected three article diagrams');
        for (let index = 0; index < 3; index++) {
            const wrapper = page.locator('.mermaid-wrapper').nth(index), svg = wrapper.locator('svg');
            await wrapper.evaluate(w => w.scrollIntoView({block: 'center'}));
            const before = await transform(svg);
            await wrapper.getByRole('button', {name: 'Zoom In', exact: true}).click();
            await page.waitForTimeout(100);
            require(await transform(svg) !== before, `Diagram ${index}: zoom did not change geometry`);
            await wrapper.getByRole('button', {name: 'Zoom Out', exact: true}).click();
            await wrapper.getByRole('button', {name: 'Reset View', exact: true}).click();
            await wrapper.getByRole('button', {name: 'Enable Pan & Zoom', exact: true}).click();
            require(await svg.getAttribute('data-interactive') === 'true', 'Gesture enable failed');
            await wrapper.getByRole('button', {name: 'Disable Pan & Zoom', exact: true}).click();
            require(await svg.getAttribute('data-interactive') === 'false', 'Gesture disable failed');
            await wrapper.getByRole('button', {name: 'Hide Toolbar', exact: true}).click();
            await wrapper.getByRole('button', {name: 'Show Toolbar', exact: true}).click();
            await page.waitForTimeout(350);
            require(!await wrapper.locator('.mermaid-controls').evaluate(c => c.classList.contains('hidden-slide')), 'Toolbar stayed clipped');
            await wrapper.getByRole('button', {name: 'Fullscreen', exact: true}).click();
            const modal = page.getByRole('dialog');
            await modal.waitFor(); await page.keyboard.press('Tab');
            require(await modal.evaluate(m => m.contains(document.activeElement)), 'Focus escaped fullscreen');
            if (index === 0) await modal.getByRole('button', {name: 'Close', exact: true}).click();
            else if (index === 1) await page.keyboard.press('Escape');
            else await modal.click({position: {x: 2, y: 2}});
            require(await modal.count() === 0, 'Fullscreen did not close');
            await download(wrapper, `article-${dark ? 'dark' : 'light'}-${index}`);
            result.diagrams.push({dark, index, controls: true, downloads: true});
        }
        const outer = page.locator('iframe.article-explainer[src*=proxy]');
        await outer.scrollIntoViewIfNeeded();
        const frame = page.frameLocator('iframe.article-explainer[src*=proxy]');
        await frame.locator('#proxy-stage svg').waitFor();
        await frame.getByRole('button', {name: 'Fullscreen', exact: true}).click();
        await frame.getByRole('dialog').getByRole('button', {name: 'Close', exact: true}).click();
        await download(frame, `article-pele-${dark ? 'dark' : 'light'}`);
    }
    const proxy = page.frameLocator('iframe.article-explainer[src*=proxy]');
    for (const scenario of ['browser', 'crawler', 'unfamiliar', 'burst']) {
        await proxy.locator('#scenario').selectOption(scenario);
        for (let index = 0; index < 5; index++) await proxy.getByRole('button', {name: 'Next step', exact: true}).click();
        const reached = await proxy.locator('#origin-reached').textContent();
        require(reached === (['browser', 'crawler'].includes(scenario) ? 'Yes' : 'No'), `Incorrect origin reachability: ${scenario}`);
        result.proxy.push({scenario, reached});
    }
    // Test scroll chaining through the actual article and its iframe, rather than a static-server substitute.
    await page.setViewportSize({width: 390, height: 844});
    const cdp = await page.context().newCDPSession(page);
    await cdp.send('Emulation.setTouchEmulationEnabled', {enabled: true, maxTouchPoints: 2});
    try {
        for (const [label, svg] of [['Mermaid', page.locator('.mermaid-wrapper svg').first()], ['Pele', proxy.locator('#proxy-stage svg')]]) {
            await svg.evaluate(s => s.scrollIntoView({block: 'center'})); await page.waitForTimeout(600);
            const box = await svg.boundingBox(), before = await page.evaluate(() => scrollY), matrix = await transform(svg);
            await cdp.send('Input.synthesizeScrollGesture', {x: Math.round(box.x + box.width / 2), y: Math.round(Math.min(650, Math.max(150, box.y + box.height / 2))), yDistance: -200, gestureSourceType: 'touch', speed: 400});
            await page.waitForTimeout(500);
            const delta = await page.evaluate(previous => scrollY - previous, before);
            require(delta > 100 && await transform(svg) === matrix, `${label}: swipe failed to scroll the article passively`);
            result.touch.push({label, delta});
        }
        const innerRange = await proxy.locator('body').evaluate(() => document.documentElement.scrollHeight - innerHeight);
        require(innerRange === 0, `Iframe has ${innerRange}px of inner scrolling`);
        require(!await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), 'Mobile article overflows horizontally');
    } finally {
        await cdp.send('Emulation.setTouchEmulationEnabled', {enabled: false}); await cdp.detach();
    }
    await page.setViewportSize({width: 1440, height: 1100});
    // Standalone links must escape the site's inherited HTMX boost.
    await page.getByRole('link', {name: 'Open the vector explorer', exact: true}).click();
    await page.waitForURL('**/interactive/explainers/index.html*');
    await page.locator('#plot').waitFor();
    require(await page.locator('#header').count() === 0, 'Standalone explorer was swapped into the article shell');
    result.standalone = true;
    await page.goBack();
    await page.waitForURL('**/blog/site-update-interactive-explainers-pele-and-deckgl*');
    return result;
}
