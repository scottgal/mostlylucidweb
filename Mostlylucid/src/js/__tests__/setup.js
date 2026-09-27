import { beforeEach, afterEach, vi } from 'vitest';

// Mock localStorage with working state, including property reflection semantics
const storageState = {};
const localStorageBase = {
  getItem: vi.fn((key) => (key in storageState ? storageState[key] : null)),
  setItem: vi.fn((key, value) => { storageState[key] = String(value); }),
  removeItem: vi.fn((key) => { delete storageState[key]; }),
  clear: vi.fn(() => { for (const k of Object.keys(storageState)) delete storageState[k]; }),
  get length() { return Object.keys(storageState).length; },
  key: vi.fn((i) => Object.keys(storageState)[i] || null),
};

const localStorageProxy = new Proxy(localStorageBase, {
  get(target, prop, receiver) {
    if (prop in target) return Reflect.get(target, prop, receiver);
    if (typeof prop === 'string' && Object.prototype.hasOwnProperty.call(storageState, prop)) {
      return storageState[prop];
    }
    return undefined;
  },
  set(target, prop, value) {
    if (typeof prop === 'string') {
      storageState[prop] = String(value);
      return true;
    }
    target[prop] = value;
    return true;
  },
  has(target, prop) {
    if (prop in target) return true;
    if (typeof prop === 'string') return Object.prototype.hasOwnProperty.call(storageState, prop);
    return false;
  },
  deleteProperty(target, prop) {
    if (typeof prop === 'string') {
      return delete storageState[prop];
    }
    return delete target[prop];
  }
});

// happy-dom 20 exposes localStorage as a getter-only property, so it has to be redefined
// rather than assigned
Object.defineProperty(globalThis, 'localStorage', {
  configurable: true,
  writable: true,
  value: localStorageProxy,
});

// Mock matchMedia
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: vi.fn().mockImplementation(query => ({
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

// Track and manage document.body event listeners so tests can cleanly remove them
(() => {
  const body = document.body;
  if (!body) return;
  const origAdd = body.addEventListener.bind(body);
  const origRemove = body.removeEventListener.bind(body);

  // Initialize storage
  body.eventListenerList = body.eventListenerList || {};

  body.addEventListener = function(type, listener, options) {
    if (!this.eventListenerList) this.eventListenerList = {};
    if (!this.eventListenerList[type]) this.eventListenerList[type] = [];
    this.eventListenerList[type].push(listener);
    return origAdd(type, listener, options);
  };

  body.removeEventListener = function(type, listener, options) {
    const list = this.eventListenerList && this.eventListenerList[type];
    if (list) {
      const idx = list.indexOf(listener);
      if (idx !== -1) list.splice(idx, 1);
    }
    return origRemove(type, listener, options);
  };
})();

// Reset mocks and state before each test
beforeEach(() => {
  // Reset localStorage state and mocks
  if (typeof localStorage.clear === 'function') localStorage.clear();
  if (localStorage.getItem?.mockClear) localStorage.getItem.mockClear();
  if (localStorage.setItem?.mockClear) localStorage.setItem.mockClear();
  if (localStorage.removeItem?.mockClear) localStorage.removeItem.mockClear();
  if (localStorage.clear?.mockClear) localStorage.clear.mockClear();

  // Clear document body
  document.body.innerHTML = '';
  document.documentElement.className = '';

  // Remove any lingering event listeners recorded on body
  const evMap = document.body.eventListenerList || {};
  for (const type of Object.keys(evMap)) {
    // copy to avoid mutation during iteration
    const listeners = [...evMap[type]];
    for (const l of listeners) {
      document.body.removeEventListener(type, l);
    }
  }
  document.body.eventListenerList = {};

  // Reset any global state
  if (window.Alpine) {
    window.Alpine = undefined;
  }
  if (window.mermaid) {
    window.mermaid = undefined;
  }
});

// Cleanup after each test
afterEach(() => {
  vi.clearAllTimers();
  vi.clearAllMocks();
});
