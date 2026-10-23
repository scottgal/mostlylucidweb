/**
 * Mermaid Theme Switcher
 *
 * Automatically switches Mermaid diagram themes based on light/dark mode
 * Supports OS-level theme changes and manual theme switching
 *
 * @module theme-switcher
 */

import { enhanceMermaidDiagrams, cleanupMermaidEnhancements, getConfig } from './enhancements.js';
import type { Theme } from './types';

const elementSelector = 'div.mermaid, pre.mermaid';
let mediaQueryList: MediaQueryList | null = null;
let mediaChangeHandler: ((e: MediaQueryListEvent) => void) | null = null;
let darkThemeHandlerRef: (() => Promise<void>) | null = null;
let lightThemeHandlerRef: (() => Promise<void>) | null = null;
let listenersAttached = false;
let htmxIntegrationSetup = false;
let printHandlersSetup = false;
let currentTheme: Theme = 'default';
let renderId = 0;

/**
 * Normalize code fence blocks to Mermaid-compatible format
 * Converts <pre><code class="language-mermaid">...</code></pre> into <pre class="mermaid">...</pre>
 */
const normalizeMermaidCodeFences = () => {
    const codeBlocks = document.querySelectorAll('pre > code.language-mermaid');
    codeBlocks.forEach(code => {
        const pre = code.parentElement;
        if (!pre) return;
        if (pre.classList && pre.classList.contains('mermaid')) {
            // Already normalized
            return;
        }
        const text = code.textContent;
        // Replace inner content with raw text and add class
        pre.textContent = text;
        pre.classList.add('mermaid');
    });
};

/**
 * Load and render Mermaid diagrams with the specified theme
 * @param {string} theme - Theme name ('dark' or 'default')
 */
const loadMermaid = async (theme: Theme): Promise<void> => {
    const renderer = getConfig().renderer;
    if (!renderer && !window.mermaid) {
        console.warn('Mermaid library not found. Make sure mermaid is loaded before calling initMermaid()');
        return;
    }

    try {
        if (renderer) {
            cleanupMermaidEnhancements();
            for (const element of document.querySelectorAll(elementSelector)) {
                const source = element.getAttribute('data-original-code') ?? element.textContent ?? '';
                element.innerHTML = await renderer.render(source, { id: `diagram-${++renderId}`, theme });
                element.setAttribute('data-renderer', renderer.name);
                element.setAttribute('data-diagram-theme', theme);
                element.setAttribute('data-processed', 'true');
            }
        } else {
            window.mermaid!.initialize({
                startOnLoad: false,
                theme,
                // Force transparent background so container styles (light/dark) show through
                themeVariables: {
                    background: 'transparent'
                }
            });

            // Use nodes array instead of querySelector for better mobile/Cloudflare compatibility
            const elements = document.querySelectorAll(elementSelector);
            await window.mermaid!.run({
                nodes: Array.from(elements),
            });
        }

        // Enhance diagrams with pan/zoom and export after rendering completes
        // Use requestAnimationFrame for better timing
        await new Promise<void>(resolve => {
            requestAnimationFrame(() => {
                requestAnimationFrame(() => {
                    enhanceMermaidDiagrams();
                    resolve();
                });
            });
        });
    } catch (err) {
        console.error('Mermaid render error:', err);
    }
};

/**
 * Save original diagram source code for re-rendering
 */
const saveOriginalData = (): void => {
    const elements = document.querySelectorAll(elementSelector);
    if (elements.length === 0) return;

    elements.forEach((el) => {
        if (el.getAttribute('data-processed') != null) return;
        // Store the raw text content so special characters aren't HTML-escaped
        const textContent = el.textContent;
        if (textContent) {
            el.setAttribute('data-original-code', textContent);
        }
    });
};

/**
 * Reset processed diagrams to their original source for re-rendering
 */
