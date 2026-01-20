import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import { backendFetch } from "@/lib/api/backendClient";
import { AUTH_COOKIE_NAME } from "@/lib/auth/constants";

export async function POST() {
  const res = await backendFetch(ApiRoutes.auth.logout, { method: "POST" });
  const response = NextResponse.json({ ok: true });
  response.cookies.set(AUTH_COOKIE_NAME, "", { path: "/", maxAge: 0 });
  void res;
  return response;
}
