// Backend base URL. Overridable per environment via VITE_API_BASE_URL (see .env files); defaults to the
// local Gateway.Api dev port so `npm run dev` works out of the box during Phase 0 development.
export const API_BASE_URL: string = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5210";