const resetProcessed = (): void => {
    const elements = document.querySelectorAll(elementSelector);
    if (elements.length === 0) return;

    elements.forEach((el) => {
        const originalCode = el.getAttribute('data-original-code');
        if (originalCode != null) {
            el.removeAttribute('data-processed');
            el.textContent = originalCode;
        }
    });
};

/**
 * Initialize Mermaid with automatic theme switching support
 *
 * This function:
 * - Normalizes code fence blocks
 * - Sets up theme change listeners
 * - Handles OS-level theme preferences
 * - Re-renders diagrams when theme changes
 * - Applies enhancements (pan/zoom, export, fullscreen)
 *
 * @returns {Promise<void>}
 *
 * @example
 * // Basic usage
 * import { initMermaid } from '@mostlylucid/mermaid-enhancements';
 *
 * document.addEventListener('DOMContentLoaded', async () => {
 *   await initMermaid();
 * });
 *
 * @example
 * // With custom theme switching events
 * import { initMermaid } from '@mostlylucid/mermaid-enhancements';
 *
 * await initMermaid();
 *
 * // Trigger theme change
 * document.body.dispatchEvent(new Event('dark-theme-set'));
 * document.body.dispatchEvent(new Event('light-theme-set'));
 */
export async function initMermaid() {
    // Normalize any code fences first so they get picked up ASAP
    normalizeMermaidCodeFences();

    const mermaidElements = document.querySelectorAll(elementSelector);
    if (mermaidElements.length === 0) return;

    // Save original source early to avoid losing raw content if anything else touches the DOM
    try {
        saveOriginalData();
    } catch (error) {
        console.error('Error saving original data:', error);
        return;
    }

    // If something else pre-processed diagrams before our init, reset them to original code
    const preProcessedExists = Array.from(mermaidElements).some(el => el.getAttribute('data-processed') != null);
    if (preProcessedExists) {
        resetProcessed();
    }

    // Always give the page a tiny chance to apply theme classes before we detect theme
    // This ensures correct initial theme even when classes are toggled very early in page load
    await new Promise(resolve => requestAnimationFrame(() => requestAnimationFrame(resolve)));

    const handleDarkThemeSet = async (): Promise<void> => {
        try {
            currentTheme = 'dark';
            resetProcessed();
            await loadMermaid('dark');
        } catch (error) {
            console.error('Error during dark theme set:', error);
        }
    };

    const handleLightThemeSet = async (): Promise<void> => {
        try {
            currentTheme = 'default';
            resetProcessed();
            await loadMermaid('default');
        } catch (error) {
            console.error('Error during light theme set:', error);
        }
    };

    // Remove previous listeners using saved references (prevents duplicates)
    if (listenersAttached) {
        if (darkThemeHandlerRef) document.body.removeEventListener('dark-theme-set', darkThemeHandlerRef);
        if (lightThemeHandlerRef) document.body.removeEventListener('light-theme-set', lightThemeHandlerRef);
    }

    darkThemeHandlerRef = handleDarkThemeSet;
    lightThemeHandlerRef = handleLightThemeSet;

    // Listen for custom theme change events
    document.body.addEventListener('dark-theme-set', darkThemeHandlerRef);
    document.body.addEventListener('light-theme-set', lightThemeHandlerRef);
    listenersAttached = true;

    // OS theme change listener
    try {
        if (typeof window.matchMedia === 'function') {
            // Remove previous
            if (mediaQueryList && mediaChangeHandler) {
                mediaQueryList.removeEventListener('change', mediaChangeHandler);
            }
            mediaQueryList = window.matchMedia('(prefers-color-scheme: dark)');
            mediaChangeHandler = async (e: MediaQueryListEvent): Promise<void> => {
                try {
                    resetProcessed();
                    await loadMermaid(e.matches ? 'dark' : 'default');
                } catch (err) {
                    console.error('Error handling OS theme change:', err);
                }
            };
            mediaQueryList.addEventListener('change', mediaChangeHandler);
        }
    } catch (e) {
        // Non-fatal in test environments
    }

    // Detect current theme with multiple fallbacks
    let isDarkMode = false;
    let detectionMethod = 'default';

    // Check global state (if set by your theme system)
    if (typeof window.__themeState !== 'undefined') {
        isDarkMode = window.__themeState === 'dark';
        detectionMethod = 'window.__themeState';
    }
    // Check localStorage
    else if (typeof localStorage !== 'undefined' && localStorage.theme) {
        isDarkMode = localStorage.theme === 'dark';
        detectionMethod = 'localStorage.theme';
    }
    // Check DOM class on documentElement or body
    else if (document.documentElement.classList.contains('dark') || document.body.classList.contains('dark')) {
        isDarkMode = true;
        detectionMethod = document.documentElement.classList.contains('dark') ? 'documentElement.classList' : 'body.classList';
    }
    // Check OS preference
    else if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        isDarkMode = true;
        detectionMethod = 'prefers-color-scheme';
    }

    console.log(`[Mermaid Theme] Detected ${isDarkMode ? 'dark' : 'light'} mode via ${detectionMethod}`);
    currentTheme = isDarkMode ? 'dark' : 'default';
    await loadMermaid(currentTheme);

    // Setup HTMX integration if HTMX is available
    setupHtmxIntegration();

    // Setup print handlers to force light theme when printing
    setupPrintHandlers();
}

