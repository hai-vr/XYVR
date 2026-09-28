import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    // The Linux build (Photino) runs on WebKitGTK 2.36, which is roughly equivalent to Safari 15.4.
    // Without this, the CSS minifier rewrites @media (min-width: X) into range syntax @media (width >= X),
    // which WebKitGTK 2.36 does not support, causing all media queries to be ignored.
    cssTarget: ['safari15'],
  },
})
