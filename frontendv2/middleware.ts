import { NextRequest, NextResponse } from "next/server";

import { AUTH_COOKIE_NAME } from "@/lib/auth/constants";

type JwtPayload = {
  roles?: string[] | string;
  exp?: number;
};

function decodeJwtPayload(token: string): JwtPayload | null {
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const base64Url = parts[1];
    const normalized = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const padLength = (4 - (normalized.length % 4)) % 4;
    const padded = normalized + "=".repeat(padLength);
    const json = atob(padded);
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

function getRoles(payload: JwtPayload | null): string[] {
  if (!payload) return [];
  const roles = payload.roles;
  if (!roles) return [];
  if (Array.isArray(roles)) return roles.filter((r) => typeof r === "string" && r.trim() !== "");
  if (typeof roles === "string" && roles.trim() !== "") return [roles];
  return [];
}

function isExpired(payload: JwtPayload | null): boolean {
  const exp = payload?.exp;
  if (!exp) return false;
  const now = Math.floor(Date.now() / 1000);
  return exp <= now;
}

function toLogin(req: NextRequest): NextResponse {
  const url = req.nextUrl.clone();
  url.pathname = "/login";
  url.searchParams.set("returnTo", req.nextUrl.pathname + req.nextUrl.search);
  const res = NextResponse.redirect(url);
  res.cookies.set(AUTH_COOKIE_NAME, "", { path: "/", maxAge: 0 });
  return res;
}

export function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;

  if (pathname.startsWith("/_next")) return NextResponse.next();
  if (pathname.startsWith("/api")) return NextResponse.next();
  if (pathname.startsWith("/favicon")) return NextResponse.next();

  const isPublic =
    pathname === "/login" ||
    pathname === "/register" ||
    pathname === "/pending" ||
    pathname === "/access-denied";

  const token = req.cookies.get(AUTH_COOKIE_NAME)?.value ?? null;
  if (!token) {
    return isPublic ? NextResponse.next() : toLogin(req);
  }

  const payload = decodeJwtPayload(token);
  if (!payload || isExpired(payload)) {
    return toLogin(req);
  }

  const roles = getRoles(payload);
  const isAdminRoute = pathname.startsWith("/admin");
  const isAccountRoute = pathname.startsWith("/account");
  const hasNonPendingRole = roles.some((r) => r === "Viewer" || r === "Operator" || r === "Admin");

  if (isAdminRoute && !roles.includes("Admin")) {
    const url = req.nextUrl.clone();
    url.pathname = "/access-denied";
    return NextResponse.redirect(url);
  }

  if (isAccountRoute) return NextResponse.next();
  if (isPublic) return NextResponse.next();

  if (!hasNonPendingRole) {
    const url = req.nextUrl.clone();
    url.pathname = "/pending";
    return NextResponse.redirect(url);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!.*\\..*).*)"],
};

