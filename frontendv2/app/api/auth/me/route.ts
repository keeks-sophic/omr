import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { MeResponse } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function GET() {
  const res = await backendFetch(ApiRoutes.auth.me, { method: "GET" });
  if (!res.ok) {
    return NextResponse.json({ error: "unauthorized" }, { status: res.status });
  }
  const data = (await res.json()) as MeResponse;
  return NextResponse.json(data);
}

