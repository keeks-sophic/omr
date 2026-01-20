import "server-only";

import { getAccessToken } from "@/lib/auth/session";

function getBackendBaseUrl(): string {
  const base = process.env.BACKEND_BASE_URL ?? process.env.NEXT_PUBLIC_BACKEND_BASE_URL ?? "http://localhost:5000";
  return base.replace(/\/$/, "");
}

export async function backendFetch(input: string, init?: RequestInit): Promise<Response> {
  const token = await getAccessToken();
  const url = input.startsWith("http") ? input : `${getBackendBaseUrl()}${input}`;
  const headers = new Headers(init?.headers);
  if (token) headers.set("Authorization", `Bearer ${token}`);
  if (!headers.has("Accept")) headers.set("Accept", "application/json");
  return fetch(url, { ...init, headers, cache: "no-store" });
}
