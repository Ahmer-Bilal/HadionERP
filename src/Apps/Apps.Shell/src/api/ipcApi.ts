import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface IpcLine {
  id: string;
  commercialDocumentLineId: string;
  rate: number;
  quantityThisPeriod: number;
  quantityToDate: number;
  valueThisPeriod: number;
  valueToDate: number;
}

export interface Ipc {
  id: string;
  documentNumber: string | null;
  status: string;
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  measurementSheetId: string;
  periodStart: string;
  periodEnd: string;
  retentionPercentageApplied: number | null;
  advancePaymentPercentageApplied: number | null;
  otherDeductions: number;
  revenueAccountId: string | null;
  receivableAccountId: string | null;
  taxCodeId: string | null;
  vatAccountId: string | null;
  linkedArInvoiceId: string | null;
  grossValueToDate: number;
  grossValueThisPeriod: number;
  grossValuePreviousIpc: number;
  retentionAmount: number;
  advanceRecoveryAmount: number;
  netPayable: number;
  lines: IpcLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateIpcInput {
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  measurementSheetId: string;
  otherDeductions: number;
  revenueAccountId?: string;
  receivableAccountId?: string;
  taxCodeId?: string;
  vatAccountId?: string;
}

const BASE_PATH = "/api/v1/construction/ipcs";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listIpcs(top = 50, skip = 0): Promise<PagedResult<Ipc>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getIpc(id: string): Promise<Ipc> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createIpc(input: CreateIpcInput): Promise<Ipc> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitIpc(id: string): Promise<Ipc> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveIpc(id: string): Promise<Ipc> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectIpc(id: string): Promise<Ipc> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
