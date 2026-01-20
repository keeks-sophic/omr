import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";
import { isPublicPath, isRouteAllowed } from "./src/lib/roleAccess";

export function middleware(req: NextRequest) {
  const { pathname } = req.nextUrl;
  if (isPublicPath(pathname)) return NextResponse.next();
  const token = req.cookies.get("auth_token")?.value;
  if (!token) {
    const url = req.nextUrl.clone();
    url.pathname = "/login";
    url.search = pathname === "/" ? "" : `?next=${encodeURIComponent(pathname)}`;
    return NextResponse.redirect(url);
  }
  try {
    const parts = token.split(".");
    if (parts.length >= 2) {
      let b64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
      const pad = b64.length % 4;
      if (pad) b64 += "=".repeat(4 - pad);
      const json = JSON.parse(atob(b64));
      let roles: string[] = [];
      if (Array.isArray(json.roles)) roles = json.roles as string[];
      else if (typeof json.roles === "string") roles = [json.roles as string];
      if (!isRouteAllowed(pathname, roles)) {
        const url = req.nextUrl.clone();
        url.pathname = "/";
        url.search = "";
        return NextResponse.redirect(url);
      }
    }
  } catch {}
  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
