import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";
import type { SupportedLanguageCode } from "../i18n/language";

export interface EventsOutboxStatus {
  published: number;
  pending: number;
}

export interface AuditStatus {
  entries: number;
  chainValid: boolean;
}

export interface ConfigurationStatus {
  defaultLanguage: string | null;
  verboseSystemStatusEnabled: boolean;
}

export interface SystemStatus {
  application: string;
  phase: string;
  utcNow: string;
  kernelServicesWired: string[];
  supportedLanguages: string[];
  configuration: ConfigurationStatus;
  // Only present when configuration.verboseSystemStatusEnabled is true — Platform.Configuration's
  // feature-flag mechanism genuinely gates whether the backend includes these, not just a UI toggle.
  eventsOutbox?: EventsOutboxStatus;
  audit?: AuditStatus;
}

export interface SystemGreeting {
  language: string;
  direction: "ltr" | "rtl";
  message: string;
}

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`, { headers: authHeaders() });

  if (!response.ok) {
    throw new Error(`Request to ${path} failed with status ${response.status}`);
  }

  return (await response.json()) as T;
}

export function fetchHealth(): Promise<string> {
  return fetch(`${API_BASE_URL}/health`).then((response) => response.text());
}

export function fetchSystemStatus(): Promise<SystemStatus> {
  return getJson<SystemStatus>("/api/v1/system/status");
}

export function fetchGreeting(language: SupportedLanguageCode): Promise<SystemGreeting> {
  return getJson<SystemGreeting>(`/api/v1/system/greeting?lang=${language}`);
}
