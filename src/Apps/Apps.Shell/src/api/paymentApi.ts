import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface PaymentAllocation {
  id: string;
  apInvoiceId: string;
  allocatedAmount: number;
}

export interface Payment {
  id: string;
  documentNumber: string | null;
  status: string;
  vendorId: string;
  bankAccountId: string;
  paymentDate: string;
  paymentMethod: string;
  reference: string | null;
  allocations: PaymentAllocation[];
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

export interface CreatePaymentAllocationInput {
  apInvoiceId: string;
  allocatedAmount: number;
}

export interface CreatePaymentInput {
  vendorId: string;
  bankAccountId: string;
  paymentDate: string;
  paymentMethod: string;
  allocations: CreatePaymentAllocationInput[];
  reference?: string;
}

const BASE_PATH = "/api/v1/finance/payments";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listPayments(top = 50, skip = 0): Promise<PagedResult<Payment>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function listPaymentsForInvoice(apInvoiceId: string): Promise<PagedResult<Payment>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?apInvoiceId=${apInvoiceId}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getPayment(id: string): Promise<Payment> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createPayment(input: CreatePaymentInput): Promise<Payment> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitPayment(id: string): Promise<Payment> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approvePayment(id: string): Promise<Payment> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectPayment(id: string): Promise<Payment> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function postPayment(id: string): Promise<Payment> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/post`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function reversePayment(id: string, reversalDate?: string): Promise<Payment> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reverse`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ reversalDate: reversalDate ?? null }),
  });
  return handleJson(response);
}
