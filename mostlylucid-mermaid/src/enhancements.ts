/**
 * Mermaid Diagram Enhancements
 *
 * Provides SVG pan/zoom and export functionality for Mermaid diagrams
 *
 * @module enhancements
 */

import svgPanZoom from 'svg-pan-zoom';
import { toPng, toSvg } from 'html-to-image';
import type { PanZoomInstance, ExportFormat, EnhancementConfig, IconConfig } from './types';

const panZoomInstances = new Map<string, PanZoomInstance>();

/**
 * Default icon configuration (Boxicons)
 */
const defaultIcons: Required<IconConfig> = {
    fullscreen: 'bx bx-fullscreen',
    zoomIn: 'bx bx-zoom-in',
    zoomOut: 'bx bx-zoom-out',
    reset: 'bx bx-reset',
    pan: 'bx bx-move',
    exportPng: 'bx bx-image',
    exportSvg: 'bx bx-code-alt',
    hide: 'bx bx-chevron-right'
};

/**
 * Global configuration
 */
let globalConfig: EnhancementConfig = {
    icons: defaultIcons,
    controls: {
        fullscreen: true,
        zoom: true,
        pan: true,
        export: true
    }
};

/**
 * Configure mermaid enhancements
 * @param config - Configuration options
 *
 * @example
 * // Use Font Awesome icons
 * configure({
 *   icons: {
 *     fullscreen: 'fas fa-expand',
 *     zoomIn: 'fas fa-plus',
 *     zoomOut: 'fas fa-minus',
 *     reset: 'fas fa-undo',
 *     pan: 'fas fa-hand-paper',
 *     exportPng: 'fas fa-image',
 *     exportSvg: 'fas fa-code'
 *   }
 * });
 *
 * @example
 * // Disable some controls
 * configure({
 *   controls: {
 *     export: false
 *   }
 * });
 */
export function configure(config: EnhancementConfig): void {
    globalConfig = {
        ...globalConfig,
        ...config,
        icons: {
            ...defaultIcons,
            ...config.icons
        },
        controls: {
            ...globalConfig.controls,
            ...config.controls
        },
        htmx: {
            ...globalConfig.htmx,
            ...config.htmx
        }
    };
}

/**
 * Get current configuration
 * @returns Current enhancement configuration
 * @private
 */
export function getConfig(): EnhancementConfig {
    return globalConfig;
}

/**
 * Create control buttons for a Mermaid diagram
 * @param {HTMLElement} container - The container element for the diagram
 * @param {string} diagramId - Unique ID for the diagram
 * @param {boolean} isLightbox - Whether this is for a lightbox (adds close button)
 */
