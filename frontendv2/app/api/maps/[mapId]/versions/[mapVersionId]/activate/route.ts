import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import { backendFetch } from "@/lib/api/backendClient";

export async function POST(_: Request, ctx: { params: Promise<{ mapId: string; mapVersionId: string }> }) {
  const { mapId, mapVersionId } = await ctx.params;

  const res = await backendFetch(ApiRoutes.maps.activate(mapId, mapVersionId), {
    method: "POST",
  });

  const text = await res.text();
  return new NextResponse(text, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

