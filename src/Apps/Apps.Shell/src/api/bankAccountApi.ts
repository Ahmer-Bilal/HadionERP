import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface BankAccount {
  id: string;
  documentNumber: string | null;
  status: string;
  accountCode: string;
  accountName: string;
  accountNameArabic: string | null;
  bankName: string;
  iban: string | null;
  linkedGLAccountId: string;
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

export interface CreateBankAccountInput {
  accountCode: string;
  accountName: string;
  bankName: string;
  linkedGLAccountId: string;
  accountNameArabic?: string;
  iban?: string;
}

export interface UpdateBankAccountInput {
  accountName: string;
  bankName: string;
  accountNameArabic?: string;
  iban?: string;
  isActive?: boolean;
}

const BASE_PATH = "/api/v1/finance/bank-accounts";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listBankAccounts(top = 50, skip = 0): Promise<PagedResult<BankAccount>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getBankAccount(id: string): Promise<BankAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createBankAccount(input: CreateBankAccountInput): Promise<BankAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function updateBankAccount(id: string, input: UpdateBankAccountInput): Promise<BankAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitBankAccount(id: string): Promise<BankAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveBankAccount(id: string): Promise<BankAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectBankAccount(id: string): Promise<BankAccount> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
