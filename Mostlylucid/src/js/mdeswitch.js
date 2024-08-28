(function(window) {
    'use strict';

    const elementCode = 'div.mermaid';

    const loadMermaid = (theme) => {
        window.mermaid.initialize({startOnLoad: false,theme: theme});
        window.mermaid.run({nodes: document.querySelectorAll(elementCode)})
    };

    const saveOriginalData = async () => {
        try {
            console.log("Saving original data");
            const elements = document.querySelectorAll(elementCode);
            const count = elements.length;

            if (count === 0) return;

            const promises = Array.from(elements).map((element) => {
                if (element.getAttribute('data-processed') != null) return;
                element.setAttribute('data-original-code', element.innerHTML);
            });

            await Promise.all(promises);
        } catch (error) {
            console.error(error);
            throw error;
        }
    };

    const resetProcessed = async () => {
        try {
            console.log("Resetting processed data");
            const elements = document.querySelectorAll(elementCode);
            const count = elements.length;

            if (count === 0) return;

            const promises = Array.from(elements).map((element) => {
                if (element.getAttribute('data-original-code') != null) {
                    element.removeAttribute('data-processed');
                    element.innerHTML = element.getAttribute('data-original-code');
                }
            });

            await Promise.all(promises);
        } catch (error) {
            console.error(error);
            throw error;
        }
    };

    const init = async () => {
        const mermaidElements = document.querySelectorAll(elementCode);
        if (mermaidElements.length === 0) return;

        try {
            await saveOriginalData();
        } catch (error) {
            console.error(error);
        }

        document.body.addEventListener('dark-theme-set', async () => {
            try {
                await resetProcessed();
                loadMermaid('dark');
                console.log("Dark theme set");
            } catch (error) {
                console.error(error);
            }
        });

        document.body.addEventListener('light-theme-set', async () => {
            try {
                await resetProcessed();
                loadMermaid('default');
                console.log("Light theme set");
            } catch (error) {
                console.error(error);
            }
        });

        const isDarkMode = localStorage.theme === 'dark';
        loadMermaid(isDarkMode ? 'dark' : 'default');
    };

    window.initMermaid = init;
})(window);