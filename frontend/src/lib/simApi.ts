import { getToken } from "./auth";

function authHeaders() {
  const token = getToken();
  return { Authorization: `Bearer ${token}` };
}

export async function createSimSession(baseUrl: string, payload: { mapVersionId: string; robotCount: number; capabilities?: any; featureFlags?: any; missions?: string[]; tasks?: any[]; speedMultiplier?: number }) {
  const res = await fetch(`${baseUrl}/api/v1/sim/sessions`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to create simulation session");
  return res.json();
}

export async function startSimSession(baseUrl: string, simSessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/sim/sessions/${encodeURIComponent(simSessionId)}/start`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to start simulation");
  return res.json();
}

export async function stopSimSession(baseUrl: string, simSessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/sim/sessions/${encodeURIComponent(simSessionId)}/stop`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to stop simulation");
  return res.json();
}

export async function pauseSimSession(baseUrl: string, simSessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/sim/sessions/${encodeURIComponent(simSessionId)}/pause`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to pause simulation");
  return res.json();
}

export async function resumeSimSession(baseUrl: string, simSessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/sim/sessions/${encodeURIComponent(simSessionId)}/resume`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to resume simulation");
  return res.json();
}
