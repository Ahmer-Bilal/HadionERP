import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface VariationOrderLine {
  id: string;
  commercialDocumentLineId: string | null;
  code: string | null;
  description: string | null;
  unitOfMeasure: string | null;
  wbsElementId: string | null;
  quantityDelta: number;
  rate: number;
  amount: number;
}

export interface VariationOrder {
  id: string;
  documentNumber: string | null;
  status: string;
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  reason: string;
  totalValue: number;
  lines: VariationOrderLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateVariationOrderLineInput {
  commercialDocumentLineId?: string;
  quantityDelta: number;
  code?: string;
  description?: string;
  descriptionArabic?: string;
  unitOfMeasure?: string;
  wbsElementId?: string;
  rate?: number;
}

export interface CreateVariationOrderInput {
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  reason: string;
  lines: CreateVariationOrderLineInput[];
}

const BASE_PATH = "/api/v1/construction/variation-orders";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listVariationOrders(top = 50, skip = 0): Promise<PagedResult<VariationOrder>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getVariationOrder(id: string): Promise<VariationOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createVariationOrder(input: CreateVariationOrderInput): Promise<VariationOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitVariationOrder(id: string): Promise<VariationOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveVariationOrder(id: string): Promise<VariationOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectVariationOrder(id: string): Promise<VariationOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
