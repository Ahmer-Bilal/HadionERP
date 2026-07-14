import { API_BASE_URL } from "../config";

export interface BusinessPartnerAddress {
  id: string;
  addressType: string;
  country: string | null;
  city: string | null;
  addressLine: string | null;
}

export interface BusinessPartnerContact {
  id: string;
  name: string;
  jobTitle: string | null;
  email: string | null;
  phone: string | null;
}

export interface BusinessPartner {
  id: string;
  documentNumber: string | null;
  status: string;
  name: string;
  nameArabic: string | null;
  partnerType: string;
  taxRegistrationNumber: string | null;
  addresses: BusinessPartnerAddress[];
  contacts: BusinessPartnerContact[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateBusinessPartnerInput {
  name: string;
  partnerType: string;
  taxRegistrationNumber?: string;
  nameArabic?: string;
}

export interface AddBusinessPartnerAddressInput {
  addressType: string;
  country?: string;
  city?: string;
  addressLine?: string;
}

export interface AddBusinessPartnerContactInput {
  name: string;
  jobTitle?: string;
  email?: string;
  phone?: string;
}

export interface Attachment {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedBy: string;
  uploadedAt: string;
}

export interface Note {
  id: string;
  text: string;
  createdBy: string;
  createdAt: string;
}

const BASE_PATH = "/api/v1/masterdata/business-partners";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listBusinessPartners(top = 50, skip = 0): Promise<PagedResult<BusinessPartner>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`);
  return handleJson(response);
}

export async function getBusinessPartner(id: string): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`);
  return handleJson(response);
}

export async function createBusinessPartner(input: CreateBusinessPartnerInput): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function addBusinessPartnerAddress(
  id: string,
  input: AddBusinessPartnerAddressInput,
): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/addresses`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function addBusinessPartnerContact(
  id: string,
  input: AddBusinessPartnerContactInput,
): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/contacts`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitBusinessPartner(id: string): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST" });
  return handleJson(response);
}

export async function approveBusinessPartner(id: string): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST" });
  return handleJson(response);
}

export async function rejectBusinessPartner(id: string): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST" });
  return handleJson(response);
}

export async function uploadBusinessPartnerAttachment(id: string, file: File): Promise<Attachment> {
  const formData = new FormData();
  formData.append("file", file);
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/attachments`, {
    method: "POST",
    body: formData,
  });
  return handleJson(response);
}

export async function listBusinessPartnerAttachments(id: string): Promise<Attachment[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/attachments`);
  return handleJson(response);
}

export function businessPartnerAttachmentDownloadUrl(id: string, attachmentId: string): string {
  return `${API_BASE_URL}${BASE_PATH}/${id}/attachments/${attachmentId}/content`;
}

export async function deleteBusinessPartnerAttachment(id: string, attachmentId: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/attachments/${attachmentId}`, { method: "DELETE" });
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
}

export async function addBusinessPartnerNote(id: string, text: string): Promise<Note> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/notes`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ text }),
  });
  return handleJson(response);
}

export async function listBusinessPartnerNotes(id: string): Promise<Note[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/notes`);
  return handleJson(response);
}

export async function deleteBusinessPartnerNote(id: string, noteId: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/notes/${noteId}`, { method: "DELETE" });
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
}
