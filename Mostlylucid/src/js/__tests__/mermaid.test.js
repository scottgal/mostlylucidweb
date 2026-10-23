import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { initMermaid } from '../../../../mostlylucid-mermaid/src/theme-switcher';

// Mock mermaid
const mermaidMock = {
  initialize: vi.fn(),
  run: vi.fn().mockResolvedValue(undefined),
};

describe('Mermaid Theme Integration', () => {
  beforeEach(() => {
    // Setup mermaid global
    window.mermaid = mermaidMock;

    // Reset mocks
    mermaidMock.initialize.mockClear();
    mermaidMock.run.mockClear();

    // Clear localStorage
    localStorage.clear();

    // Reset DOM
    document.body.innerHTML = '';
  });

  afterEach(() => {
    // Clean up event listeners
    const events = ['dark-theme-set', 'light-theme-set'];
    events.forEach(event => {
      const listeners = document.body.eventListenerList?.[event] || [];
      listeners.forEach(listener => {
        document.body.removeEventListener(event, listener);
      });
    });
  });

  describe('Code fence normalization', () => {
    it('should convert pre > code.language-mermaid to pre.mermaid', async () => {
      document.body.innerHTML = `
        <pre><code class="language-mermaid"></code></pre>
      `;
      // Set source as text, as an escaped Markdown code fence does in the browser.
      document.querySelector('code').textContent = 'graph TD\n    A-->B';

      await initMermaid();

      const pre = document.querySelector('pre');
      expect(pre.classList.contains('mermaid')).toBe(true);
      expect(pre.textContent.trim()).toBe('graph TD\n    A-->B');
      expect(pre.querySelector('code')).toBeNull();
    });

    it('should handle multiple code fences', async () => {
      document.body.innerHTML = `
        <pre><code class="language-mermaid">graph TD
    A-->B</code></pre>
        <pre><code class="language-mermaid">graph LR
    C-->D</code></pre>
      `;

      await initMermaid();

      const pres = document.querySelectorAll('pre.mermaid');
      expect(pres).toHaveLength(2);
      expect(pres[0].textContent.includes('A-->B')).toBe(true);
      expect(pres[1].textContent.includes('C-->D')).toBe(true);
    });

    it('should not re-normalize already processed blocks', async () => {
      document.body.innerHTML = `
        <pre class="mermaid">graph TD
    A-->B</pre>
      `;

      const originalContent = document.querySelector('pre').textContent;

      await initMermaid();

      const pre = document.querySelector('pre');
      expect(pre.textContent).toBe(originalContent);
    });

    it('should skip code blocks without parent', async () => {
      const code = document.createElement('code');
      code.className = 'language-mermaid';
      code.textContent = 'graph TD';

      // Don't append to DOM - test orphaned element
      await initMermaid();

      // Should not throw and should not have mermaid class
      expect(code.parentElement).toBeNull();
    });
  });

  describe('Data attribute management', () => {
    it('should save original code to data attribute', async () => {
      const diagramCode = 'graph TD\n    A-->B';
      document.body.innerHTML = '<pre class="mermaid"></pre>';
      document.querySelector('pre').textContent = diagramCode;

      await initMermaid();

      const pre = document.querySelector('pre.mermaid');
      expect(pre.getAttribute('data-original-code')).toBe(diagramCode);
    });

    it('should not overwrite data attribute if already processed', async () => {
      document.body.innerHTML = `
        <pre class="mermaid" data-processed="true" data-original-code="original">modified</pre>
      `;

      await initMermaid();

      const pre = document.querySelector('pre.mermaid');
      expect(pre.getAttribute('data-original-code')).toBe('original');
    });
  });

  describe('Theme event handling', () => {
    it('should listen for dark-theme-set event', async () => {
      localStorage.theme = 'light';
      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      await initMermaid();

      // Spy on mermaid.run
      mermaidMock.run.mockClear();

      // Trigger dark theme event
      document.body.dispatchEvent(new CustomEvent('dark-theme-set'));

      // Wait for async handler
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(mermaidMock.initialize).toHaveBeenCalledWith(
        expect.objectContaining({ theme: 'dark' })
      );
      expect(mermaidMock.run).toHaveBeenCalled();
    });

    it('should listen for light-theme-set event', async () => {
      localStorage.theme = 'dark';
      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      await initMermaid();

      // Spy on mermaid.run
      mermaidMock.run.mockClear();

      // Trigger light theme event
      document.body.dispatchEvent(new CustomEvent('light-theme-set'));

      // Wait for async handler
      await new Promise(resolve => setTimeout(resolve, 10));

      expect(mermaidMock.initialize).toHaveBeenCalledWith(
        expect.objectContaining({ theme: 'default' })
      );
      expect(mermaidMock.run).toHaveBeenCalled();
    });

    it('should remove old event listeners before adding new ones', async () => {
      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      // Initialize twice
      await initMermaid();
      await initMermaid();

      // Trigger event once
      mermaidMock.run.mockClear();
      document.body.dispatchEvent(new CustomEvent('dark-theme-set'));

      await new Promise(resolve => setTimeout(resolve, 10));

      // Should only be called once (not twice from duplicate listeners)
      expect(mermaidMock.run).toHaveBeenCalledOnce();
    });
  });

  describe('OS theme preference handling', () => {
    it('should listen to matchMedia changes', async () => {
      const mediaQueryListMock = {
        matches: false,
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
      };

      window.matchMedia = vi.fn(() => mediaQueryListMock);

      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      await initMermaid();

      expect(window.matchMedia).toHaveBeenCalledWith('(prefers-color-scheme: dark)');
      expect(mediaQueryListMock.addEventListener).toHaveBeenCalledWith(
        'change',
        expect.any(Function)
      );
    });

    it('should handle OS theme change event', async () => {
      let changeHandler;
      const mediaQueryListMock = {
        matches: false,
        addEventListener: vi.fn((event, handler) => {
          if (event === 'change') changeHandler = handler;
        }),
        removeEventListener: vi.fn(),
      };

      window.matchMedia = vi.fn(() => mediaQueryListMock);

      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      await initMermaid();

      // Simulate OS theme change to dark
      mermaidMock.run.mockClear();
      mermaidMock.initialize.mockClear();

      await changeHandler({ matches: true });

      expect(mermaidMock.initialize).toHaveBeenCalledWith(
        expect.objectContaining({ theme: 'dark' })
      );
      expect(mermaidMock.run).toHaveBeenCalled();
    });

    it('should remove old matchMedia listener before adding new', async () => {
      let firstHandler, secondHandler;
      const mediaQueryListMock = {
        matches: false,
        addEventListener: vi.fn((event, handler) => {
          if (event === 'change') {
            if (!firstHandler) firstHandler = handler;
            else secondHandler = handler;
          }
        }),
        removeEventListener: vi.fn(),
      };

      window.matchMedia = vi.fn(() => mediaQueryListMock);

      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      await initMermaid();
      await initMermaid();

      // Should have removed the first handler
      expect(mediaQueryListMock.removeEventListener).toHaveBeenCalledWith(
        'change',
        expect.any(Function)
      );
    });
  });

  describe('Error handling', () => {
    it('should handle mermaid.run errors gracefully', async () => {
      mermaidMock.run.mockRejectedValueOnce(new Error('Mermaid error'));

      document.body.innerHTML = '<pre class="mermaid">invalid diagram</pre>';

      // Should not throw
      await expect(initMermaid()).resolves.not.toThrow();
    });

    it('should handle errors during theme switch', async () => {
      mermaidMock.run.mockRejectedValueOnce(new Error('Theme switch error'));

      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      await initMermaid();

      // Trigger theme change - should not throw
      await expect((async () => {
        document.body.dispatchEvent(new CustomEvent('dark-theme-set'));
        await new Promise(resolve => setTimeout(resolve, 10));
      })()).resolves.not.toThrow();
    });
  });

  describe('Early exit conditions', () => {
    it('should exit early if no mermaid elements exist', async () => {
      document.body.innerHTML = '<div>No mermaid here</div>';

      await initMermaid();

      // Should not have set up event listeners or called mermaid
      expect(mermaidMock.run).not.toHaveBeenCalled();
    });

    it('should render on init using current theme without waiting for event', async () => {
      localStorage.theme = 'dark';
      document.body.innerHTML = '<pre class="mermaid">graph TD</pre>';

      mermaidMock.run.mockClear();
      mermaidMock.initialize.mockClear();

      await initMermaid();

      // Should render immediately based on current theme
      expect(mermaidMock.initialize).toHaveBeenCalledWith(
        expect.objectContaining({ theme: 'dark' })
      );
      expect(mermaidMock.run).toHaveBeenCalled();
    });
  });

  describe('Re-initialization (HTMX scenarios)', () => {
    it('should handle re-initialization with new content', async () => {
      // Initial content
      document.body.innerHTML = '<pre class="mermaid">graph TD\nA-->B</pre>';
      await initMermaid();

      // Simulate HTMX adding new content
      document.body.innerHTML += '<pre class="mermaid">graph LR\nC-->D</pre>';
      await initMermaid();

      const diagrams = document.querySelectorAll('pre.mermaid');
      expect(diagrams).toHaveLength(2);
      expect(diagrams[0].getAttribute('data-original-code')).toBeTruthy();
      expect(diagrams[1].getAttribute('data-original-code')).toBeTruthy();
    });
  });
});
