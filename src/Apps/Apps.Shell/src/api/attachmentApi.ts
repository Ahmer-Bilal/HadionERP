import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface AttachmentMetadata {
  id: string;
  businessObjectType: string;
  businessObjectId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
}

const BASE_PATH = "/api/v1/attachments";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listAttachments(businessObjectType: string, businessObjectId: string): Promise<AttachmentMetadata[]> {
  const response = await fetch(
    `${API_BASE_URL}${BASE_PATH}?businessObjectType=${encodeURIComponent(businessObjectType)}&businessObjectId=${businessObjectId}`,
    { headers: authHeaders() });
  return handleJson(response);
}

export async function uploadAttachment(businessObjectType: string, businessObjectId: string, file: File): Promise<AttachmentMetadata> {
  const formData = new FormData();
  formData.append("file", file);
  const response = await fetch(
    `${API_BASE_URL}${BASE_PATH}?businessObjectType=${encodeURIComponent(businessObjectType)}&businessObjectId=${businessObjectId}`,
    { method: "POST", headers: authHeaders(), body: formData });
  return handleJson(response);
}

// A plain <a href> can't carry the Authorization header this endpoint requires, so downloading fetches the
// bytes with auth first, then hands the browser a local object URL to actually save — the file never
// reaches an unauthenticated request.
export async function downloadAttachment(id: string, fileName: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/download`, { headers: authHeaders() });
  if (!response.ok) throw new Error(`Request failed with status ${response.status}`);
  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  link.click();
  URL.revokeObjectURL(url);
}

export async function deleteAttachment(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { method: "DELETE", headers: authHeaders() });
  if (!response.ok) throw new Error(`Request failed with status ${response.status}`);
}