function createControlButtons(container: HTMLElement, diagramId: string, isLightbox: boolean = false): void {
    // Remove existing controls if they exist (for reconfiguration)
    const existingControls = container.querySelector('.mermaid-controls');
    if (existingControls) {
        existingControls.remove();
    }

    const controls = globalConfig.controls || {};

    // Check if controls should be hidden entirely
    if (controls.showControls === false) {
        return; // Don't create any controls
    }

    const controlsDiv = document.createElement('div');
    controlsDiv.className = 'mermaid-controls';

    const icons = globalConfig.icons || defaultIcons;

    // Helper to check if a button is enabled (supports both grouped and granular config)
    const isEnabled = (action: string, groupKey?: string): boolean => {
        // Check granular control first
        const granularKey = action as keyof typeof controls;
        if (controls[granularKey] !== undefined) {
            return controls[granularKey] === true;
        }
        // Fall back to grouped control
        if (groupKey && controls[groupKey as keyof typeof controls] !== undefined) {
            return controls[groupKey as keyof typeof controls] !== false;
        }
        // Default to enabled
        return true;
    };

    const buttons: Array<{ icon: string; title: string; action: string; enabled?: boolean }> = [];

    // Add buttons (close button will be added last to be on far right)
    buttons.push(
        { icon: icons.hide!, title: 'Hide Toolbar', action: 'hideToolbar', enabled: true },
        { icon: icons.fullscreen!, title: 'Fullscreen', action: 'fullscreen', enabled: isEnabled('fullscreen') && !isLightbox },
        { icon: icons.zoomIn!, title: 'Zoom In', action: 'zoomIn', enabled: isEnabled('zoomIn', 'zoom') },
        { icon: icons.zoomOut!, title: 'Zoom Out', action: 'zoomOut', enabled: isEnabled('zoomOut', 'zoom') },
        { icon: icons.reset!, title: 'Reset View', action: 'reset', enabled: isEnabled('reset', 'zoom') },
        { icon: icons.pan!, title: 'Enable Pan & Zoom', action: 'pan', enabled: isEnabled('pan') },
        { icon: icons.exportPng!, title: 'Export as PNG', action: 'exportPng', enabled: isEnabled('exportPng', 'export') },
        { icon: icons.exportSvg!, title: 'Export as SVG', action: 'exportSvg', enabled: isEnabled('exportSvg', 'export') }
    );

    // Add close button last if this is a lightbox (so it appears on far right)
    if (isLightbox) {
        buttons.push({ icon: 'bx bx-x', title: 'Close', action: 'close', enabled: true });
    }

    buttons.forEach(btn => {
        if (btn.enabled === false) return;

        const button = document.createElement('button');
        button.className = `mermaid-control-btn ${btn.icon}`;
        button.setAttribute('title', btn.title);
        button.setAttribute('aria-label', btn.title);
        button.setAttribute('data-action', btn.action);
        button.setAttribute('data-diagram-id', diagramId);
        if (btn.action === 'pan') button.setAttribute('aria-pressed', 'false');

        // Pan button starts as inactive (pan/zoom disabled by default)
        // User must click it to enable pan/zoom functionality

        controlsDiv.appendChild(button);
    });

    container.appendChild(controlsDiv);
}

/**
 * Initialize SVG pan/zoom for a diagram
 * @param {HTMLElement} svgElement - The SVG element
 * @param {string} diagramId - Unique ID for the diagram
 * @returns {Object|null} Pan-zoom instance or null on failure
 */
function initPanZoom(svgElement: SVGElement, diagramId: string): PanZoomInstance | null {
    svgElement.style.touchAction = 'pan-y pinch-zoom';
    svgElement.dataset.interactive = 'false';
    // Clean up existing instance if present
    if (panZoomInstances.has(diagramId)) {
        try {
            panZoomInstances.get(diagramId)?.destroy();
        } catch (e) {
            console.warn('Failed to destroy existing pan-zoom instance:', e);
        }
        panZoomInstances.delete(diagramId);
    }

    try {
        const panZoomInstance = svgPanZoom(svgElement, {
            zoomEnabled: false, // Disabled by default - requires pan button click
            controlIconsEnabled: false, // We use custom controls
            fit: true,
            center: true,
            minZoom: 0.1,
            maxZoom: 10,
            zoomScaleSensitivity: 0.3,
            dblClickZoomEnabled: false, // Disabled by default
            mouseWheelZoomEnabled: false, // Disabled by default - requires pan button click
            preventMouseEventsDefault: false,
            contain: false,
            // Disable pan by default - requires pan button click
            panEnabled: false,
            eventsListenerElement: svgElement
        });

        panZoomInstances.set(diagramId, panZoomInstance);
        return panZoomInstance;
    } catch (error) {
        console.error('Failed to initialize pan-zoom:', error);
        return null;
    }
}

/**
 * Handle control button clicks
 * @param {Event} event - Click event
 */
