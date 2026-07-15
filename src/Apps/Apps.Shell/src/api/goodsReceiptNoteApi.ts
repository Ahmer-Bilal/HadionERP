import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface GrnLine {
  id: string;
  purchaseOrderLineId: string;
  itemId: string;
  quantityReceived: number;
  unitPrice: number;
  lineValue: number;
}

export interface GoodsReceiptNote {
  id: string;
  documentNumber: string | null;
  status: string;
  purchaseOrderId: string;
  receivedDate: string;
  receivedValue: number;
  lines: GrnLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateGoodsReceiptNoteLineInput {
  purchaseOrderLineId: string;
  quantityReceived: number;
}

export interface CreateGoodsReceiptNoteInput {
  purchaseOrderId: string;
  receivedDate: string;
  lines: CreateGoodsReceiptNoteLineInput[];
}

const BASE_PATH = "/api/v1/procurement/goods-receipt-notes";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listGoodsReceiptNotes(top = 50, skip = 0): Promise<PagedResult<GoodsReceiptNote>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getGoodsReceiptNote(id: string): Promise<GoodsReceiptNote> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createGoodsReceiptNote(input: CreateGoodsReceiptNoteInput): Promise<GoodsReceiptNote> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitGoodsReceiptNote(id: string): Promise<GoodsReceiptNote> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveGoodsReceiptNote(id: string): Promise<GoodsReceiptNote> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectGoodsReceiptNote(id: string): Promise<GoodsReceiptNote> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
