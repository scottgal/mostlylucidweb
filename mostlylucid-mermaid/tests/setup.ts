/**
 * Test setup file
 * This runs before all tests
 */

// Mock Mermaid
global.window = global.window || ({} as any);
window.mermaid = {
  initialize: vi.fn(),
  run: vi.fn(async () => Promise.resolve()),
};

// Mock svg-pan-zoom
vi.mock('svg-pan-zoom', () => ({
  default: vi.fn(() => ({
    zoom: vi.fn(),
    zoomIn: vi.fn(),
    zoomOut: vi.fn(),
    reset: vi.fn(),
    fit: vi.fn(),
    center: vi.fn(),
    resize: vi.fn(),
    destroy: vi.fn(),
    isPanEnabled: vi.fn(() => false),
    enablePan: vi.fn(),
    disablePan: vi.fn(),
    enableZoom: vi.fn(),
    disableZoom: vi.fn(),
    enableMouseWheelZoom: vi.fn(),
    disableMouseWheelZoom: vi.fn(),
    enableDblClickZoom: vi.fn(),
    disableDblClickZoom: vi.fn(),
  })),
}));

// Mock html-to-image
vi.mock('html-to-image', () => ({
  toPng: vi.fn(async () => 'data:image/png;base64,mock'),
  toSvg: vi.fn(async () => 'data:image/svg+xml;base64,mock'),
}));

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: vi.fn(),
    removeListener: vi.fn(),
    addEventListener: vi.fn(),
    removeEventListener: vi.fn(),
    dispatchEvent: vi.fn(),
  })),
});

// Mock localStorage
const localStorageMock = {
  getItem: vi.fn(),
  setItem: vi.fn(),
  removeItem: vi.fn(),
  clear: vi.fn(),
};
global.localStorage = localStorageMock as any;

// Ensure theme state does not leak across tests
beforeEach(() => {
  document.body.classList.remove('dark');
  document.documentElement.classList.remove('dark');
  delete (window as any).__themeState;
  // clear any direct property set by tests
  delete (global as any).localStorage?.theme;
});
