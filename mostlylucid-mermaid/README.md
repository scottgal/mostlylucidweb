# @mostlylucid/mermaid-enhancements

> Enhance Mermaid.js diagrams with interactive pan/zoom, fullscreen lightbox, export capabilities, and automatic theme switching.


[![npm version](https://img.shields.io/npm/v/@mostlylucid/mermaid-enhancements.svg)](https://www.npmjs.com/package/@mostlylucid/mermaid-enhancements)
[![npm downloads](https://img.shields.io/npm/dm/@mostlylucid/mermaid-enhancements.svg)](https://www.npmjs.com/package/@mostlylucid/mermaid-enhancements)
[![License: Unlicense](https://img.shields.io/badge/License-Unlicense-blue.svg)](http://unlicense.org/)
[![TypeScript](https://img.shields.io/badge/TypeScript-Ready-blue.svg)](https://www.typescriptlang.org/)
[![Bundle Size](https://img.shields.io/bundlephobia/minzip/@mostlylucid/mermaid-enhancements)](https://bundlephobia.com/package/@mostlylucid/mermaid-enhancements)


## Features

- **Interactive Pan & Zoom** - Navigate large diagrams with smooth mouse wheel zooming and drag-to-pan
- **Touch Support** - Full touch gesture support (pinch-to-zoom, swipe-to-pan) for mobile devices
- **Fullscreen Lightbox** - View diagrams in immersive fullscreen mode with integrated toolbar
- **Export to PNG/SVG** - Download high-quality diagrams (2x pixel ratio for PNG)
- **Automatic Theme Switching** - Seamlessly adapts to light/dark mode changes
- **Customizable Toolbar** - Show/hide/toggle individual toolbars or all toolbars at once
- **Enhanced Styling** - Theme-appropriate borders, drop shadows, and backdrop blur
- **Mobile/Cloudflare Optimized** - Uses direct DOM node references for better compatibility
- **Responsive Design** - Works perfectly on mobile, tablet, and desktop
- **Keyboard Shortcuts** - Press ESC to close lightbox, double-click to zoom
- **Accessible** - ARIA labels and keyboard navigation support
- **TypeScript Support** - Full type definitions included
- **Zero Configuration** - Works out of the box

## Installation

```bash
npm install @mostlylucid/mermaid-enhancements
```

### Peer Dependencies

This package requires Mermaid.js:

```bash
npm install mermaid
```

## Quick Start

### Basic Usage

```typescript
import mermaid from 'mermaid';
import { init } from '@mostlylucid/mermaid-enhancements';
import '@mostlylucid/mermaid-enhancements/styles.css';

// Initialize Mermaid with enhancements
await init();
```

### Minified Version (Production)

For production use, import the minified version (58% smaller):

```typescript
import { init } from '@mostlylucid/mermaid-enhancements/min';
import '@mostlylucid/mermaid-enhancements/styles.css';

await init();
```

**Size comparison:**
- Regular: 23.46 KB
- Minified: 9.78 KB (58.3% smaller)

### HTML (via CDN)

```html
<!DOCTYPE html>
<html>
<head>
    <!-- Boxicons for control button icons -->
    <link href="https://unpkg.com/boxicons@2.1.4/css/boxicons.min.css" rel="stylesheet">

    <!-- Mermaid Enhancements CSS -->
    <link rel="stylesheet" href="https://unpkg.com/@mostlylucid/mermaid-enhancements/src/styles.css">
</head>
<body>
    <!-- Your Mermaid diagram -->
    <div class="mermaid">
graph TD
    A[Start] --> B{Decision}
    B -->|Yes| C[Result 1]
    B -->|No| D[Result 2]
    </div>

    <script type="module">
        import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
        // Use minified version for production
        import { init } from 'https://unpkg.com/@mostlylucid/mermaid-enhancements/dist/index.min.js';

        await init();
    </script>
</body>
</html>
```

**CDN Options:**

```html
<!-- unpkg (minified, recommended) -->
<script type="module" src="https://unpkg.com/@mostlylucid/mermaid-enhancements"></script>

<!-- jsdelivr (minified, with version pinning) -->
<script type="module" src="https://cdn.jsdelivr.net/npm/@mostlylucid/mermaid-enhancements@1.0.0"></script>

<!-- Direct import -->
<script type="module">
  import { init } from 'https://unpkg.com/@mostlylucid/mermaid-enhancements/dist/index.min.js';
  await init();
</script>
```

## Easy Path

If you just want it to work with the fewest moving parts, use one of these two copy‑paste setups.

### Option A — Single HTML file (CDN, no build tools)

```html
<!doctype html>
<html>
  <head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />

    <!-- Icons for the toolbar buttons (required unless you override icons) -->
    <link href="https://unpkg.com/boxicons@2.1.4/css/boxicons.min.css" rel="stylesheet">

    <!-- Enhancements styles (toolbar, lightbox, etc.) -->
    <link rel="stylesheet" href="https://unpkg.com/@mostlylucid/mermaid-enhancements/src/styles.css">
  </head>
  <body>
    <div class="mermaid">
      graph TD
        A[Start] --> B{Decision}
        B -->|Yes| C[Result 1]
        B -->|No| D[Result 2]
    </div>

    <script type="module">
      // 1) Load Mermaid (v11 or newer)
      import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
      // 2) Load the enhancements (minified build for production)
      import { init } from 'https://unpkg.com/@mostlylucid/mermaid-enhancements/dist/index.min.js';

      // 3) Initialize once the DOM is ready
      window.addEventListener('DOMContentLoaded', async () => {
        // Optional: configure before init()
        // import { configure } from 'https://unpkg.com/@mostlylucid/mermaid-enhancements/dist/index.min.js';
        // configure({ controls: { zoomOut: false } });

        await init();
      });
    </script>
  </body>
</html>
```

Notes:
- Include the Boxicons CSS link or provide your own icon classes via `configure({ icons: { ... } })`.
- Use the minified build (`dist/index.min.js`) in production.
- Call `init()` once after Mermaid is available and the DOM is ready.

### Option B — NPM + bundler (Vite/Next/Nuxt/SvelteKit)

```ts
// main.ts (or app entry)
import { init } from '@mostlylucid/mermaid-enhancements';
import '@mostlylucid/mermaid-enhancements/styles.css';

// If Mermaid is installed locally
// import mermaid from 'mermaid';

// Call once on the client after the page mounts
(async () => {
  // Optional: configure before init()
  // import { configure } from '@mostlylucid/mermaid-enhancements';
  // configure({ controls: { exportSvg: false } });

  await init();
})();
```

Framework hints:
- Next.js/Nuxt: ensure `init()` runs client‑side only (e.g., inside `useEffect` or mounted hook). Avoid running during SSR.
- Astro/MDX/Markdown: your diagram containers should have the `mermaid` class. You can call `init()` once per page.
- Re‑rendering: if your framework rehydrates or replaces the diagram nodes, call `cleanupMermaidEnhancements()` before re‑initializing to avoid duplicate toolbars.

## API Reference

### `init()`

Initialize Mermaid with all enhancements. This is the main entry point.

```typescript
import { init } from '@mostlylucid/mermaid-enhancements';

await init();
```

**Returns:** `Promise<void>`

### `initMermaid()`

Initialize Mermaid with theme switching support. Called automatically by `init()`.

```typescript
import { initMermaid } from '@mostlylucid/mermaid-enhancements';

await initMermaid();
```

**Features:**
- Normalizes code fence blocks
- Sets up theme change listeners
- Handles OS-level theme preferences
- Re-renders diagrams when theme changes

**Returns:** `Promise<void>`

### `enhanceMermaidDiagrams()`

Add pan/zoom, fullscreen, and export capabilities to rendered diagrams.

```typescript
import { enhanceMermaidDiagrams } from '@mostlylucid/mermaid-enhancements';

enhanceMermaidDiagrams();
```

**Returns:** `void`

### `cleanupMermaidEnhancements()`

Clean up all pan-zoom instances. Call before re-rendering diagrams or unmounting.

```typescript
import { cleanupMermaidEnhancements } from '@mostlylucid/mermaid-enhancements';

cleanupMermaidEnhancements();
```

**Returns:** `void`

### `configure(config)`

Configure toolbar buttons and icons globally. Must be called before `init()`.

```typescript
import { configure } from '@mostlylucid/mermaid-enhancements';

configure({
  controls: {
    showControls: false,  // Hide entire toolbar
    fullscreen: true,
    zoomIn: true,
    zoomOut: false,       // Hide zoom out button
    reset: true,
    exportPng: true,
    exportSvg: false      // Hide SVG export
  },
  icons: {
    fullscreen: 'fas fa-expand',  // Use Font Awesome instead of Boxicons
    zoomIn: 'fas fa-plus'
  }
});
```

**Parameters:**
- `config` (EnhancementConfig): Configuration object

**Returns:** `void`

### `hideToolbar(diagramId?)`

Hide the toolbar for a specific diagram or all diagrams. Can be called at any time after initialization.

```typescript
import { hideToolbar } from '@mostlylucid/mermaid-enhancements';

// Hide toolbar for a specific diagram
hideToolbar('diagram-1');

// Hide toolbars for all diagrams
hideToolbar();
```

**Parameters:**
- `diagramId` (string, optional): The ID of the diagram wrapper. If omitted, hides all toolbars.

**Returns:** `void`

### `showToolbar(diagramId?)`

Show the toolbar for a specific diagram or all diagrams. Can be called at any time after initialization.

```typescript
import { showToolbar } from '@mostlylucid/mermaid-enhancements';

// Show toolbar for a specific diagram
showToolbar('diagram-1');

// Show toolbars for all diagrams
showToolbar();
```

**Parameters:**
- `diagramId` (string, optional): The ID of the diagram wrapper. If omitted, shows all toolbars.

**Returns:** `void`

### `toggleToolbar(diagramId?)`

Toggle the toolbar visibility for a specific diagram or all diagrams. Can be called at any time after initialization.

```typescript
import { toggleToolbar } from '@mostlylucid/mermaid-enhancements';

// Toggle toolbar for a specific diagram
toggleToolbar('diagram-1');

// Toggle toolbars for all diagrams
toggleToolbar();
```

**Parameters:**
- `diagramId` (string, optional): The ID of the diagram wrapper. If omitted, toggles all toolbars.

**Returns:** `void`

**Note:** To get the diagram ID, inspect the `data-diagram-id` attribute on the `.mermaid-wrapper` element, or capture the return value from `enhanceMermaidDiagrams()` which returns the generated diagram IDs.

## Styling

The package includes a complete CSS file with sensible defaults. Import it in your project:

```typescript
import '@mostlylucid/mermaid-enhancements/styles.css';
```

### Customization

Override CSS variables or classes to customize the appearance:

```css
/* Custom control button colors */
.mermaid-control-btn {
    color: #your-color;
}

.mermaid-control-btn:hover {
    background: #your-hover-color;
}

/* Custom lightbox background */
.mermaid-lightbox {
    background: rgba(0, 0, 0, 0.95);
}
```

## Theme Switching

The package automatically detects and responds to theme changes through multiple methods:

### 1. Custom Events

Dispatch custom events to trigger theme changes:

```javascript
// Switch to dark theme
document.body.dispatchEvent(new Event('dark-theme-set'));

// Switch to light theme
document.body.dispatchEvent(new Event('light-theme-set'));
```

### 2. Global State

Set a global theme state:

```javascript
window.__themeState = 'dark'; // or 'light'
await initMermaid();
```

### 3. LocalStorage

The package checks `localStorage.theme`:

```javascript
localStorage.theme = 'dark'; // or 'light'
```

### 4. CSS Class

Add a `dark` class to the `<html>` element:

```javascript
document.documentElement.classList.add('dark');
```

### 5. OS Preference

Automatically detects system-level dark mode preference via `prefers-color-scheme`.

## Toolbar Customization

Configure which buttons appear in the toolbar. All configurations are **global** and affect all diagrams on the page.

### Hide Entire Toolbar

```typescript
import { configure, init } from '@mostlylucid/mermaid-enhancements';

configure({
  controls: {
    showControls: false  // Hide all toolbars
  }
});

await init();
```

### Hide Button Groups

```typescript
configure({
  controls: {
    export: false,      // Hide both PNG and SVG export buttons
    fullscreen: false   // Hide fullscreen button
  }
});
```

### Granular Button Control

```typescript
configure({
  controls: {
    fullscreen: true,
    zoomIn: true,
    zoomOut: true,
    reset: false,        // Hide reset button
    exportPng: true,
    exportSvg: false     // Hide SVG export, keep PNG
  }
});

await init();
```

### Custom Icon Library

By default, Boxicons are used. You can configure different icon classes:

```typescript
configure({
  icons: {
    fullscreen: 'fas fa-expand',
    zoomIn: 'fas fa-plus',
    zoomOut: 'fas fa-minus',
    reset: 'fas fa-undo',
    exportPng: 'fas fa-image',
    exportSvg: 'fas fa-code'
  }
});
```

## Control Buttons

Each diagram gets the following controls (when enabled):

| Action | Description |
|--------|-------------|
| Fullscreen | Open diagram in lightbox with close button on far right |
| Zoom In | Increase zoom level |
| Zoom Out | Decrease zoom level |
| Reset | Reset zoom and position to default |
| Export PNG | Download as high-quality PNG (2x pixel ratio) |
| Export SVG | Download as vector SVG file |

**Note:** Panning is always enabled by default (drag to pan). In lightbox mode, a close button (✕) appears on the far right of the toolbar.

## Framework Integration

### React

```tsx
import { useEffect } from 'react';
import { init, cleanupMermaidEnhancements } from '@mostlylucid/mermaid-enhancements';
import '@mostlylucid/mermaid-enhancements/styles.css';

function MermaidDiagram({ chart }: { chart: string }) {
    useEffect(() => {
        init();

        return () => cleanupMermaidEnhancements();
    }, [chart]);

    return (
        <div className="mermaid">
            {chart}
        </div>
    );
}
```

### Vue

```vue
<template>
    <div class="mermaid">
        {{ chart }}
    </div>
</template>

<script setup>
import { onMounted, onUnmounted } from 'vue';
import { init, cleanupMermaidEnhancements } from '@mostlylucid/mermaid-enhancements';
import '@mostlylucid/mermaid-enhancements/styles.css';

const props = defineProps(['chart']);

onMounted(async () => {
    await init();
});

onUnmounted(() => {
    cleanupMermaidEnhancements();
});
</script>
```

### Svelte

```svelte
<script>
    import { onMount, onDestroy } from 'svelte';
    import { init, cleanupMermaidEnhancements } from '@mostlylucid/mermaid-enhancements';
    import '@mostlylucid/mermaid-enhancements/styles.css';

    export let chart;

    onMount(async () => {
        await init();
    });

    onDestroy(() => {
        cleanupMermaidEnhancements();
    });
</script>

<div class="mermaid">
    {@html chart}
</div>
```

## Development

### Run Demo

```bash
npm install
npm run dev
```

This will start a local server and open the demo page at `http://localhost:3000`.

### Build

```bash
npm run build
```

## TypeScript

Full TypeScript support is included:

```typescript
import type { PanZoomInstance, Theme, ExportFormat } from '@mostlylucid/mermaid-enhancements';

// Types are automatically available
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This is free and unencumbered software released into the public domain. See [LICENSE](LICENSE) for details.

## Acknowledgments

Built with:
- [Mermaid.js](https://mermaid.js.org/) - Diagram generation
- [svg-pan-zoom](https://github.com/bumbu/svg-pan-zoom) - Pan and zoom functionality
- [html-to-image](https://github.com/bubkoo/html-to-image) - Export capabilities
- [Boxicons](https://boxicons.com/) - Icon library

## Browser Support

- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)

## Known Issues & Workarounds

These are the most common integration issues and how to fix them quickly.

### 1) Toolbar icons are missing
- Symptom: buttons show empty squares or no icons.
- Cause: Boxicons CSS not loaded, or you use a different icon set.
- Fix:
  - Add Boxicons to the page:
    ```html
    <link href="https://unpkg.com/boxicons@2.1.4/css/boxicons.min.css" rel="stylesheet">
    ```
  - Or provide your own icon classes before `init()`:
    ```ts
    import { configure } from '@mostlylucid/mermaid-enhancements';
    configure({
      icons: {
        fullscreen: 'fas fa-expand',
        zoomIn: 'fas fa-plus',
        zoomOut: 'fas fa-minus',
        reset: 'fas fa-undo',
        exportPng: 'fas fa-file-image',
        exportSvg: 'fas fa-file-code'
      }
    });
    ```

### 2) Styles look off or toolbar overlaps
- Ensure you import the library CSS once:
  ```ts
  import '@mostlylucid/mermaid-enhancements/styles.css';
  ```
- If you have strict CSS resets, ensure `.mermaid-lightbox`, `.mermaid-controls`, and `.mermaid-container` aren’t unintentionally overridden.

### 3) Nothing happens after calling `init()`
- Verify Mermaid v11+ is present before `init()`.
- Run client-side only (SSR frameworks):
  - Next.js: call inside `useEffect` or a dynamic component with `ssr: false`.
  - Nuxt: call in `onMounted`.
- If your framework replaces DOM nodes post-mount, call:
  ```ts
  import { cleanupMermaidEnhancements } from '@mostlylucid/mermaid-enhancements';
  cleanupMermaidEnhancements();
  await init();
  ```

### 4) Double toolbars or duplicated pan/zoom
- Cause: re-initializing without cleanup.
- Fix: call `cleanupMermaidEnhancements()` before re-rendering/re-initializing.

### 5) Theme doesn’t switch with OS / site toggle
- Ensure you’re not re-initializing Mermaid on every theme change manually; the library listens to `prefers-color-scheme`.
- If you have a custom theme toggle that changes a `data-theme` or class, call a debounced re-render:
  ```ts
  // Example: after your site theme toggles
  await init(); // library re-renders diagrams respecting the new theme
  ```

### 6) Export to PNG/SVG fails
- Check the browser console for CORS errors. Cross-origin images/fonts in your diagram labels can block canvas export.
- Try same-origin fonts and images, or set permissive CORS headers.
- Very large diagrams may run out of memory; zoom in before exporting, or prefer SVG export which is lighter.

### 7) Mobile pinch/zoom is jumpy
- Some mobile layouts add nested transform/scroll containers. Ensure the `.mermaid-container` is not inside an element that intercepts touch actions. Add:
  ```css
  .mermaid-container, .mermaid-container * { touch-action: pinch-zoom; }
  ```

### 8) Performance on extremely large diagrams
- Pan/zoom is enabled, but massive SVGs can be heavy.
- Tips:
  - Prefer `flowchart` over `graph` where possible.
  - Collapse subgraphs.
  - Use the lightbox to view fullscreen (less layout thrash).

### 9) Mermaid version mismatch
- Ensure you’re using Mermaid 11 or newer. Pin versions when using a CDN:
  ```html
  <script type="module">
    import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
  </script>
  ```

### 10) Shadow DOM or MDX/Markdown renderers
- If your diagrams render inside Shadow DOM or are injected after page load, call `init()` after they appear.
- For MDX/Markdown engines that hydrate, use cleanup + init as shown above.

### 11) Content Security Policy (CSP)
- If your site blocks inline styles or data URLs, exporting may fail. Ensure your CSP allows:
  - `img-src 'self' data:`
  - `style-src` permitting necessary inline styles or add a nonce that your bundler/framework applies to injected styles.

### 12) Controlling the toolbar programmatically
- You can show/hide at runtime:
  ```ts
  import { hideToolbar, showToolbar, toggleToolbar } from '@mostlylucid/mermaid-enhancements';
  hideToolbar(); // all diagrams
  // hideToolbar('my-diagram-id'); // specific diagram if you provided ids
  ```

## Troubleshooting

### Controls not appearing

Make sure you've included Boxicons:

```html
<link href="https://unpkg.com/boxicons@2.1.4/css/boxicons.min.css" rel="stylesheet">
```

### Theme not switching

Ensure Mermaid is loaded before calling `init()`:

```typescript
// Wait for Mermaid to be available
await new Promise(resolve => setTimeout(resolve, 100));
await init();
```

### Export not working

The export feature requires modern browsers with Canvas API support. Check browser console for errors.

## Support

- [GitHub Issues](https://github.com/scottgal/mostlylucidweb/issues)
- [Documentation](https://github.com/scottgal/mostlylucidweb/tree/main/mostlylucid-mermaid)

---

Made with love for the Mermaid.js community


## Opt-in Pele renderer (next release)

The source now supports renderer adapters. Mermaid remains the default; Pele is supplied by the host and is not bundled by this package. These additions have not yet been published to npm.

```typescript
import { render } from 'pele';
import { configure, createPeleRenderer, init } from '@mostlylucid/mermaid-enhancements';

configure({ renderer: createPeleRenderer((source, options) => render(source, options)) });
await init();
```

The adapter consumes original Mermaid source and returns SVG. Pan/zoom, fullscreen and export work against that SVG, keeping renderer-specific layout out of the controls. CSS variables style Pele; Mermaid theme variables do not translate automatically. Use unique SVG prefixes per render and retain the source for later theme changes. Pele syntax/layout differences should be qualified on the diagrams your site uses.

### Touch scrolling

Diagrams start passive. A tap or click on the SVG does not enable gestures. Use the explicit **Enable Pan & Zoom** toolbar button to capture gestures inside the diagram; disable it to return to ordinary page scrolling. Zoom buttons remain available separately.

The disable path now calls `disablePan()` rather than `enablePan(false)`: svg-pan-zoom's enable method takes no boolean and would leave panning enabled. Fullscreen uses the same explicit interaction rule. Cloned diagrams retain resolved presentation colours and their original bounds for fullscreen/export.
