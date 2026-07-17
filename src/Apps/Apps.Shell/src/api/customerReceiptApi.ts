import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface CustomerReceiptAllocation {
  id: string;
  arInvoiceId: string;
  allocatedAmount: number;
}

export interface CustomerReceipt {
  id: string;
  documentNumber: string | null;
  status: string;
  customerId: string;
  bankAccountId: string;
  receiptDate: string;
  paymentMethod: string;
  reference: string | null;
  allocations: CustomerReceiptAllocation[];
  amount: number;
  linkedJournalEntryId: string | null;
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateCustomerReceiptAllocationInput {
  arInvoiceId: string;
  allocatedAmount: number;
}

export interface CreateCustomerReceiptInput {
  customerId: string;
  bankAccountId: string;
  receiptDate: string;
  paymentMethod: string;
  allocations: CreateCustomerReceiptAllocationInput[];
  reference?: string;
}

const BASE_PATH = "/api/v1/finance/customer-receipts";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listCustomerReceipts(top = 50, skip = 0): Promise<PagedResult<CustomerReceipt>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function listCustomerReceiptsForInvoice(arInvoiceId: string): Promise<PagedResult<CustomerReceipt>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?arInvoiceId=${arInvoiceId}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getCustomerReceipt(id: string): Promise<CustomerReceipt> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createCustomerReceipt(input: CreateCustomerReceiptInput): Promise<CustomerReceipt> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitCustomerReceipt(id: string): Promise<CustomerReceipt> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveCustomerReceipt(id: string): Promise<CustomerReceipt> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectCustomerReceipt(id: string): Promise<CustomerReceipt> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function postCustomerReceipt(id: string): Promise<CustomerReceipt> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/post`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function reverseCustomerReceipt(id: string, reversalDate?: string): Promise<CustomerReceipt> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reverse`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ reversalDate: reversalDate ?? null }),
  });
  return handleJson(response);
}
