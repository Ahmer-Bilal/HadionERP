import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface AuditHistoryEntry {
  occurredAt: string;
  action: string;
  actor: string;
  summary: string;
}

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listAuditHistory(targetType: string, targetId: string): Promise<AuditHistoryEntry[]> {
  const response = await fetch(
    `${API_BASE_URL}/api/v1/audit-history?targetType=${encodeURIComponent(targetType)}&targetId=${targetId}`,
    { headers: authHeaders() });
  return handleJson(response);
}