function handleControlClick(event: Event): void {
    const button = (event.target as Element)?.closest('.mermaid-control-btn');
    if (!button) return;

    const action = button.getAttribute('data-action');
    const diagramId = button.getAttribute('data-diagram-id');
    if (!diagramId) return;
    const panZoomInstance = panZoomInstances.get(diagramId);

    // Find container - could be .mermaid-wrapper (in-page) or .mermaid-lightbox-diagram-wrapper (lightbox)
    const container = button.closest('.mermaid-wrapper') || button.closest('.mermaid-lightbox-diagram-wrapper');

    switch (action) {
        case 'hideToolbar':
            // Slide toolbar off to the right
            const controls = button.closest('.mermaid-controls') as HTMLElement;
            if (controls) {
                controls.classList.add('hidden-slide');

                // Create show button if it doesn't exist
                let showButton = container?.querySelector('.mermaid-show-toolbar') as HTMLElement;
                if (!showButton && container) {
                    showButton = document.createElement('button');
                    showButton.className = 'mermaid-show-toolbar bx bx-chevron-left';
                    showButton.setAttribute('title', 'Show Toolbar');
                    showButton.setAttribute('aria-label', 'Show Toolbar');
                    showButton.setAttribute('data-diagram-id', diagramId);
                    showButton.addEventListener('click', () => {
                        controls.classList.remove('hidden-slide');
                        showButton.style.display = 'none';
                    });
                    container.appendChild(showButton);
                }
                if (showButton) {
                    showButton.style.display = 'flex';
                }
            }
            break;
        case 'close':
            // Close lightbox
            const lightbox = button.closest('.mermaid-lightbox');
            if (lightbox) {
                // Clean up pan-zoom instance
                if (panZoomInstances.has(diagramId)) {
                    try {
                        panZoomInstances.get(diagramId)?.destroy();
                    } catch (e) {
                        console.warn('Failed to destroy lightbox pan-zoom instance:', e);
                    }
                    panZoomInstances.delete(diagramId);
                }
                lightbox.remove();
            }
            break;
        case 'fullscreen':
            // Fullscreen only works for in-page diagrams
            if (container?.classList.contains('mermaid-wrapper')) {
                openFullscreenLightbox(container as HTMLElement, diagramId);
            }
            break;
        case 'zoomIn':
            if (panZoomInstance) panZoomInstance.zoomIn();
            break;
        case 'zoomOut':
            if (panZoomInstance) panZoomInstance.zoomOut();
            break;
        case 'reset':
            if (panZoomInstance) {
                panZoomInstance.reset();
                panZoomInstance.center();
                panZoomInstance.fit();
            }
            break;
        case 'pan':
            // Toggle pan and zoom mode (they work together)
            if (panZoomInstance) {
                const isPanEnabled = panZoomInstance.isPanEnabled();
                if (isPanEnabled) {
                    // Disable pan and zoom
                    panZoomInstance.disablePan();
                    const svg = container?.querySelector('svg');
                    if (svg) { svg.style.touchAction = 'pan-y pinch-zoom'; svg.dataset.interactive = 'false'; }
                    (panZoomInstance as any).disableZoom();
                    (panZoomInstance as any).disableMouseWheelZoom();
                    (panZoomInstance as any).disableDblClickZoom();
                    button.classList.remove('active');
                    button.setAttribute('title', 'Enable Pan & Zoom');
                    button.setAttribute('aria-label', 'Enable Pan & Zoom');
                    button.setAttribute('aria-pressed', 'false');
                } else {
                    // Enable pan and zoom (including touch)
                    panZoomInstance.enablePan();
                    const svg = container?.querySelector('svg');
                    if (svg) { svg.style.touchAction = 'none'; svg.dataset.interactive = 'true'; }
                    (panZoomInstance as any).enableZoom();
                    (panZoomInstance as any).enableMouseWheelZoom();
                    (panZoomInstance as any).enableDblClickZoom();
                    button.classList.add('active');
                    button.setAttribute('title', 'Disable Pan & Zoom');
                    button.setAttribute('aria-label', 'Disable Pan & Zoom');
                    button.setAttribute('aria-pressed', 'true');
                }
            }
            break;
        case 'exportPng':
            if (container) exportDiagram(container as HTMLElement, 'png', diagramId);
            break;
        case 'exportSvg':
            if (container) exportDiagram(container as HTMLElement, 'svg', diagramId);
            break;
    }
}

/** Preserve inherited/CSS-variable paints when moving an SVG outside its themed container. */
function cloneWithPresentation(source: SVGElement): SVGElement {
    const clone = source.cloneNode(true) as SVGElement;
    const originals = [source, ...Array.from(source.querySelectorAll('*'))];
    const copies = [clone, ...Array.from(clone.querySelectorAll('*'))];
    const properties = ['fill', 'stroke', 'stroke-width', 'color', 'font-family', 'font-size', 'font-weight', 'opacity'];
    originals.forEach((element, index) => {
        const style = getComputedStyle(element);
        const target = copies[index] as SVGElement;
        for (const property of properties) {
            const value = style.getPropertyValue(property);
            if (value && target.style) target.style.setProperty(property, value);
        }
    });
    if (source.dataset.originalViewbox) clone.setAttribute('viewBox', source.dataset.originalViewbox);
    clone.querySelector('.svg-pan-zoom_viewport')?.removeAttribute('transform');
    return clone;
}

