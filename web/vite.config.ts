import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vitest/config';
import vue from '@vitejs/plugin-vue';

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    // Forwards /api to the ASP.NET Core API. Start the API with ASPNETCORE_URLS=http://localhost:5000.
    proxy: {
      '/api': 'http://localhost:5000',
    },
  },
  test: {
    environment: 'node',
  },
});
