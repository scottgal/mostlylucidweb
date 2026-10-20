// Run with a local site on :8080 and an open playwright-cli browser:
// playwright-cli run-code --filename scripts/check-search-race.js
(async page => {
    await page.goto('http://localhost:8080/search?query=bot');
    let markStarted;
    const started = new Promise(resolve => { markStarted = resolve; });
    await page.route('**/search?**', async route => {
        if (route.request().headers()['hx-target'] === 'contentcontainer') {
            markStarted();
            await new Promise(resolve => setTimeout(resolve, 1500));
        }
        await route.continue().catch(() => {}); // Replacement requests abort the old search.
    });
    try {
        await page.locator('#searchQuery').fill('adaptive');
        await Promise.race([
            started,
            new Promise((_, reject) => setTimeout(() => reject(new Error('Auto-search did not start')), 5000))
        ]);
        // Choose a topic while the automatic search still carries the previous topic.
        await page.locator('#categorySelect').selectOption('Bot Detection');
        await page.waitForTimeout(2300);
        await page.waitForLoadState('networkidle');
        const category = await page.locator('#categorySelect').inputValue();
        const query = await page.locator('#searchQuery').inputValue();
        const url = new URL(page.url());
        if (category !== 'Bot Detection' || url.searchParams.get('category') !== category || query !== 'adaptive') {
            throw new Error(`A stale search overwrote the filters: ${page.url()}`);
        }
        return { passed: true, category, query, url: page.url() };
    } finally {
        await page.unrouteAll();
    }
})
