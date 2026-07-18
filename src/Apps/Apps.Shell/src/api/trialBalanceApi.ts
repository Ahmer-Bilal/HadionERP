import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface TrialBalanceAccount {
  accountId: string;
  accountCode: string;
  accountName: string;
  accountType: string;
  level: number;
  isHeader: boolean;
  openingDebit: number;
  openingCredit: number;
  periodDebit: number;
  periodCredit: number;
  endingDebit: number;
  endingCredit: number;
}

export interface TrialBalance {
  periodStart: string;
  periodEnd: string;
  accounts: TrialBalanceAccount[];
  totalEndingDebit: number;
  totalEndingCredit: number;
}

const BASE_PATH = "/api/v1/finance/reports";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function getTrialBalance(periodStart: string, periodEnd: string): Promise<TrialBalance> {
  const response = await fetch(
    `${API_BASE_URL}${BASE_PATH}/trial-balance?periodStart=${periodStart}&periodEnd=${periodEnd}`,
    { headers: authHeaders() },
  );
  return handleJson(response);
}
