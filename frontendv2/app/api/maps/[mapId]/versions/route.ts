import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import { backendFetch } from "@/lib/api/backendClient";

export async function GET(_: Request, ctx: { params: Promise<{ mapId: string }> }) {
  const { mapId } = await ctx.params;
  const res = await backendFetch(ApiRoutes.maps.versions(mapId), { method: "GET" });
  const text = await res.text();
  return new NextResponse(text, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

