import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";
import type { AuthenticatedUser } from "./authApi";

export type ManagedUser = AuthenticatedUser;

export interface CreateUserInput {
  username: string;
  displayName: string;
  password: string;
  email?: string;
}

export interface AssignRoleInput {
  roleKey: string;
  overrideReason?: string;
}

/** Thrown specifically for a 409 Segregation of Duties conflict (see UsersController.AssignRole) — carries
 * the structured conflict descriptions so the Users admin page can list them and prompt for an override
 * reason, instead of just showing a generic error string. */
export class SodConflictError extends Error {
  conflicts: string[];
  constructor(message: string, conflicts: string[]) {
    super(message);
    this.conflicts = conflicts;
  }
}

const BASE_PATH = "/api/v1/identity/users";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    if (response.status === 409 && body?.errors) {
      const conflicts = Object.values(body.errors).flat() as string[];
      throw new SodConflictError(body?.detail ?? "Segregation of Duties conflict.", conflicts);
    }
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listUsers(): Promise<ManagedUser[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createUser(input: CreateUserInput): Promise<ManagedUser> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function assignRole(userId: string, input: AssignRoleInput): Promise<ManagedUser> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${userId}/roles`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function removeRole(userId: string, roleKey: string): Promise<ManagedUser> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${userId}/roles/${encodeURIComponent(roleKey)}`, {
    method: "DELETE",
    headers: authHeaders(),
  });
  return handleJson(response);
}

export async function activateUser(userId: string): Promise<ManagedUser> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${userId}/activate`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function deactivateUser(userId: string): Promise<ManagedUser> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${userId}/deactivate`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function resetPassword(userId: string, newPassword: string): Promise<ManagedUser> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${userId}/reset-password`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ newPassword }),
  });
  return handleJson(response);
}
