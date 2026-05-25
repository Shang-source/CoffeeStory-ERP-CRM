export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
    public readonly traceId?: string,
    public readonly errors?: Record<string, string[]>,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function readApiError(response: Response) {
  try {
    const body = await response.json();
    return new ApiError(
      readValidationMessage(body) ?? friendlyErrorMessage(body.code) ?? body.message ?? body.title ?? `Request failed with ${response.status}`,
      response.status,
      body.code,
      body.traceId,
      body.errors,
    );
  } catch {
    return new ApiError(`Request failed with ${response.status}`, response.status);
  }
}

function readValidationMessage(body: { errors?: Record<string, string[]> }) {
  const firstError = Object.values(body.errors ?? {})[0]?.[0];
  return firstError || undefined;
}

function friendlyErrorMessage(code?: string) {
  if (code === 'persistence_concurrency_conflict') {
    return 'The data changed while processing this request. Please refresh and retry.';
  }

  return undefined;
}
