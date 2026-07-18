import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface Note {
  id: string;
  businessObjectType: string;
  businessObjectId: string;
  text: string;
  createdBy: string;
  createdAt: string;
}

const BASE_PATH = "/api/v1/notes";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listNotes(businessObjectType: string, businessObjectId: string): Promise<Note[]> {
  const response = await fetch(
    `${API_BASE_URL}${BASE_PATH}?businessObjectType=${encodeURIComponent(businessObjectType)}&businessObjectId=${businessObjectId}`,
    { headers: authHeaders() });
  return handleJson(response);
}

export async function addNote(businessObjectType: string, businessObjectId: string, text: string): Promise<Note> {
  const response = await fetch(
    `${API_BASE_URL}${BASE_PATH}?businessObjectType=${encodeURIComponent(businessObjectType)}&businessObjectId=${businessObjectId}`,
    { method: "POST", headers: { "Content-Type": "application/json", ...authHeaders() }, body: JSON.stringify({ text }) });
  return handleJson(response);
}

export async function deleteNote(id: string): Promise<void> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { method: "DELETE", headers: authHeaders() });
  if (!response.ok) throw new Error(`Request failed with status ${response.status}`);
}
