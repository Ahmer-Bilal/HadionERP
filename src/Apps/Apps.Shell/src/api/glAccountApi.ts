import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface GLAccount {
  id: string;
  documentNumber: string | null;
  status: string;
  accountCode: string;
  accountName: string;
  accountNameArabic: string | null;
  accountType: string;
  normalBalance: string;
  parentAccountId: string | null;
  isPostable: boolean;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateGLAccountInput {
  accountCode: string;
  accountName: string;
  accountType: string;
  accountNameArabic?: string;
  parentAccountId?: string | null;
  isPostable?: boolean;
}

export interface UpdateGLAccountInput {
  accountName: string;
  accountNameArabic?: string;
  parentAccountId?: string | null;
  isPostable?: boolean;
  isActive?: boolean;
}

const BASE_PATH = "/api/v1/masterdata/gl-accounts";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listGLAccounts(top = 50, skip = 0): Promise<PagedResult<GLAccount>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getGLAccount(id: string): Promise<GLAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createGLAccount(input: CreateGLAccountInput): Promise<GLAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function updateGLAccount(id: string, input: UpdateGLAccountInput): Promise<GLAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitGLAccount(id: string): Promise<GLAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveGLAccount(id: string): Promise<GLAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectGLAccount(id: string): Promise<GLAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
