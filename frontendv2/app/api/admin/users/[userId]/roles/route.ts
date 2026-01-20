import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { AdminAssignRolesRequest } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function POST(req: Request, ctx: { params: Promise<{ userId: string }> }) {
  const { userId } = await ctx.params;
  let body: AdminAssignRolesRequest;
  try {
    body = (await req.json()) as AdminAssignRolesRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await backendFetch(ApiRoutes.adminUsers.roles(userId), {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  const text = await res.text();
  return new NextResponse(text, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

