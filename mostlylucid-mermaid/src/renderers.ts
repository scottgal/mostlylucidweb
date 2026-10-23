import type { DiagramRenderer } from './types';

/**
 * Adapt Pele without making it a dependency of the enhancements package.
 * Pass `(source, options) => render(source, options)` from the installed Pele version.
 * A fresh SVG idPrefix prevents gradients/markers colliding across diagrams.
 * CSS variables supply colours; the adapter does not translate Mermaid themeVariables.
 */
export function createPeleRenderer(render: (
    source: string, options: { idPrefix: string; responsive: boolean }
) => { svg: string }): DiagramRenderer {
    return {
        name: 'pele',
        render: (source, context) => render(source, { idPrefix: context.id, responsive: false }).svg,
    };
}
