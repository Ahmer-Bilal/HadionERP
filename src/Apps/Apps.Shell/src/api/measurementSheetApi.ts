import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface MeasurementLine {
  id: string;
  commercialDocumentLineId: string;
  quantitySubmitted: number;
  quantityCertified: number | null;
  remarks: string | null;
}

export interface MeasurementSheet {
  id: string;
  documentNumber: string | null;
  status: string;
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  periodStart: string;
  periodEnd: string;
  notes: string | null;
  lines: MeasurementLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateMeasurementLineInput {
  commercialDocumentLineId: string;
  quantitySubmitted: number;
  remarks?: string;
}

export interface CreateMeasurementSheetInput {
  projectId: string;
  commercialDocumentType: string;
  commercialDocumentId: string;
  periodStart: string;
  periodEnd: string;
  notes?: string;
  lines: CreateMeasurementLineInput[];
}

export interface CertifyMeasurementLineInput {
  lineId: string;
  quantityCertified: number;
}

export interface CertifyMeasurementSheetInput {
  lines: CertifyMeasurementLineInput[];
}

const BASE_PATH = "/api/v1/construction/measurement-sheets";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listMeasurementSheets(top = 50, skip = 0): Promise<PagedResult<MeasurementSheet>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getMeasurementSheet(id: string): Promise<MeasurementSheet> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createMeasurementSheet(input: CreateMeasurementSheetInput): Promise<MeasurementSheet> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitMeasurementSheet(id: string): Promise<MeasurementSheet> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function certifyMeasurementSheet(id: string, input: CertifyMeasurementSheetInput): Promise<MeasurementSheet> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/certify`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function rejectMeasurementSheet(id: string): Promise<MeasurementSheet> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
