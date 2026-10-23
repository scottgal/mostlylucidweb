# Changelog

## Unreleased

- Repair the local demo's obsolete imports; build both renderers locally and add a compatibility checklist/matrix.
- Cancel delayed fits and disconnect resize observers on cleanup; refit passive diagrams after resize.
- Close fullscreen dialogs through one cleanup path, trap keyboard focus and restore the opener.
- Serialize async theme renders, isolate adapter errors, and honour HTMX opt-in for root diagrams and code fences.
- Export source geometry without CSS viewport transforms, preserve local marker references, and remove failed PNG staging nodes.
- Preserve custom icons across configuration updates; prevent form submission from toolbar buttons; restore slid toolbars through the public APIs.
- Point public package imports/requires at built browser bundles.


- Add an optional renderer contract and an injected Pele adapter; Mermaid stays the default.
- Keep source text and themed SVG presentation when rendering, opening fullscreen and exporting.
- Require explicit gesture activation. Correct the pan-disable API call so finger scrolling can return to the page.
- Generate public function declarations through the package's root types entry. Mermaid's peer dependency is optional for adapter-only consumers.

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-11-10

### Added
- Initial release
- Interactive SVG pan and zoom functionality
- Fullscreen lightbox view for diagrams
- Export diagrams to PNG and SVG formats
- Automatic theme switching (light/dark mode)
- Support for OS-level theme preferences
- Responsive design for mobile and desktop
- TypeScript support with full type definitions
- Comprehensive documentation and examples
- Demo HTML page
- Control buttons for zoom, pan, reset, fullscreen, and export
- Keyboard shortcuts (ESC to close lightbox)
- Double-click zoom functionality
- Mouse wheel zoom support
- Drag-to-pan navigation
- Accessible ARIA labels
- Event delegation for efficient event handling
- Cleanup utilities for memory management

### Dependencies
- svg-pan-zoom ^3.6.1
- html-to-image ^1.11.11
- mermaid ^10.0.0 || ^11.0.0 (peer dependency)

### Browser Support
- Chrome/Edge (latest)
- Firefox (latest)
- Safari (latest)
- Mobile browsers (iOS Safari, Chrome Mobile)
