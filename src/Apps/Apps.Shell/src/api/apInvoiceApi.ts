import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface APInvoice {
  id: string;
  documentNumber: string | null;
  status: string;
  vendorId: string;
  vendorInvoiceNumber: string;
  invoiceDate: string;
  description: string;
  expenseAccountId: string;
  payableAccountId: string;
  costCenterId: string | null;
  taxCodeId: string | null;
  taxRate: number;
  vatAccountId: string | null;
  netAmount: number;
  taxAmount: number;
  grossAmount: number;
  outstandingBalance: number;
  linkedJournalEntryId: string | null;
  sourceDocumentType: string | null;
  sourceDocumentId: string | null;
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateAPInvoiceInput {
  vendorId: string;
  vendorInvoiceNumber: string;
  invoiceDate: string;
  description: string;
  expenseAccountId: string;
  payableAccountId: string;
  netAmount: number;
  costCenterId?: string | null;
  taxCodeId?: string | null;
  vatAccountId?: string | null;
  purchaseOrderId?: string | null;
}

const BASE_PATH = "/api/v1/finance/ap-invoices";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listAPInvoices(top = 50, skip = 0): Promise<PagedResult<APInvoice>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getAPInvoice(id: string): Promise<APInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createAPInvoice(input: CreateAPInvoiceInput): Promise<APInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitAPInvoice(id: string): Promise<APInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveAPInvoice(id: string): Promise<APInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectAPInvoice(id: string): Promise<APInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function postAPInvoice(id: string): Promise<APInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/post`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function reverseAPInvoice(id: string): Promise<APInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reverse`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({}),
  });
  return handleJson(response);
}
