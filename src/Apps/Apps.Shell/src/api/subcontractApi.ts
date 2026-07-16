import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface SubcontractLine {
  id: string;
  code: string;
  description: string;
  descriptionArabic: string | null;
  unitOfMeasure: string;
  quantity: number;
  rate: number;
  amount: number;
  wbsElementId: string;
}

export interface BackCharge {
  id: string;
  description: string;
  amount: number;
  dateIncurred: string;
}

export interface Subcontract {
  id: string;
  documentNumber: string | null;
  status: string;
  projectId: string;
  contractId: string | null;
  subcontractorId: string;
  retentionPercentage: number | null;
  mobilizationAdvancePercentage: number | null;
  defectsLiabilityPeriodMonths: number | null;
  subcontractValue: number;
  totalBackCharges: number;
  netPayableValue: number;
  lines: SubcontractLine[];
  backCharges: BackCharge[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateSubcontractLineInput {
  code: string;
  description: string;
  descriptionArabic?: string;
  unitOfMeasure: string;
  quantity: number;
  rate: number;
  wbsElementId: string;
}

export interface CreateSubcontractInput {
  projectId: string;
  contractId?: string;
  subcontractorId: string;
  retentionPercentage?: number;
  mobilizationAdvancePercentage?: number;
  defectsLiabilityPeriodMonths?: number;
  lines: CreateSubcontractLineInput[];
}

export interface AddBackChargeInput {
  description: string;
  amount: number;
  dateIncurred: string;
}

const BASE_PATH = "/api/v1/construction/subcontracts";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listSubcontracts(top = 50, skip = 0): Promise<PagedResult<Subcontract>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getSubcontract(id: string): Promise<Subcontract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createSubcontract(input: CreateSubcontractInput): Promise<Subcontract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitSubcontract(id: string): Promise<Subcontract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveSubcontract(id: string): Promise<Subcontract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectSubcontract(id: string): Promise<Subcontract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function addBackCharge(id: string, input: AddBackChargeInput): Promise<Subcontract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/back-charges`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}