/**
 * Open diagram in fullscreen lightbox
 * @param {HTMLElement} container - The diagram container
 * @param {string} diagramId - Unique ID for the diagram
 */
function openFullscreenLightbox(container: HTMLElement, diagramId: string): void {
    // Find the SVG element
    const svgElement = container.querySelector('svg');
    if (!svgElement) return;

    // Create lightbox overlay
    const lightbox = document.createElement('div');
    lightbox.className = 'mermaid-lightbox';
    lightbox.innerHTML = `
        <div class="mermaid-lightbox-content">
            <div class="mermaid-lightbox-diagram-wrapper">
                <div class="mermaid-lightbox-diagram"></div>
            </div>
        </div>
    `;

    // Clone the SVG
    const clonedSvg = cloneWithPresentation(svgElement);

    // Remove any inline size constraints
    clonedSvg.removeAttribute('width');
    clonedSvg.removeAttribute('height');
    clonedSvg.style.width = '100%';
    clonedSvg.style.height = '100%';

    const diagramContainer = lightbox.querySelector('.mermaid-lightbox-diagram');
    if (!diagramContainer) return;
    diagramContainer.appendChild(clonedSvg);

    // Add controls to the lightbox wrapper (with close button)
    const wrapper = lightbox.querySelector('.mermaid-lightbox-diagram-wrapper') as HTMLElement;
    if (!wrapper) return;
    const lightboxDiagramId = `${diagramId}-lightbox`;
    createControlButtons(wrapper, lightboxDiagramId, true);

    // Add to body
    document.body.appendChild(lightbox);

    // Wait for layout to complete before initializing pan-zoom
    setTimeout(() => {
        const panZoom = initPanZoom(clonedSvg, lightboxDiagramId);
        if (panZoom) {
            // Force the diagram to fit the container
            panZoom.resize();
            panZoom.fit();
            panZoom.center();
        }
    }, 100);

    // Close on background click
    lightbox.addEventListener('click', (e: Event) => {
        if (e.target === lightbox) {
            // Clean up pan-zoom instance
            if (panZoomInstances.has(lightboxDiagramId)) {
                try {
                    panZoomInstances.get(lightboxDiagramId)?.destroy();
                } catch (e) {
                    console.warn('Failed to destroy lightbox pan-zoom instance:', e);
                }
                panZoomInstances.delete(lightboxDiagramId);
            }
            lightbox.remove();
        }
    });

    // ESC key to close
    const escHandler = (e: KeyboardEvent): void => {
        if (e.key === 'Escape') {
            // Clean up pan-zoom instance
            if (panZoomInstances.has(lightboxDiagramId)) {
                try {
                    panZoomInstances.get(lightboxDiagramId)?.destroy();
                } catch (e) {
                    console.warn('Failed to destroy lightbox pan-zoom instance:', e);
                }
                panZoomInstances.delete(lightboxDiagramId);
            }
            lightbox.remove();
            document.removeEventListener('keydown', escHandler);
        }
    };
    document.addEventListener('keydown', escHandler);
}

/**
 * Export diagram as PNG or SVG
 * @param {HTMLElement} container - The diagram container
 * @param {string} format - Export format ('png' or 'svg')
 * @param {string} diagramId - Unique ID for the diagram
 */
