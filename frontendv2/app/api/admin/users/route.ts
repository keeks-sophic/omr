import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { UserDto } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function GET() {
  const res = await backendFetch(ApiRoutes.adminUsers.base, { method: "GET" });
  if (!res.ok) {
    return NextResponse.json({ error: "forbidden" }, { status: res.status });
  }
  const data = (await res.json()) as UserDto[];
  return NextResponse.json(data);
}

