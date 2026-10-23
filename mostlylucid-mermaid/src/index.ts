/**
 * @mostlylucid/mermaid-enhancements
 *
 * Enhance Mermaid.js diagrams with:
 * - Interactive SVG pan and zoom
 * - Fullscreen lightbox view
 * - Export to PNG/SVG
 * - Automatic theme switching (light/dark)
 * - Responsive design
 *
 * @module @mostlylucid/mermaid-enhancements
 */

export {
    enhanceMermaidDiagrams,
    cleanupMermaidEnhancements,
    configure,
    hideToolbar,
    showToolbar,
    toggleToolbar
} from './enhancements.js';

export {
    initMermaid
} from './theme-switcher.js';

export { createPeleRenderer } from './renderers.js';

export type {
    EnhancementConfig,
    DiagramRenderer,
    IconConfig,
    PanZoomInstance,
    Theme,
    ExportFormat,
    ControlAction,
    ControlButton
} from './types';

// Re-export for convenience
import { enhanceMermaidDiagrams, configure, hideToolbar, showToolbar, toggleToolbar } from './enhancements.js';
import { initMermaid } from './theme-switcher.js';

/**
 * Initialize Mermaid with all enhancements
 * This is the main entry point that most users will want to call
 *
 * @returns {Promise<void>}
 *
 * @example
 * import { init } from '@mostlylucid/mermaid-enhancements';
 *
 * // After Mermaid is loaded
 * await init();
 */
export async function init() {
    await initMermaid();
}

// Default export for convenience
export default {
    init,
    initMermaid,
    enhanceMermaidDiagrams,
    configure,
    hideToolbar,
    showToolbar,
    toggleToolbar,
};