async function exportDiagram(container: HTMLElement, format: ExportFormat, diagramId: string): Promise<void> {
    try {
        // Find the SVG element
        const svgElement = container.querySelector('svg');
        if (!svgElement) {
            console.warn('No diagram found to export');
            return;
        }

        // Clone the SVG to avoid modifying the original
        const clonedSvg = cloneWithPresentation(svgElement);

        // Get the viewBox or calculate from bounding box
        let viewBox = clonedSvg.getAttribute('viewBox');
        if (!viewBox) {
            const bbox = svgElement.getBBox();
            viewBox = `${bbox.x} ${bbox.y} ${bbox.width} ${bbox.height}`;
            clonedSvg.setAttribute('viewBox', viewBox);
        }

        // Parse viewBox to get dimensions
        const [, , vbWidth, vbHeight] = viewBox.split(' ').map(Number);

        // Set explicit dimensions based on viewBox for proper export
        clonedSvg.setAttribute('width', String(vbWidth));
        clonedSvg.setAttribute('height', String(vbHeight));

        // Remove inline styles (pan-zoom transforms) but keep viewBox
        clonedSvg.removeAttribute('style');
        clonedSvg.style.backgroundColor = 'transparent';
        clonedSvg.style.maxWidth = 'none';

        // Create a temporary container
        const tempDiv = document.createElement('div');
        tempDiv.style.position = 'absolute';
        tempDiv.style.left = '-9999px';
        tempDiv.appendChild(clonedSvg);
        document.body.appendChild(tempDiv);

        let dataUrl;
        const timestamp = new Date().toISOString().replace(/[:.]/g, '-');
        const filename = `mermaid-diagram-${timestamp}`;

        if (format === 'png') {
            dataUrl = await toPng(clonedSvg as unknown as HTMLElement, {
                backgroundColor: 'white',
                pixelRatio: 2 // Higher quality
            });
            downloadFile(dataUrl, `${filename}.png`);
        } else {
            dataUrl = await toSvg(clonedSvg as unknown as HTMLElement, {
                backgroundColor: 'transparent'
            });
            downloadFile(dataUrl, `${filename}.svg`);
        }

        // Clean up
        document.body.removeChild(tempDiv);

        console.log(`Diagram exported as ${format.toUpperCase()}`);
    } catch (error) {
        console.error('Failed to export diagram:', error);
    }
}

/**
 * Trigger file download
 * @param {string} dataUrl - Data URL
 * @param {string} filename - Download filename
 */
function downloadFile(dataUrl: string, filename: string): void {
    const link = document.createElement('a');
    link.download = filename;
    link.href = dataUrl;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}

/**
 * Wrap diagram in container with controls
 * @param {HTMLElement} diagramElement - The diagram element
 * @returns {string} Unique diagram ID
 */
