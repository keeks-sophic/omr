import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { UpdateQrRequest } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";

export async function PUT(req: Request, ctx: { params: Promise<{ mapId: string; mapVersionId: string; qrId: string }> }) {
  const { mapId, mapVersionId, qrId } = await ctx.params;
  let body: UpdateQrRequest;
  try {
    body = (await req.json()) as UpdateQrRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await backendFetch(ApiRoutes.maps.qrById(mapId, mapVersionId, qrId), {
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

