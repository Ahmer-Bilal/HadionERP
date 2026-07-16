import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface BoqLine {
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

export interface Contract {
  id: string;
  documentNumber: string | null;
  status: string;
  projectId: string;
  contractType: string;
  paymentTerms: string | null;
  advancePaymentPercentage: number | null;
  defectsLiabilityPeriodMonths: number | null;
  contractValue: number;
  boqLines: BoqLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateBoqLineInput {
  code: string;
  description: string;
  descriptionArabic?: string;
  unitOfMeasure: string;
  quantity: number;
  rate: number;
  wbsElementId: string;
}

export interface CreateContractInput {
  projectId: string;
  contractType: string;
  paymentTerms?: string;
  advancePaymentPercentage?: number;
  defectsLiabilityPeriodMonths?: number;
  boqLines: CreateBoqLineInput[];
}

const BASE_PATH = "/api/v1/construction/contracts";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listContracts(top = 50, skip = 0): Promise<PagedResult<Contract>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getContract(id: string): Promise<Contract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createContract(input: CreateContractInput): Promise<Contract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitContract(id: string): Promise<Contract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveContract(id: string): Promise<Contract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectContract(id: string): Promise<Contract> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
