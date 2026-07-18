import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface RetentionRelease {
  id: string;
  documentNumber: string | null;
  status: string;
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  releaseDate: string;
  amountReleased: number;
  triggerEvent: string;
  revenueAccountId: string | null;
  receivableAccountId: string | null;
  expenseAccountId: string | null;
  payableAccountId: string | null;
  taxCodeId: string | null;
  vatAccountId: string | null;
  linkedArInvoiceId: string | null;
  linkedApInvoiceId: string | null;
  createdAt: string;
  createdBy: string;
}

export interface RetentionBalance {
  commercialDocumentId: string;
  totalWithheldToDate: number;
  totalReleasedToDate: number;
  outstandingBalance: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateRetentionReleaseInput {
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  releaseDate: string;
  amountReleased: number;
  triggerEvent: string;
  revenueAccountId?: string;
  receivableAccountId?: string;
  expenseAccountId?: string;
  payableAccountId?: string;
  taxCodeId?: string;
  vatAccountId?: string;
}

const BASE_PATH = "/api/v1/construction/retention-releases";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listRetentionReleases(top = 50, skip = 0): Promise<PagedResult<RetentionRelease>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getRetentionReleaseBalance(
  commercialDocumentType: string, commercialDocumentId: string,
): Promise<RetentionBalance> {
  const response = await fetch(
    `${API_BASE_URL}${BASE_PATH}/balance?commercialDocumentType=${commercialDocumentType}&commercialDocumentId=${commercialDocumentId}`,
    { headers: authHeaders() },
  );
  return handleJson(response);
}

export async function getRetentionRelease(id: string): Promise<RetentionRelease> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createRetentionRelease(input: CreateRetentionReleaseInput): Promise<RetentionRelease> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitRetentionRelease(id: string): Promise<RetentionRelease> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveRetentionRelease(id: string): Promise<RetentionRelease> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectRetentionRelease(id: string): Promise<RetentionRelease> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
