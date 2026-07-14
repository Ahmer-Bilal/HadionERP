import { API_BASE_URL } from "../config";

export interface TaxCode {
  id: string;
  documentNumber: string | null;
  status: string;
  taxCodeCode: string;
  taxCodeName: string;
  taxCodeNameArabic: string | null;
  rate: number;
  taxType: string;
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

export interface CreateTaxCodeInput {
  taxCodeCode: string;
  taxCodeName: string;
  rate: number;
  taxType: string;
  taxCodeNameArabic?: string;
}

export interface UpdateTaxCodeInput {
  taxCodeName: string;
  rate: number;
  taxCodeNameArabic?: string;
  isActive?: boolean;
}

const BASE_PATH = "/api/v1/masterdata/tax-codes";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listTaxCodes(top = 50, skip = 0): Promise<PagedResult<TaxCode>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`);
  return handleJson(response);
}

export async function getTaxCode(id: string): Promise<TaxCode> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`);
  return handleJson(response);
}

export async function createTaxCode(input: CreateTaxCodeInput): Promise<TaxCode> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function updateTaxCode(id: string, input: UpdateTaxCodeInput): Promise<TaxCode> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitTaxCode(id: string): Promise<TaxCode> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST" });
  return handleJson(response);
}

export async function approveTaxCode(id: string): Promise<TaxCode> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST" });
  return handleJson(response);
}

export async function rejectTaxCode(id: string): Promise<TaxCode> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST" });
  return handleJson(response);
}
