import { API_BASE_URL } from "../config";

export interface RfqLine {
  id: string;
  purchaseRequisitionLineId: string;
  itemId: string;
  quantity: number;
}

export interface RfqInvitedVendor {
  id: string;
  vendorId: string;
}

export interface RfqVendorQuoteLine {
  id: string;
  vendorId: string;
  rfqLineId: string;
  quotedUnitPrice: number;
}

export interface RequestForQuotation {
  id: string;
  documentNumber: string | null;
  status: string;
  purchaseRequisitionId: string;
  description: string;
  responseDeadline: string | null;
  lines: RfqLine[];
  invitedVendors: RfqInvitedVendor[];
  vendorQuoteLines: RfqVendorQuoteLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateRequestForQuotationInput {
  purchaseRequisitionId: string;
  description: string;
  invitedVendorIds: string[];
  responseDeadline?: string;
}

export interface RecordVendorQuoteInput {
  vendorId: string;
  rfqLineId: string;
  quotedUnitPrice: number;
}

const BASE_PATH = "/api/v1/procurement/requests-for-quotation";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listRequestsForQuotation(top = 50, skip = 0): Promise<PagedResult<RequestForQuotation>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`);
  return handleJson(response);
}

export async function getRequestForQuotation(id: string): Promise<RequestForQuotation> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`);
  return handleJson(response);
}

export async function createRequestForQuotation(input: CreateRequestForQuotationInput): Promise<RequestForQuotation> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitRequestForQuotation(id: string): Promise<RequestForQuotation> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST" });
  return handleJson(response);
}

export async function approveRequestForQuotation(id: string): Promise<RequestForQuotation> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST" });
  return handleJson(response);
}

export async function rejectRequestForQuotation(id: string): Promise<RequestForQuotation> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST" });
  return handleJson(response);
}

export async function recordVendorQuote(id: string, input: RecordVendorQuoteInput): Promise<RequestForQuotation> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/vendor-quotes`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}
