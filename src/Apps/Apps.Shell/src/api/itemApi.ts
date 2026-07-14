import { API_BASE_URL } from "../config";

export interface Item {
  id: string;
  documentNumber: string | null;
  status: string;
  itemCode: string;
  itemName: string;
  itemNameArabic: string | null;
  itemType: string;
  unitOfMeasure: string;
  isActive: boolean;
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateItemInput {
  itemCode: string;
  itemName: string;
  itemType: string;
  unitOfMeasure: string;
  itemNameArabic?: string;
}

export interface UpdateItemInput {
  itemName: string;
  unitOfMeasure: string;
  itemNameArabic?: string;
  isActive?: boolean;
}

const BASE_PATH = "/api/v1/masterdata/items";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listItems(top = 50, skip = 0): Promise<PagedResult<Item>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`);
  return handleJson(response);
}

export async function getItem(id: string): Promise<Item> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`);
  return handleJson(response);
}

export async function createItem(input: CreateItemInput): Promise<Item> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function updateItem(id: string, input: UpdateItemInput): Promise<Item> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitItem(id: string): Promise<Item> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST" });
  return handleJson(response);
}

export async function approveItem(id: string): Promise<Item> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST" });
  return handleJson(response);
}

export async function rejectItem(id: string): Promise<Item> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST" });
  return handleJson(response);
}
