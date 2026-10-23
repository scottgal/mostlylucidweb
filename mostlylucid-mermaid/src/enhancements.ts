/**
 * Mermaid Diagram Enhancements
 *
 * Provides SVG pan/zoom and export functionality for Mermaid diagrams
 *
 * @module enhancements
 */

import svgPanZoom from 'svg-pan-zoom';
import { toPng } from 'html-to-image';
import type { PanZoomInstance, ExportFormat, EnhancementConfig, IconConfig } from './types';

const panZoomInstances = new Map<string, PanZoomInstance>();
const diagramElements = new Map<string, SVGElement>();
const resizeObservers = new Map<string, ResizeObserver>();
const fitTimers = new Map<string, ReturnType<typeof setTimeout>>();
const closeLightboxes = new Map<string, () => void>();
let cloneId = 0;

// A render or navigation can destroy an instance before its delayed layout pass.
function destroyInstance(id: string): void {
    clearTimeout(fitTimers.get(id));
    fitTimers.delete(id);
    resizeObservers.get(id)?.disconnect();
    resizeObservers.delete(id);
    try { panZoomInstances.get(id)?.destroy(); }
    catch (error) { console.warn('Failed to destroy pan-zoom instance:', error); }
    panZoomInstances.delete(id);
    const svg = diagramElements.get(id);
    if (svg) { svg.style.touchAction = 'pan-y pinch-zoom'; svg.dataset.interactive = 'false'; }
    diagramElements.delete(id);
}
function fitAfterLayout(svg: SVGElement, id: string, instance: PanZoomInstance): void {
    clearTimeout(fitTimers.get(id));
    fitTimers.set(id, setTimeout(() => {
        fitTimers.delete(id);
        if (!svg.isConnected || panZoomInstances.get(id) !== instance) return;
        instance.resize(); instance.fit(); instance.center();
    }, 100));
}

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
            ...globalConfig.icons,
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
    container.querySelector('.mermaid-show-toolbar')?.remove();
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
        button.type = 'button';
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
    destroyInstance(diagramId);

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
        diagramElements.set(diagramId, svgElement);
        if (typeof ResizeObserver !== 'undefined') {
            const observer = new ResizeObserver(() => {
                if (!svgElement.isConnected || panZoomInstances.get(diagramId) !== panZoomInstance) return;
                if (panZoomInstance.isPanEnabled()) panZoomInstance.resize();
                else fitAfterLayout(svgElement, diagramId, panZoomInstance);
            });
            observer.observe(svgElement);
            resizeObservers.set(diagramId, observer);
        }
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
                    (showButton as HTMLButtonElement).type = 'button';
                    showButton.className = 'mermaid-show-toolbar bx bx-chevron-left';
                    showButton.setAttribute('title', 'Show Toolbar');
                    showButton.setAttribute('aria-label', 'Show Toolbar');
                    showButton.setAttribute('data-diagram-id', diagramId);
                    showButton.addEventListener('click', () => {
                        container.querySelector('.mermaid-controls')?.classList.remove('hidden-slide');
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
            closeLightboxes.get(diagramId)?.();
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
                    panZoomInstance.disableZoom();
                    panZoomInstance.disableMouseWheelZoom();
                    panZoomInstance.disableDblClickZoom();
                    button.classList.remove('active');
                    button.setAttribute('title', 'Enable Pan & Zoom');
                    button.setAttribute('aria-label', 'Enable Pan & Zoom');
                    button.setAttribute('aria-pressed', 'false');
                } else {
                    // Enable pan and zoom (including touch)
                    panZoomInstance.enablePan();
                    const svg = container?.querySelector('svg');
                    if (svg) { svg.style.touchAction = 'none'; svg.dataset.interactive = 'true'; }
                    panZoomInstance.enableZoom();
                    panZoomInstance.enableMouseWheelZoom();
                    panZoomInstance.enableDblClickZoom();
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
    const properties = ['fill', 'stroke', 'stroke-width', 'color', 'font-family', 'font-size', 'font-weight', 'font-style', 'opacity', 'text-anchor', 'dominant-baseline', 'marker-start', 'marker-mid', 'marker-end', 'stroke-dasharray', 'display', 'line-height', 'margin', 'padding', 'white-space', 'text-align', 'letter-spacing', 'word-spacing', 'box-sizing'];
    originals.forEach((element, index) => {
        const style = getComputedStyle(element);
        const target = copies[index] as SVGElement;
        for (const property of properties) {
            const value = style.getPropertyValue(property);
            if (value && target.style) target.style.setProperty(property, value);
        }
    });
    if (source.dataset.originalViewbox) clone.setAttribute('viewBox', source.dataset.originalViewbox);
    const viewport = clone.querySelector<SVGElement>('.svg-pan-zoom_viewport');
    viewport?.removeAttribute('transform');
    viewport?.style.removeProperty('transform');
    viewport?.style.removeProperty('transform-origin');
    // A fullscreen copy shares a document with its source. Keep marker/gradient IDs distinct.
    const ids = new Map<string, string>();
    [clone, ...Array.from(clone.querySelectorAll('[id]'))].filter(element => element.id).forEach(element => {
        const old = element.id, next = `diagram-copy-${cloneId}-${old}`;
        ids.set(old, next); element.id = next;
    });
    cloneId++;
    // Mermaid scopes its embedded stylesheet to the root SVG ID. Keep those selectors valid.
    clone.querySelectorAll('style').forEach(style => {
        let css = style.textContent ?? '';
        for (const [old, next] of Array.from(ids).sort((a,b) => b[0].length - a[0].length)) {
            css = css.split(`#${old}`).join(`#${next}`);
        }
        style.textContent = css;
    });
    [clone, ...Array.from(clone.querySelectorAll('*'))].forEach(element => {
        for (const attribute of Array.from(element.attributes)) {
            let value = attribute.value;
            // Computed paints may turn local URLs into absolute document URLs.
            value = value.replace(/url\(["']?([^"')]+)["']?\)/g, (whole, target: string) => {
                const fragment = target.slice(target.lastIndexOf('#') + 1);
                return ids.has(fragment) ? `url(#${ids.get(fragment)})` : whole;
            });
            if (attribute.name === 'href' || attribute.name === 'xlink:href') {
                const next = ids.get(value.replace(/^#/, ''));
                if (next) value = `#${next}`;
            }
            element.setAttribute(attribute.name, value);
        }
    });
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

    // One modal at a time. Every close route uses the same timer/listener cleanup.
    closeLightboxes.forEach(close => close());
    const opener = document.activeElement as HTMLElement | null;
    const previousOverflow = document.body.style.overflow;
    lightbox.setAttribute('role', 'dialog');
    lightbox.setAttribute('aria-modal', 'true');
    lightbox.setAttribute('aria-label', 'Diagram fullscreen view');
    lightbox.tabIndex = -1;
    document.body.appendChild(lightbox);
    document.body.style.overflow = 'hidden';
    let closed = false;
    const close = () => {
        if (closed) return;
        closed = true;
        clearTimeout(openTimer);
        destroyInstance(lightboxDiagramId);
        document.removeEventListener('keydown', keyHandler);
        closeLightboxes.delete(lightboxDiagramId);
        lightbox.remove();
        document.body.style.overflow = previousOverflow;
        if (opener?.isConnected) opener.focus();
    };
    const keyHandler = (event: KeyboardEvent) => {
        if (event.key === 'Escape') { event.preventDefault(); close(); }
        if (event.key === 'Tab') {
            const buttons = Array.from(lightbox.querySelectorAll<HTMLButtonElement>('button')).filter(button => getComputedStyle(button).display !== 'none');
            const first = buttons[0], last = buttons[buttons.length - 1];
            if (!first) { event.preventDefault(); lightbox.focus(); return; }
            if (event.shiftKey && (document.activeElement === first || document.activeElement === lightbox)) { event.preventDefault(); last.focus(); }
            else if (!event.shiftKey && (document.activeElement === last || document.activeElement === lightbox)) { event.preventDefault(); first.focus(); }
        }
    };
    const openTimer = setTimeout(() => {
        if (!lightbox.isConnected || closed) return;
        const instance = initPanZoom(clonedSvg, lightboxDiagramId);
        if (instance) fitAfterLayout(clonedSvg, lightboxDiagramId, instance);
    }, 100);
    closeLightboxes.set(lightboxDiagramId, close);
    document.addEventListener('keydown', keyHandler);
    lightbox.addEventListener('click', event => { if (event.target === lightbox) close(); });
    (lightbox.querySelector<HTMLButtonElement>('[data-action="close"]') ?? lightbox).focus();
}

/**
 * Export diagram as PNG or SVG
 * @param {HTMLElement} container - The diagram container
 * @param {string} format - Export format ('png' or 'svg')
 * @param {string} diagramId - Unique ID for the diagram
 */
async function exportDiagram(container: HTMLElement, format: ExportFormat, diagramId: string): Promise<void> {
    let tempDiv: HTMLDivElement | undefined;
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
        const [, , vbWidth, vbHeight] = viewBox.trim().split(/[\s,]+/).map(Number);

        // Set explicit dimensions based on viewBox for proper export
        clonedSvg.setAttribute('width', String(vbWidth));
        clonedSvg.setAttribute('height', String(vbHeight));

        // Remove inline styles (pan-zoom transforms) but keep viewBox
        clonedSvg.style.removeProperty('transform');
        clonedSvg.style.backgroundColor = 'transparent';
        clonedSvg.style.maxWidth = 'none';

        // Create a temporary container
        tempDiv = document.createElement('div');
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
            clonedSvg.setAttribute('xmlns', 'http://www.w3.org/2000/svg');
            dataUrl = `data:image/svg+xml;charset=utf-8,${encodeURIComponent(new XMLSerializer().serializeToString(clonedSvg))}`;
            downloadFile(dataUrl, `${filename}.svg`);
        }

        // Clean up
        document.body.removeChild(tempDiv);

        console.log(`Diagram exported as ${format.toUpperCase()}`);
    } catch (error) {
        console.error('Failed to export diagram:', error);
    } finally {
        tempDiv?.remove();
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
            fitAfterLayout(svgElement, diagramId, panZoom);

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
    closeLightboxes.forEach(close => close());
    for (const id of panZoomInstances.keys()) destroyInstance(id);
    for (const timer of fitTimers.values()) clearTimeout(timer);
    fitTimers.clear();
}

// Filter by values instead of interpolating a caller's ID into a CSS selector.
function toolbars(diagramId?: string): HTMLElement[] {
    return Array.from(document.querySelectorAll<HTMLElement>('.mermaid-controls')).filter(toolbar =>
        !diagramId || toolbar.querySelector<HTMLElement>('[data-diagram-id]')?.dataset.diagramId === diagramId);
}
function setToolbarVisibility(toolbar: HTMLElement, visible: boolean): void {
    toolbar.style.display = visible ? 'flex' : 'none';
    if (visible) toolbar.classList.remove('hidden-slide');
    const showButton = toolbar.parentElement?.querySelector<HTMLElement>('.mermaid-show-toolbar');
    if (showButton) showButton.style.display = 'none';
}

/** Hide the toolbar for one generated diagram ID, or every toolbar when omitted. */
export function hideToolbar(diagramId?: string): void {
    toolbars(diagramId).forEach(toolbar => setToolbarVisibility(toolbar, false));
}

/** Show toolbars, restoring any toolbar previously slid away with its hide button. */
export function showToolbar(diagramId?: string): void {
    toolbars(diagramId).forEach(toolbar => setToolbarVisibility(toolbar, true));
}

/** Toggle one toolbar or every toolbar. Hidden slide state counts as hidden. */
export function toggleToolbar(diagramId?: string): void {
    toolbars(diagramId).forEach(toolbar => setToolbarVisibility(toolbar,
        toolbar.style.display === 'none' || toolbar.classList.contains('hidden-slide')));
}
