import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";
import type { StatementLine } from "./incomeStatementApi";

export interface BalanceSheet {
  asOfDate: string;
  compareAsOfDate: string | null;
  assetLines: StatementLine[];
  totalAssets: number;
  compareTotalAssets: number | null;
  liabilityLines: StatementLine[];
  totalLiabilities: number;
  compareTotalLiabilities: number | null;
  equityLines: StatementLine[];
  totalEquity: number;
  compareTotalEquity: number | null;
  totalLiabilitiesAndEquity: number;
  compareTotalLiabilitiesAndEquity: number | null;
}

const BASE_PATH = "/api/v1/finance/reports";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function getBalanceSheet(asOfDate: string, compareAsOfDate?: string): Promise<BalanceSheet> {
  const params = new URLSearchParams({ asOfDate });
  if (compareAsOfDate) params.set("compareAsOfDate", compareAsOfDate);
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/balance-sheet?${params}`, { headers: authHeaders() });
  return handleJson(response);
}
