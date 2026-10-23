/**
 * Type definitions for @mostlylucid/mermaid-enhancements
 */

/**
 * SVG Pan Zoom instance interface
 */
export interface PanZoomInstance {
    zoom(scale: number): void;
    zoomIn(): void;
    zoomOut(): void;
    reset(): void;
    fit(): void;
    center(): void;
    resize(): void;
    destroy(): void;
    isPanEnabled(): boolean;
    enablePan(): void;
    disablePan(): void;
    enableZoom(): void;
    disableZoom(): void;
    enableMouseWheelZoom(): void;
    disableMouseWheelZoom(): void;
    enableDblClickZoom(): void;
    disableDblClickZoom(): void;
}

/**
 * Export format type
 */
export type ExportFormat = 'png' | 'svg';

/**
 * Theme type
 */
export type Theme = 'dark' | 'default';

/** Rendering boundary: enhancements work with the SVG rather than a renderer's internal classes. */
export interface DiagramRenderer {
    /** Stable renderer name exposed on the diagram container for scoped CSS. */
    name: string;
    /** Produce SVG from the original source. The host owns and loads the renderer dependency. */
    render(source: string, context: { id: string; theme: Theme }): string | Promise<string>;
}

/**
 * Control button action types
 */
export type ControlAction = 'fullscreen' | 'zoomIn' | 'zoomOut' | 'reset' | 'pan' | 'exportPng' | 'exportSvg' | 'hideToolbar' | 'close';

/**
 * Control button configuration
 */
export interface ControlButton {
    icon: string;
    title: string;
    action: ControlAction;
}

/**
 * Icon configuration for control buttons
 * Allows customization of icon classes (e.g., Boxicons, Font Awesome, etc.)
 */
export interface IconConfig {
    fullscreen?: string;
    zoomIn?: string;
    zoomOut?: string;
    reset?: string;
    pan?: string;
    exportPng?: string;
    exportSvg?: string;
    hide?: string;
}

/**
 * Enhancement configuration options
 */
export interface EnhancementConfig {
    /** Opt-in SVG renderer. Omit to keep the existing window.mermaid integration. */
    renderer?: DiagramRenderer;
    /**
     * Icon classes for control buttons
     * Defaults to Boxicons (bx-*)
     *
     * @example
     * // Using Font Awesome
     * icons: {
     *   fullscreen: 'fas fa-expand',
     *   zoomIn: 'fas fa-plus',
     *   // ...
     * }
     *
     * @example
     * // Using Material Icons
     * icons: {
     *   fullscreen: 'material-icons fullscreen',
     *   zoomIn: 'material-icons zoom_in',
     *   // ...
     * }
     */
    icons?: IconConfig;

    /**
     * Enable/disable specific controls
     * Set to false to hide a control, or provide granular settings
     *
     * @example
     * // Hide entire toolbar
     * controls: {
     *   showControls: false
     * }
     *
     * @example
     * // Simple enable/disable
     * controls: {
     *   fullscreen: false,  // Hide fullscreen button
     *   export: false       // Hide export buttons
     * }
     *
     * @example
     * // Granular control
     * controls: {
     *   fullscreen: true,
     *   zoomIn: true,
     *   zoomOut: true,
     *   reset: false,        // Hide reset button
     *   exportPng: true,
     *   exportSvg: false     // Hide SVG export
     * }
     */
    controls?: {
        showControls?: boolean;  // Show/hide entire toolbar
        fullscreen?: boolean;
        zoom?: boolean;
        pan?: boolean;
        export?: boolean;
        // Granular button controls
        zoomIn?: boolean;
        zoomOut?: boolean;
        reset?: boolean;
        exportPng?: boolean;
        exportSvg?: boolean;
    };

    /**
     * Enable automatic HTMX integration
     * When enabled, automatically re-initializes diagrams after HTMX content swaps
     * Default: false (manual re-initialization required)
     *
     * @example
     * // Enable HTMX auto-enhancement
     * configure({
     *   htmx: {
     *     enabled: true
     *   }
     * });
     */
    htmx?: {
        enabled?: boolean;
    };
}

/**
 * Mermaid initialization options
 */
export interface MermaidOptions {
    startOnLoad: boolean;
    theme: Theme;
    themeVariables?: {
        background?: string;
        [key: string]: any;
    };
    [key: string]: any;
}

/**
 * Global window interface extensions
 */
declare global {
    interface Window {
        mermaid?: {
            initialize(options: MermaidOptions): void;
            run(options?: { querySelector?: string; nodes?: Element[]; suppressErrors?: boolean }): Promise<void>;
        };
        __themeState?: 'dark' | 'light';
        enhanceMermaidDiagrams?: () => void;
        cleanupMermaidEnhancements?: () => void;
        initMermaid?: () => Promise<void>;
    }
}

export {};
