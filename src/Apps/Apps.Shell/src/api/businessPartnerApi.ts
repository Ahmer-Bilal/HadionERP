import { API_BASE_URL } from "../config";

export interface BusinessPartner {
  id: string;
  documentNumber: string | null;
  status: string;
  name: string;
  partnerType: string;
  taxRegistrationNumber: string | null;
  email: string | null;
  phone: string | null;
  country: string | null;
  city: string | null;
  addressLine: string | null;
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
  email?: string;
  phone?: string;
  country?: string;
  city?: string;
  addressLine?: string;
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

export async function submitBusinessPartner(id: string): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST" });
  return handleJson(response);
}

export async function approveBusinessPartner(id: string): Promise<BusinessPartner> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST" });
  return handleJson(response);
}
