import { API_BASE_URL } from "../config";
import { authHeaders } from "./authApi";

export interface WbsElement {
  id: string;
  code: string;
  name: string;
  nameArabic: string | null;
  parentWbsElementId: string | null;
  isPlanningElement: boolean;
  isAccountAssignmentElement: boolean;
  isBillingElement: boolean;
}

export interface Project {
  id: string;
  documentNumber: string | null;
  status: string;
  projectName: string;
  projectNameArabic: string | null;
  customerId: string | null;
  startDate: string | null;
  endDate: string | null;
  wbsElements: WbsElement[];
  createdAt: string;
  createdBy: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  skip: number;
  top: number;
}

export interface CreateWbsElementInput {
  tempId: number;
  parentTempId: number | null;
  code: string;
  name: string;
  nameArabic?: string;
  isPlanningElement: boolean;
  isAccountAssignmentElement: boolean;
  isBillingElement: boolean;
}

export interface CreateProjectInput {
  projectName: string;
  projectNameArabic?: string;
  customerId?: string;
  startDate?: string;
  endDate?: string;
  wbsElements: CreateWbsElementInput[];
}

const BASE_PATH = "/api/v1/projectmanagement/projects";

async function handleJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const body = await response.json().catch(() => null);
    throw new Error(body?.detail ?? body?.title ?? `Request failed with status ${response.status}`);
  }
  return (await response.json()) as T;
}

export async function listProjects(top = 50, skip = 0): Promise<PagedResult<Project>> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}?$top=${top}&$skip=${skip}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function getProject(id: string): Promise<Project> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}`, { headers: authHeaders() });
  return handleJson(response);
}

export async function createProject(input: CreateProjectInput): Promise<Project> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(input),
  });
  return handleJson(response);
}

export async function submitProject(id: string): Promise<Project> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/submit`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function approveProject(id: string): Promise<Project> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/approve`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}

export async function rejectProject(id: string): Promise<Project> {
  const response = await fetch(`${API_BASE_URL}${BASE_PATH}/${id}/reject`, { method: "POST", headers: authHeaders() });
  return handleJson(response);
}
