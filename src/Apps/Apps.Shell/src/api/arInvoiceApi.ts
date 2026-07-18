import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface ARInvoice {
  id: string;
  documentNumber: string | null;
  status: string;
  customerId: string;
  customerReference: string | null;
  invoiceDate: string;
  description: string;
  revenueAccountId: string;
  receivableAccountId: string;
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

export interface CreateARInvoiceInput {
  customerId: string;
  customerReference?: string | null;
  invoiceDate: string;
  description: string;
  revenueAccountId: string;
  receivableAccountId: string;
  netAmount: number;
  costCenterId?: string | null;
  taxCodeId?: string | null;
  vatAccountId?: string | null;
}

const BASE_PATH = "/api/v1/finance/ar-invoices";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listARInvoices(top = 50, skip = 0): Promise<PagedResult<ARInvoice>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getARInvoice(id: string): Promise<ARInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createARInvoice(input: CreateARInvoiceInput): Promise<ARInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitARInvoice(id: string): Promise<ARInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveARInvoice(id: string): Promise<ARInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectARInvoice(id: string): Promise<ARInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function postARInvoice(id: string): Promise<ARInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/post`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function reverseARInvoice(id: string): Promise<ARInvoice> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reverse`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({}),
  });
  return handleJson(response);
}
