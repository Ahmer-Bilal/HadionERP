import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface CostCenter {
  id: string;
  documentNumber: string | null;
  status: string;
  costCenterCode: string;
  costCenterName: string;
  costCenterNameArabic: string | null;
  parentCostCenterId: string | null;
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

export interface CreateCostCenterInput {
  costCenterCode: string;
  costCenterName: string;
  costCenterNameArabic?: string;
  parentCostCenterId?: string | null;
  isPostable?: boolean;
}

export interface UpdateCostCenterInput {
  costCenterName: string;
  costCenterNameArabic?: string;
  parentCostCenterId?: string | null;
  isPostable?: boolean;
  isActive?: boolean;
}

const BASE_PATH = "/api/v1/masterdata/cost-centers";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listCostCenters(top = 50, skip = 0): Promise<PagedResult<CostCenter>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getCostCenter(id: string): Promise<CostCenter> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createCostCenter(input: CreateCostCenterInput): Promise<CostCenter> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function updateCostCenter(id: string, input: UpdateCostCenterInput): Promise<CostCenter> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitCostCenter(id: string): Promise<CostCenter> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveCostCenter(id: string): Promise<CostCenter> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectCostCenter(id: string): Promise<CostCenter> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
