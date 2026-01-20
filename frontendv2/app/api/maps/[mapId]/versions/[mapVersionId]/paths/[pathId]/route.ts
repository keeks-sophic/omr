import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { UpdatePathRequest } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function PUT(req: Request, ctx: { params: Promise<{ mapId: string; mapVersionId: string; pathId: string }> }) {
  const { mapId, mapVersionId, pathId } = await ctx.params;
  let body: UpdatePathRequest;
  try {
    body = (await req.json()) as UpdatePathRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await backendFetch(ApiRoutes.maps.pathById(mapId, mapVersionId, pathId), {
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

