import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { CreateMapRequest } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function GET() {
  const res = await backendFetch(ApiRoutes.maps.base, { method: "GET" });
  const text = await res.text();
  return new NextResponse(text, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

export async function POST(req: Request) {
  let body: CreateMapRequest;
  try {
    body = (await req.json()) as CreateMapRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await backendFetch(ApiRoutes.maps.base, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(body),
  });

  const data = await res.text();
  return new NextResponse(data, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}
