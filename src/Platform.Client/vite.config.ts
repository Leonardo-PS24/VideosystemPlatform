import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '^/(api|Account|Admin|Permissions|Kiosk)': {
        target: 'https://localhost:5001',
        secure: false,
        changeOrigin: true
      },
      '/kioskhub': {
        target: 'https://localhost:5001',
        secure: false,
        ws: true
      }
    }
  },
  build: {
    outDir: '../Platform.Portal/wwwroot',
    emptyOutDir: true
  }
})
