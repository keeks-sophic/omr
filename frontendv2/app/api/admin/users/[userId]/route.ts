import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { UserDto } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function GET(_: Request, ctx: { params: Promise<{ userId: string }> }) {
  const { userId } = await ctx.params;
  const res = await backendFetch(ApiRoutes.adminUsers.byId(userId), { method: "GET" });
  if (!res.ok) {
    return NextResponse.json({ error: "not_found" }, { status: res.status });
  }
  const data = (await res.json()) as UserDto;
  return NextResponse.json(data);
}

