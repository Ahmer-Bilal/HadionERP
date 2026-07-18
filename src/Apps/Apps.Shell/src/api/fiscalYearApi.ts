import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface FiscalPeriod {
  id: string;
  periodNumber: number;
  startDate: string;
  endDate: string;
  isOpen: boolean;
  targetCloseDate: string;
}

export interface FiscalYear {
  id: string;
  year: number;
  periods: FiscalPeriod[];
  createdAt: string;
  createdBy: string;
}

export interface ClosingActivityStep {
  id: string;
  description: string;
  isAutoTracked: boolean;
  isCompleted: boolean;
  completedBy: string | null;
  completedAt: string | null;
}

export interface ClosingActivity {
  id: string;
  fiscalPeriodId: string;
  activityKey: string;
  sequenceNumber: number;
  title: string;
  description: string;
  assignedToUserId: string | null;
  assignedToDisplayName: string | null;
  assignedToRoleKey: string | null;
  dueDate: string | null;
  status: "NotStarted" | "InProgress" | "Completed" | "Blocked";
  completedSteps: number;
  totalSteps: number;
  steps: ClosingActivityStep[];
  lastActionBy: string | null;
  lastActionAt: string | null;
}

export interface ClosingInsight {
  severity: "OnTrack" | "AttentionRequired" | "BestPractice";
  title: string;
  message: string;
}

export interface ClosingActivityLogEntry {
  at: string;
  actor: string;
  message: string;
}

export interface CompletionTrendPoint {
  date: string;
  percentComplete: number;
}

export interface ActiveUser {
  id: string;
  username: string;
  displayName: string;
  isActive: boolean;
  roleKeys: string[];
}

const BASE_PATH = "/api/v1/finance/fiscal-years";
const ACTIVITY_BASE_PATH = "/api/v1/finance/closing-activities";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listFiscalYears(): Promise<FiscalYear[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createFiscalYear(year: number): Promise<FiscalYear> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ year }),
  });
  return handleJson(response);
}

export async function closePeriod(fiscalYearId: string, periodNumber: number): Promise<FiscalYear> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${fiscalYearId}/periods/${periodNumber}/close`, {
    method: "POST", headers: authHeaders(),
  });
  return handleJson(response);
}

export async function reopenPeriod(fiscalYearId: string, periodNumber: number): Promise<FiscalYear> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${fiscalYearId}/periods/${periodNumber}/reopen`, {
    method: "POST", headers: authHeaders(),
  });
  return handleJson(response);
}

export async function setTargetCloseDate(fiscalYearId: string, periodNumber: number, targetCloseDate: string): Promise<FiscalYear> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${fiscalYearId}/periods/${periodNumber}/target-close-date`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ targetCloseDate }),
  });
  return handleJson(response);
}

export async function getClosingChecklist(fiscalYearId: string, periodNumber: number): Promise<ClosingActivity[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${fiscalYearId}/periods/${periodNumber}/closing-checklist`, {
    headers: authHeaders(),
  });
  return handleJson(response);
}

export async function getInsights(fiscalYearId: string, periodNumber: number): Promise<ClosingInsight[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${fiscalYearId}/periods/${periodNumber}/insights`, {
    headers: authHeaders(),
  });
  return handleJson(response);
}

export async function getActivityLog(fiscalYearId: string, periodNumber: number, take = 20): Promise<ClosingActivityLogEntry[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${fiscalYearId}/periods/${periodNumber}/activity-log?take=${take}`, {
    headers: authHeaders(),
  });
  return handleJson(response);
}

export async function getCompletionTrend(fiscalYearId: string, periodNumber: number): Promise<CompletionTrendPoint[]> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${fiscalYearId}/periods/${periodNumber}/completion-trend`, {
    headers: authHeaders(),
  });
  return handleJson(response);
}

export async function assignClosingActivity(activityId: string, userId: string, dueDate: string | null): Promise<ClosingActivity> {
  const response = await fetch(`${API_BASE_URL}${ACTIVITY_BASE_PATH}/${activityId}/assign`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ userId, dueDate }),
  });
  return handleJson(response);
}

export async function toggleClosingActivityStep(activityId: string, stepId: string, isCompleted: boolean): Promise<ClosingActivity> {
  const response = await fetch(`${API_BASE_URL}${ACTIVITY_BASE_PATH}/${activityId}/steps/${stepId}/toggle`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ isCompleted }),
  });
  return handleJson(response);
}

export async function setClosingActivityBlocked(activityId: string, isBlocked: boolean): Promise<ClosingActivity> {
  const response = await fetch(`${API_BASE_URL}${ACTIVITY_BASE_PATH}/${activityId}/blocked`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ isBlocked }),
  });
  return handleJson(response);
}

export async function listAssignableUsers(): Promise<ActiveUser[]> {
  const response = await fetch(`${API_BASE_URL}${ACTIVITY_BASE_PATH}/assignable-users`, { headers: authHeaders() });
  return handleJson(response);
}