function wrapDiagramWithControls(diagramElement: HTMLElement): string {
    // Check if already wrapped
    const existingWrapper = diagramElement.closest('.mermaid-wrapper');
    if (existingWrapper) {
        const diagramId = existingWrapper.getAttribute('data-diagram-id') || '';
        // Recreate controls with new configuration
        createControlButtons(existingWrapper as HTMLElement, diagramId);
        return diagramId;
    }

    const diagramId = `mermaid-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;

    const wrapper = document.createElement('div');
    wrapper.className = 'mermaid-wrapper';
    wrapper.setAttribute('data-diagram-id', diagramId);

    // Insert wrapper before the diagram
    const parentNode = diagramElement.parentNode;
    if (!parentNode) return diagramId;
    parentNode.insertBefore(wrapper, diagramElement);

    // Move diagram into wrapper
    wrapper.appendChild(diagramElement);

    // Create controls
    createControlButtons(wrapper, diagramId);

    return diagramId;
}

/**
 * Enhance all Mermaid diagrams on the page
 * Adds pan/zoom, fullscreen, and export capabilities
 *
 * @example
 * import { enhanceMermaidDiagrams } from '@mostlylucid/mermaid-enhancements';
 *
 * // After Mermaid has rendered diagrams
 * enhanceMermaidDiagrams();
 */
export function enhanceMermaidDiagrams() {
    const diagrams = document.querySelectorAll('.mermaid[data-processed="true"]');

    diagrams.forEach(diagram => {
        const svgElement = diagram.querySelector('svg');
        if (!svgElement) return;

        // svg-pan-zoom manages the viewport. Keep the source bounds for fullscreen and clean exports.
        if (!svgElement.dataset.originalViewbox && svgElement.getAttribute('viewBox')) {
            svgElement.dataset.originalViewbox = svgElement.getAttribute('viewBox')!;
        }

        svgElement.style.touchAction = 'pan-y pinch-zoom';
        svgElement.dataset.interactive = 'false';

        // Remove inline max-width constraint that Mermaid adds
        svgElement.style.maxWidth = 'none';

        // Wrap diagram with controls
        const diagramId = wrapDiagramWithControls(diagram as HTMLElement);

        // Initialize pan/zoom and fit to view
        const panZoom = initPanZoom(svgElement, diagramId);
        if (panZoom) {
            // Fit the diagram to the container by default so entire diagram is visible
            setTimeout(() => {
                panZoom.resize();
                panZoom.fit();
                panZoom.center();
            }, 100);

            // Explicit toolbar activation only: synthetic clicks after touch scrolling must never arm gestures.

        }
    });

    // Set up event delegation for control buttons (only once)
    if (!document.body.hasAttribute('data-mermaid-controls-initialized')) {
        document.body.addEventListener('click', handleControlClick);
        document.body.setAttribute('data-mermaid-controls-initialized', 'true');
    }
}

/**
 * Clean up all pan-zoom instances
 * Call this before re-rendering diagrams or unmounting
 *
 * @example
 * import { cleanupMermaidEnhancements } from '@mostlylucid/mermaid-enhancements';
 *
 * // Before navigation or cleanup
 * cleanupMermaidEnhancements();
 */
export function cleanupMermaidEnhancements() {
    panZoomInstances.forEach((instance, id) => {
        try {
            instance.destroy();
        } catch (e) {
            console.warn(`Failed to destroy pan-zoom instance ${id}:`, e);
        }
    });
    panZoomInstances.clear();
}

/**
 * Hide toolbar for a specific diagram or all diagrams
 * @param diagramId - Optional diagram ID. If omitted, hides all toolbars
 *
 * @example
 * // Hide specific diagram's toolbar
 * hideToolbar('mermaid-123');
 *
 * @example
 * // Hide all toolbars
 * hideToolbar();
 */
export function hideToolbar(diagramId?: string): void {
    if (diagramId) {
        const wrapper = document.querySelector(`[data-diagram-id="${diagramId}"]`);
        const controls = wrapper?.querySelector('.mermaid-controls');
        if (controls) {
            (controls as HTMLElement).style.display = 'none';
        }
    } else {
        document.querySelectorAll('.mermaid-controls').forEach(controls => {
            (controls as HTMLElement).style.display = 'none';
        });
    }
}

/**
 * Show toolbar for a specific diagram or all diagrams
 * @param diagramId - Optional diagram ID. If omitted, shows all toolbars
 *
 * @example
 * // Show specific diagram's toolbar
 * showToolbar('mermaid-123');
 *
 * @example
 * // Show all toolbars
 * showToolbar();
 */
export function showToolbar(diagramId?: string): void {
    if (diagramId) {
        const wrapper = document.querySelector(`[data-diagram-id="${diagramId}"]`);
        const controls = wrapper?.querySelector('.mermaid-controls');
        if (controls) {
            (controls as HTMLElement).style.display = 'flex';
        }
    } else {
        document.querySelectorAll('.mermaid-controls').forEach(controls => {
            (controls as HTMLElement).style.display = 'flex';
        });
    }
}

/**
 * Toggle toolbar visibility for a specific diagram or all diagrams
 * @param diagramId - Optional diagram ID. If omitted, toggles all toolbars
 *
 * @example
 * // Toggle specific diagram's toolbar
 * toggleToolbar('mermaid-123');
 *
 * @example
 * // Toggle all toolbars
 * toggleToolbar();
 */
export function toggleToolbar(diagramId?: string): void {
    if (diagramId) {
        const wrapper = document.querySelector(`[data-diagram-id="${diagramId}"]`);
        const controls = wrapper?.querySelector('.mermaid-controls') as HTMLElement;
        if (controls) {
            controls.style.display = controls.style.display === 'none' ? 'flex' : 'none';
        }
    } else {
        document.querySelectorAll('.mermaid-controls').forEach(controls => {
            const el = controls as HTMLElement;
            el.style.display = el.style.display === 'none' ? 'flex' : 'none';
        });
    }
}
