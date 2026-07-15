import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface PurchaseOrderLine {
  id: string;
  itemId: string;
  costCenterId: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  rfqLineId: string | null;
}

export interface PurchaseOrder {
  id: string;
  documentNumber: string | null;
  status: string;
  vendorId: string;
  requestForQuotationId: string | null;
  total: number;
  lines: PurchaseOrderLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreatePurchaseOrderLineInput {
  itemId: string;
  costCenterId: string;
  quantity: number;
  unitPrice: number;
}

export interface CreatePurchaseOrderInput {
  vendorId: string;
  requestForQuotationId?: string;
  lines?: CreatePurchaseOrderLineInput[];
}

const BASE_PATH = "/api/v1/procurement/purchase-orders";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listPurchaseOrders(top = 50, skip = 0): Promise<PagedResult<PurchaseOrder>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getPurchaseOrder(id: string): Promise<PurchaseOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createPurchaseOrder(input: CreatePurchaseOrderInput): Promise<PurchaseOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitPurchaseOrder(id: string): Promise<PurchaseOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approvePurchaseOrder(id: string): Promise<PurchaseOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectPurchaseOrder(id: string): Promise<PurchaseOrder> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export interface ThreeWayMatchResult {
  purchaseOrderId: string;
  apInvoiceId: string;
  vendorMatches: boolean;
  orderedTotal: number;
  receivedValue: number;
  invoicedNetAmount: number;
  withinReceived: boolean;
  withinOrdered: boolean;
  isMatched: boolean;
  varianceNotes: string[];
}

export async function checkThreeWayMatch(purchaseOrderId: string, apInvoiceId: string): Promise<ThreeWayMatchResult> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${purchaseOrderId}/three-way-match?apInvoiceId=${apInvoiceId}`, { headers: authHeaders() });
  return handleJson(response);
}
