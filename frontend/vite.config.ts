import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    {
      name: 'presentation-rewrite',
      configureServer(server) {
        server.middlewares.use((req, _res, next) => {
          if (req.url === '/presentation' || req.url === '/presentation/') {
            req.url = '/presentation/index.html';
          }
          next();
        });
      },
    },
    react(),
    tailwindcss(),
  ],
})
