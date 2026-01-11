    import { defineConfig } from 'vite'
    import react from '@vitejs/plugin-react'
    import svgr from "vite-plugin-svgr"

    // https://vite.dev/config/
    export default defineConfig({
    plugins: [react(), svgr()],
    server: {
        proxy:{
            '/api': {
                target: 'http://localhost:5000',
                changeOrigin: true,
                secure: false,
            },
            '/be_assets': {
                target: 'http://localhost:5000',
                changeOrigin: true,
                secure: false,
            },
            '/chatHub': {
                target: 'http://localhost:5000',
                changeOrigin: true,
                secure: false,
            },
            '/directMessageHub': {
                target: 'http://localhost:5000',
                changeOrigin: true,
                secure: false,
            }
        }
    }
    })
