import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface VendorPrequalification {
  id: string;
  documentNumber: string | null;
  status: string;
  businessPartnerId: string;
  roleType: string;
  trade: string | null;
  validFrom: string | null;
  validUntil: string | null;
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateVendorPrequalificationInput {
  businessPartnerId: string;
  roleType: string;
  trade?: string;
}

export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
}

const BASE_PATH = "/api/v1/procurement/vendor-prequalifications";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listVendorPrequalifications(top = 50, skip = 0): Promise<PagedResult<VendorPrequalification>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getVendorPrequalification(id: string): Promise<VendorPrequalification> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createVendorPrequalification(
  input: CreateVendorPrequalificationInput,
): Promise<VendorPrequalification> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitVendorPrequalification(id: string): Promise<VendorPrequalification> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveVendorPrequalification(id: string): Promise<VendorPrequalification> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectVendorPrequalification(id: string): Promise<VendorPrequalification> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function uploadVendorPrequalificationAttachment(id: string, file: File): Promise<Attachment> {
  const formData = new FormData();
  formData.append("file", file);
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/attachments`, {
    method: "POST",
    headers: authHeaders(),
    body: formData,
  });
  return handleJson(response);
}

export async function listVendorPrequalificationAttachments(id: string): Promise<Attachment[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/attachments`, { headers: authHeaders() });
  return handleJson(response);
}

export function vendorPrequalificationAttachmentDownloadUrl(id: string, attachmentId: string): string {
  return `${API_BASE_URL}${BASE_PATH}/${id}/attachments/${attachmentId}/content`;
}

export async function deleteVendorPrequalificationAttachment(id: string, attachmentId: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/attachments/${attachmentId}`, { method: "DELETE", headers: authHeaders() });
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
}
