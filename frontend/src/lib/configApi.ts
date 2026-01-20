import { getToken } from "./auth";

export async function getRobots(baseUrl: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/robots`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch robots");
  return res.json();
}

export async function putMotionLimits(baseUrl: string, robotId: string, limits: { maxDriveSpeed: number; maxAcceleration: number; maxDeceleration: number }) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/config/motion-limits`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(limits),
  });
  if (!res.ok) throw new Error("Failed to update motion limits");
  return res.json();
}

export async function putRuntimeMode(baseUrl: string, robotId: string, runtimeMode: "LIVE" | "SIM") {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/config/runtime-mode`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify({ runtimeMode }),
  });
  if (!res.ok) throw new Error("Failed to update runtime mode");
  return res.json();
}

export async function putFeatureFlags(baseUrl: string, robotId: string, flags: { telescopeEnabled: boolean }) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/config/feature-flags`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(flags),
  });
  if (!res.ok) throw new Error("Failed to update feature flags");
  return res.json();
}
