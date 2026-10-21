import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The API's CORS policy whitelists http://localhost:5173, so the dev server port is pinned.
export default defineConfig(({ mode }) => {
  const apiBaseUrl = loadEnv(mode, process.cwd(), 'VITE_').VITE_API_BASE_URL?.trim();
  if (mode === 'production') {
    if (!apiBaseUrl) {
      throw new Error('VITE_API_BASE_URL must be configured for production builds.');
    }

    let parsedApiBaseUrl;
    try {
      parsedApiBaseUrl = new URL(apiBaseUrl);
    } catch {
      throw new Error('VITE_API_BASE_URL must be an absolute HTTPS URL.');
    }

    if (parsedApiBaseUrl.protocol !== 'https:') {
      throw new Error('VITE_API_BASE_URL must use HTTPS in production.');
    }
  }

  return {
    plugins: [react(), tailwindcss()],
    server: {
      port: 5173,
      strictPort: true,
    },
  };
});
