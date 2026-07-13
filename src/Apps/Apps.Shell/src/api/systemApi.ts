import { API_BASE_URL } from "../config";
import type { SupportedLanguageCode } from "../i18n/language";

export interface SystemStatus {
  application: string;
  phase: string;
  utcNow: string;
  kernelServicesWired: string[];
  supportedLanguages: string[];
}

export interface SystemGreeting {
  language: string;
  direction: "ltr" | "rtl";
  message: string;
}

async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${path}`);

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
