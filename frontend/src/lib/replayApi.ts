import { getToken } from "./auth";

function authHeaders() {
  const token = getToken();
  return { Authorization: `Bearer ${token}` };
}

export async function createReplaySession(baseUrl: string, payload: { robotId: string; fromTime: string; toTime: string; playbackSpeed?: number }) {
  const res = await fetch(`${baseUrl}/api/v1/replay/sessions`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to create replay session");
  return res.json();
}

export async function startReplay(baseUrl: string, replaySessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/replay/sessions/${encodeURIComponent(replaySessionId)}/start`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to start replay");
  return res.json();
}

export async function stopReplay(baseUrl: string, replaySessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/replay/sessions/${encodeURIComponent(replaySessionId)}/stop`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to stop replay");
  return res.json();
}

export async function seekReplay(baseUrl: string, replaySessionId: string, payload: { timestamp: string }) {
  const res = await fetch(`${baseUrl}/api/v1/replay/sessions/${encodeURIComponent(replaySessionId)}/seek`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to seek replay");
  return res.json();
}
