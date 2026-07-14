import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath } from 'node:url'

// Platform.UI lives at src/Platform/Platform.UI as plain .ts/.tsx/.css files (not a real npm package yet —
// see its README). Two aliases are needed because Platform.UI is outside this app's node_modules tree, so
// neither the Vite bundler nor the TS checker can resolve "react" by walking up from Platform.UI's files:
//   1. "@platform/ui"  — the design system itself.
//   2. "react"          — pointed at this app's copy so Platform.UI's imports resolve to the same instance
//      Apps.Shell uses (also keeps the JSX runtime resolvable). This is the standard approach for a
//      monorepo without npm workspaces; a real workspace setup later removes the need for it.
const platformUiPath = fileURLToPath(new URL('../../Platform/Platform.UI', import.meta.url))
const reactPath = fileURLToPath(new URL('./node_modules/react', import.meta.url))
const projectRootPath = fileURLToPath(new URL('.', import.meta.url))

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@platform/ui': platformUiPath,
      react: reactPath,
    },
    // Ensure a single React instance even if resolved from two locations.
    dedupe: ['react'],
  },
  server: {
    fs: {
      // CSS imported via JS (design-tokens.css, components.css) is transformed/inlined by Vite's own
      // pipeline and never hits this check — but a plain url() reference inside CSS (fonts.css's
      // @font-face src) is fetched as a raw static file, which Vite's dev server refuses to serve from
      // outside its project root by default. Platform.UI lives outside Apps.Shell's root (same reason
      // the two aliases above exist), so it needs to be explicitly allow-listed. Setting `allow`
      // REPLACES Vite's default list rather than extending it, so Apps.Shell's own root must be listed
      // too, or the app's own index.html/source files 403.
      allow: [projectRootPath, platformUiPath],
    },
  },
})
