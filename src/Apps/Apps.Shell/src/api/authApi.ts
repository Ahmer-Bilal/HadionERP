import { API_BASE_URL } from "../config";

export interface AuthenticatedUser {
  id: string;
  username: string;
  email: string | null;
  displayName: string;
  isActive: boolean;
  roleKeys: string[];
  createdAt: string;
  createdBy: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

const BASE_PATH = "/api/v1/identity/auth";
const TOKEN_STORAGE_KEY = "hadionerp.auth.token";

// Module-level cache + localStorage persistence across reloads — a disclosed simplification (not httpOnly-
// cookie-grade XSS hardening), see MISSING-FEATURES-AUDIT.md Part 1 §1's own stated scope boundary for this pass.
let cachedToken: string | null = localStorage.getItem(TOKEN_STORAGE_KEY);

export function getToken(): string | null {
  return cachedToken;
}

export function setToken(token: string | null): void {
  cachedToken = token;
  if (token) localStorage.setItem(TOKEN_STORAGE_KEY, token);
  else localStorage.removeItem(TOKEN_STORAGE_KEY);
}

/** Merged into every other api/*.ts file's fetch headers so every request carries the bearer token —
 * one shared helper instead of 15 separate implementations. */
export function authHeaders(): Record<string, string> {
  const token = getToken();
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function login(username: string, password: string): Promise<LoginResponse> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });
  return handleJson(response);
}

export async function fetchCurrentUser(): Promise<AuthenticatedUser> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/me`, { headers: authHeaders() });
  return handleJson(response);
}
