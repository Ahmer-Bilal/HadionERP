import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface LookupType {
  id: string;
  code: string;
  name: string;
  nameArabic: string | null;
  isSystemDefined: boolean;
  valueCount: number;
}

export interface LookupValue {
  id: string;
  lookupTypeCode: string;
  code: string;
  name: string;
  nameArabic: string | null;
  isActive: boolean;
  sortOrder: number;
}

export interface CreateLookupTypeInput {
  code: string;
  name: string;
  nameArabic?: string;
}

export interface UpdateLookupTypeInput {
  name: string;
  nameArabic?: string;
}

export interface CreateLookupValueInput {
  code: string;
  name: string;
  nameArabic?: string;
  sortOrder?: number;
}

export interface UpdateLookupValueInput {
  name: string;
  nameArabic?: string;
  sortOrder: number;
}

const BASE_PATH = "/api/v1/masterdata/lookup-types";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

async function handleVoid(response: Response): Promise<void> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
}

export async function listLookupTypes(): Promise<LookupType[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createLookupType(input: CreateLookupTypeInput): Promise<LookupType> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function updateLookupType(typeCode: string, input: UpdateLookupTypeInput): Promise<LookupType> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function deleteLookupType(typeCode: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}`, { method: "DELETE", headers: authHeaders() });
  return handleVoid(response);
}

export async function listLookupValues(typeCode: string, includeInactive = true): Promise<LookupValue[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}/values?includeInactive=${includeInactive}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createLookupValue(typeCode: string, input: CreateLookupValueInput): Promise<LookupValue> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}/values`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function updateLookupValue(typeCode: string, id: string, input: UpdateLookupValueInput): Promise<LookupValue> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}/values/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function activateLookupValue(typeCode: string, id: string): Promise<LookupValue> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}/values/${id}/activate`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function deactivateLookupValue(typeCode: string, id: string): Promise<LookupValue> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}/values/${id}/deactivate`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function deleteLookupValue(typeCode: string, id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${typeCode}/values/${id}`, { method: "DELETE", headers: authHeaders() });
  return handleVoid(response);
}
