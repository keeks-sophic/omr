import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { AdminCreateUserRequest, UserDto } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function GET() {
  const res = await backendFetch(ApiRoutes.adminUsers.base, { method: "GET" });
  if (!res.ok) {
    return NextResponse.json({ error: "forbidden" }, { status: res.status });
  }
  const data = (await res.json()) as UserDto[];
  return NextResponse.json(data);
}

export async function POST(req: Request) {
  let body: AdminCreateUserRequest;
  try {
    body = (await req.json()) as AdminCreateUserRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await backendFetch(ApiRoutes.adminUsers.base, {
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
