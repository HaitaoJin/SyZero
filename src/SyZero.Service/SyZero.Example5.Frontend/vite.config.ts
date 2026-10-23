import { defineConfig } from 'vite'

const port = Number(process.env.PORT || 5175)

export default defineConfig({
  server: {
    host: '0.0.0.0',
    port,
    strictPort: true,
    proxy: {
      '/example1': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
      '/example2': {
        target: 'http://localhost:5000',
        changeOrigin: true,
      },
    },
  },
})
