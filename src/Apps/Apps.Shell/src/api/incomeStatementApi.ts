import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface StatementLine {
  accountId: string | null;
  accountCode: string;
  accountName: string;
  amount: number;
  compareAmount: number | null;
  variance: number | null;
  variancePercent: number | null;
}

export interface IncomeStatement {
  periodStart: string;
  periodEnd: string;
  comparePeriodStart: string | null;
  comparePeriodEnd: string | null;
  revenueLines: StatementLine[];
  totalRevenue: number;
  compareTotalRevenue: number | null;
  expenseLines: StatementLine[];
  totalExpenses: number;
  compareTotalExpenses: number | null;
  netProfit: number;
  compareNetProfit: number | null;
}

const BASE_PATH = "/api/v1/finance/reports";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function getIncomeStatement(
  periodStart: string,
  periodEnd: string,
  comparePeriodStart?: string,
  comparePeriodEnd?: string,
): Promise<IncomeStatement> {
  const params = new URLSearchParams({ periodStart, periodEnd });
  if (comparePeriodStart && comparePeriodEnd) {
    params.set("comparePeriodStart", comparePeriodStart);
    params.set("comparePeriodEnd", comparePeriodEnd);
  }
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/income-statement?${params}`, { headers: authHeaders() });
  return handleJson(response);
}
