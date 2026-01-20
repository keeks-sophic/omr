import { NextResponse } from "next/server";

import { ApiRoutes } from "@/lib/api/routes";
import type { RegisterRequest, RegisterResponse } from "@/lib/api/types";

function getBackendBaseUrl(): string {
  const base = process.env.BACKEND_BASE_URL ?? process.env.NEXT_PUBLIC_BACKEND_BASE_URL ?? "http://localhost:5000";
  return base.replace(/\/$/, "");
}

export async function POST(req: Request) {
  let body: RegisterRequest;
  try {
    body = (await req.json()) as RegisterRequest;
  } catch {
    return NextResponse.json({ error: "invalid_json" }, { status: 400 });
  }

  const res = await fetch(`${getBackendBaseUrl()}${ApiRoutes.auth.register}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body),
  });

  const text = await res.text();
  if (!res.ok) {
    return NextResponse.json({ error: "register_failed", detail: text }, { status: res.status });
  }

  const data = JSON.parse(text) as RegisterResponse;
  return NextResponse.json(data);
}

