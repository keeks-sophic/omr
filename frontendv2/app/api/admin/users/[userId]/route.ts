import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { AdminUpdateUserRequest, UserDto } from "@/lib/api/types";
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

export async function PUT(req: Request, ctx: { params: Promise<{ userId: string }> }) {
  const { userId } = await ctx.params;
  let body: AdminUpdateUserRequest;
  try {
    body = (await req.json()) as AdminUpdateUserRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await backendFetch(ApiRoutes.adminUsers.byId(userId), {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  const text = await res.text();
  return new NextResponse(text, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

