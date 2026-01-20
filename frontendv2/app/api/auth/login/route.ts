import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { LoginRequest, LoginResponse } from "@/lib/api/types";
import { AUTH_COOKIE_NAME } from "@/lib/auth/constants";

function getBackendBaseUrl(): string {
  const base = process.env.BACKEND_BASE_URL ?? process.env.NEXT_PUBLIC_BACKEND_BASE_URL ?? "http://localhost:5000";
  return base.replace(/\/$/, "");
}

export async function POST(req: Request) {
  let body: LoginRequest;
  try {
    body = (await req.json()) as LoginRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await fetch(`${getBackendBaseUrl()}${ApiRoutes.auth.login}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body),
  });

  if (!res.ok) {
    return NextResponse.json({ error: "invalid_credentials" }, { status: res.status });
  }

  const data = (await res.json()) as LoginResponse;
  const response = NextResponse.json({ user: data.user, expiresAt: data.expiresAt });

  const expires = new Date(data.expiresAt);
  response.cookies.set(AUTH_COOKIE_NAME, data.accessToken, {
    httpOnly: true,
    sameSite: "lax",
    secure: process.env.NODE_ENV === "production",
    path: "/",
    expires,
  });

  return response;
}