/**
 * Setup HTMX integration to automatically enhance diagrams after content swaps
 * @private
 */
function setupHtmxIntegration(): void {
    // Only set up once
    if (htmxIntegrationSetup) return;

    // Check if HTMX is available
    if (typeof document.body.addEventListener === 'undefined') return;

    // Listen for HTMX afterSettle event
    document.body.addEventListener('htmx:afterSettle', async (event: Event) => {
        console.log('[Mermaid Theme] HTMX afterSettle detected, re-initializing diagrams');

        // Re-initialize Mermaid for any new diagrams in the swapped content
        const detail = (event as CustomEvent).detail;
        const target = detail?.target || document.body;

        // Check if there are new Mermaid diagrams in the swapped content
        const newDiagrams = target.querySelectorAll?.(elementSelector);
        if (newDiagrams && newDiagrams.length > 0) {
            await initMermaid();
        }
    });

    htmxIntegrationSetup = true;
    console.log('[Mermaid Theme] HTMX integration enabled');
}

/**
 * Setup print handlers to force light theme when printing
 * @private
 */
function setupPrintHandlers(): void {
    // Only set up once
    if (printHandlersSetup) return;

    // Check if print events are available
    if (typeof window === 'undefined') return;

    // Handle before print - switch to light theme
    const handleBeforePrint = async (): Promise<void> => {
        console.log('[Mermaid Theme] Print mode detected, switching to light theme');
        try {
            resetProcessed();
            await loadMermaid('default');
        } catch (error) {
            console.error('Error switching to light theme for print:', error);
        }
    };

    // Handle after print - restore original theme
    const handleAfterPrint = async (): Promise<void> => {
        console.log('[Mermaid Theme] Print complete, restoring original theme');
        try {
            resetProcessed();
            await loadMermaid(currentTheme);
        } catch (error) {
            console.error('Error restoring theme after print:', error);
        }
    };

    // Add print event listeners
    window.addEventListener('beforeprint', handleBeforePrint);
    window.addEventListener('afterprint', handleAfterPrint);

    // Handle CSS-based print media query for browsers that don't fire print events
    if (typeof window.matchMedia === 'function') {
        const printMediaQuery = window.matchMedia('print');
        printMediaQuery.addEventListener('change', async (e: MediaQueryListEvent) => {
            if (e.matches) {
                await handleBeforePrint();
            } else {
                await handleAfterPrint();
            }
        });
    }

    printHandlersSetup = true;
    console.log('[Mermaid Theme] Print handlers enabled');
}
