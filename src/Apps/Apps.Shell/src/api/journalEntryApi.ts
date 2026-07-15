import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface JournalLine {
  id: string;
  glAccountId: string;
  costCenterId: string | null;
  debitAmount: number;
  creditAmount: number;
  lineDescription: string | null;
}

export interface JournalEntry {
  id: string;
  documentNumber: string | null;
  status: string;
  postingDate: string;
  description: string;
  reversalOfEntryId: string | null;
  totalDebits: number;
  totalCredits: number;
  isBalanced: boolean;
  lines: JournalLine[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateJournalLineInput {
  glAccountId: string;
  debitAmount: number;
  creditAmount: number;
  costCenterId?: string | null;
  lineDescription?: string;
}

export interface CreateJournalEntryInput {
  postingDate: string;
  description: string;
  lines: CreateJournalLineInput[];
}

const BASE_PATH = "/api/v1/finance/journal-entries";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listJournalEntries(top = 50, skip = 0): Promise<PagedResult<JournalEntry>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getJournalEntry(id: string): Promise<JournalEntry> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createJournalEntry(input: CreateJournalEntryInput): Promise<JournalEntry> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitJournalEntry(id: string): Promise<JournalEntry> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveJournalEntry(id: string): Promise<JournalEntry> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectJournalEntry(id: string): Promise<JournalEntry> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function postJournalEntry(id: string): Promise<JournalEntry> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/post`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function reverseJournalEntry(id: string): Promise<JournalEntry> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reverse`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({}),
  });
  return handleJson(response);
}
