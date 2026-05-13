export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly code?: string,
    public readonly traceId?: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

export async function readApiError(response: Response) {
  try {
    const body = await response.json();
    return new ApiError(
      body.message ?? body.title ?? `Request failed with ${response.status}`,
      response.status,
      body.code,
      body.traceId,
    );
  } catch {
    return new ApiError(`Request failed with ${response.status}`, response.status);
  }
}
