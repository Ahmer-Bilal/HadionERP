import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
// Platform.UI's tokens + component styles load first, so Apps.Shell's own index.css can override
// app-specific concerns without being overridden itself. The design tokens (--pi-*) are the single source
// of truth for color/spacing/typography — see src/Platform/Platform.UI/tokens/design-tokens.css.
import "@platform/ui/fonts/fonts.css"
import "@platform/ui/tokens/design-tokens.css"
import "@platform/ui/components/components.css"
import "./index.css"
import App from './App.tsx'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
