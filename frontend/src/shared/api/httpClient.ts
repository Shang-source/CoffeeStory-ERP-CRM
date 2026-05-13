import type { ApiMethod, ApiPath, ApiResponse } from './openapi';
import { readApiError } from './apiError';
import { getStoredToken } from './sessionStorage';

export async function apiRequest<Path extends ApiPath, Method extends ApiMethod<Path>>(
  _contractPath: Path,
  method: Method,
  path: string,
  init: RequestInit = {},
): Promise<ApiResponse<Path, Method>> {
  return request<ApiResponse<Path, Method>>(path, {
    ...init,
    method: method.toUpperCase(),
  });
}

export async function apiRequestNoContent<Path extends ApiPath, Method extends ApiMethod<Path>>(
  _contractPath: Path,
  method: Method,
  path: string,
  init: RequestInit = {},
) {
  await requestNoContent(path, {
    ...init,
    method: method.toUpperCase(),
  });
}

export async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(path, withAuthHeaders(init));

  if (!response.ok) {
    throw await readApiError(response);
  }

  return response.json();
}

export async function requestNoContent(path: string, init: RequestInit = {}) {
  const response = await fetch(path, withAuthHeaders(init));

  if (!response.ok) {
    throw await readApiError(response);
  }
}

export async function downloadBlob(path: string, fileName: string) {
  const response = await fetch(path, withAuthHeaders({ headers: {} }, false));

  if (!response.ok) {
    throw await readApiError(response);
  }

  const blob = await response.blob();
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

function withAuthHeaders(init: RequestInit = {}, includeJsonContentType = true): RequestInit {
  const token = getStoredToken();
  return {
    ...init,
    headers: {
      ...(includeJsonContentType ? { 'Content-Type': 'application/json' } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  };
}
