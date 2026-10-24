import { defineConfig } from 'vitest/config';
import path from 'path';

export default defineConfig({
  test: {
    globals: true,
    environment: 'happy-dom',
    environmentOptions: {
      happyDOM: {settings: {enableIframePageLoading: true}},
    },
    setupFiles: ['./src/js/__tests__/setup.js'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      exclude: [
        'node_modules/',
        'src/js/__tests__/',
        '**/*.spec.js',
        '**/*.test.js',
      ]
    },
    include: ['src/js/**/*.{test,spec}.js'],
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
});
