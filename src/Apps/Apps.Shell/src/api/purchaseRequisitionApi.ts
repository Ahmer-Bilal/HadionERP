import { API_BASE_URL } from "../config";

export interface PurchaseRequisitionLine {
  id: string;
  itemId: string;
  costCenterId: string;
  quantity: number;
  estimatedUnitPrice: number;
  estimatedLineTotal: number;
  lineDescription: string | null;
}

export interface PurchaseRequisition {
  id: string;
  documentNumber: string | null;
  status: string;
  description: string;
  requiredByDate: string | null;
  estimatedTotal: number;
  lines: PurchaseRequisitionLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreatePurchaseRequisitionLineInput {
  itemId: string;
  costCenterId: string;
  quantity: number;
  estimatedUnitPrice: number;
  lineDescription?: string;
}

export interface CreatePurchaseRequisitionInput {
  description: string;
  lines: CreatePurchaseRequisitionLineInput[];
  requiredByDate?: string;
}

const BASE_PATH = "/api/v1/procurement/purchase-requisitions";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listPurchaseRequisitions(top = 50, skip = 0): Promise<PagedResult<PurchaseRequisition>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`);
  return handleJson(response);
}

export async function getPurchaseRequisition(id: string): Promise<PurchaseRequisition> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`);
  return handleJson(response);
}

export async function createPurchaseRequisition(input: CreatePurchaseRequisitionInput): Promise<PurchaseRequisition> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitPurchaseRequisition(id: string): Promise<PurchaseRequisition> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST" });
  return handleJson(response);
}

export async function approvePurchaseRequisition(id: string): Promise<PurchaseRequisition> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST" });
  return handleJson(response);
}

export async function rejectPurchaseRequisition(id: string): Promise<PurchaseRequisition> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST" });
  return handleJson(response);
}
