import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react-swc'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 8802,
    proxy: {
      '/api': {
        target: 'http://localhost:8801',
        changeOrigin: true,
      }
    }
  }
})
