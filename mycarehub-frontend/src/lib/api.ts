import { supabase } from "@/src/lib/supabase/client";

const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL;

if (!API_BASE_URL) {
  throw new Error(
    "Missing NEXT_PUBLIC_API_BASE_URL environment variable."
  );
}

const RESOLVED_API_BASE_URL = API_BASE_URL;

export class ApiError extends Error {
  readonly status: number;
  readonly body: unknown;

  constructor(status: number, message: string, body: unknown = null) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.body = body;
  }
}

async function getAccessToken(): Promise<string | null> {
  const {
    data: { session },
  } = await supabase.auth.getSession();

  return session?.access_token ?? null;
}

type ApiRequestInit = Omit<RequestInit, "body"> & {
  body?: unknown;
  /** Se true, non richiede (né invia) il JWT. Default: false. */
  skipAuth?: boolean;
};

/**
 * Bridge Next.js ↔ ASP.NET Core Web API.
 * Preleva il JWT dalla sessione Supabase e lo inserisce in Authorization: Bearer.
 */
export async function apiFetch<T>(
  path: string,
  init: ApiRequestInit = {}
): Promise<T> {
  const { body, skipAuth = false, headers: customHeaders, ...rest } = init;

  const headers = new Headers(customHeaders);

  if (!skipAuth) {
    const token = await getAccessToken();
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }
  }

  let requestBody: BodyInit | undefined;

  if (body !== undefined && body !== null) {
    if (body instanceof FormData || body instanceof Blob) {
      requestBody = body;
    } else {
      if (!headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
      }
      requestBody = JSON.stringify(body);
    }
  }

  const url = path.startsWith("http")
    ? path
    : `${RESOLVED_API_BASE_URL.replace(/\/$/, "")}/${path.replace(/^\//, "")}`;

  const response = await fetch(url, {
    ...rest,
    headers,
    body: requestBody,
  });

  if (response.status === 204) {
    return undefined as T;
  }

  const contentType = response.headers.get("Content-Type") ?? "";
  const payload = contentType.includes("application/json")
    ? await response.json()
    : await response.text();

  if (!response.ok) {
    const message =
      typeof payload === "object" &&
      payload !== null &&
      "title" in payload &&
      typeof (payload as { title: unknown }).title === "string"
        ? (payload as { title: string }).title
        : typeof payload === "string" && payload.length > 0
          ? payload
          : `API request failed with status ${response.status}`;

    throw new ApiError(response.status, message, payload);
  }

  return payload as T;
}

export const api = {
  get: <T>(path: string, init?: Omit<ApiRequestInit, "method" | "body">) =>
    apiFetch<T>(path, { ...init, method: "GET" }),

  post: <T>(
    path: string,
    body?: unknown,
    init?: Omit<ApiRequestInit, "method" | "body">
  ) => apiFetch<T>(path, { ...init, method: "POST", body }),

  put: <T>(
    path: string,
    body?: unknown,
    init?: Omit<ApiRequestInit, "method" | "body">
  ) => apiFetch<T>(path, { ...init, method: "PUT", body }),

  patch: <T>(
    path: string,
    body?: unknown,
    init?: Omit<ApiRequestInit, "method" | "body">
  ) => apiFetch<T>(path, { ...init, method: "PATCH", body }),

  delete: <T>(path: string, init?: Omit<ApiRequestInit, "method" | "body">) =>
    apiFetch<T>(path, { ...init, method: "DELETE" }),
};
