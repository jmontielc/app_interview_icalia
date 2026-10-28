import { useAuthStore } from '@/stores/auth';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

/**
 * Calls the API with the in-memory bearer token. Error messages come from the server's
 * ProblemDetails `detail`, which never contains personal data.
 */
export async function api<T>(path: string, options: { method?: string; body?: unknown } = {}): Promise<T> {
  const auth = useAuthStore();
  const headers: Record<string, string> = { Accept: 'application/json' };
  if (auth.token) {
    headers.Authorization = `Bearer ${auth.token}`;
  }

  let body: BodyInit | undefined;
  if (options.body instanceof FormData) {
    body = options.body;
  } else if (options.body !== undefined) {
    headers['Content-Type'] = 'application/json';
    body = JSON.stringify(options.body);
  }

  const response = await fetch(path, {
    method: options.method ?? 'GET',
    headers,
    body,
    credentials: 'omit',
  });

  if (!response.ok) {
    if (response.status === 401) {
      auth.clear();
    }
    const problem = await response.json().catch(() => null);
    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? `Request failed (${response.status}).`);
  }

  return (await response.json()) as T;
}
