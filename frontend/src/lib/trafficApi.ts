import { getToken } from "./auth";

function authHeaders() {
  const token = getToken();
  return { Authorization: `Bearer ${token}` };
}

export async function fetchTrafficConflicts(baseUrl: string) {
  const res = await fetch(`${baseUrl}/api/v1/traffic/conflicts`, {
    headers: { ...authHeaders() },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch traffic conflicts");
  return res.json();
}

export async function createTrafficHold(baseUrl: string, payload: { nodeId?: string; pathId?: string; durationMs: number; reason?: string }) {
  const res = await fetch(`${baseUrl}/api/v1/traffic/holds`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to create traffic hold");
  return res.json();
}

export async function deleteTrafficHold(baseUrl: string, holdId: string) {
  const res = await fetch(`${baseUrl}/api/v1/traffic/holds/${encodeURIComponent(holdId)}`, {
    method: "DELETE",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to delete traffic hold");
  return res.json();
}
