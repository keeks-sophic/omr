import { getToken } from "./auth";

function authHeaders() {
  const token = getToken();
  return { Authorization: `Bearer ${token}` };
}

export async function createMission(baseUrl: string, mission: any) {
  const res = await fetch(`${baseUrl}/api/v1/missions`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(mission),
  });
  if (!res.ok) throw new Error("Failed to create mission");
  return res.json();
}

export async function updateMission(baseUrl: string, missionId: string, mission: any) {
  const res = await fetch(`${baseUrl}/api/v1/missions/${encodeURIComponent(missionId)}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(mission),
  });
  if (!res.ok) throw new Error("Failed to update mission");
  return res.json();
}

export async function validateMission(baseUrl: string, missionId: string, payload: any = {}) {
  const res = await fetch(`${baseUrl}/api/v1/missions/${encodeURIComponent(missionId)}/validate`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to validate mission");
  return res.json();
}

export async function createTeachSession(baseUrl: string, payload: { robotId: string; mapVersionId: string }) {
  const res = await fetch(`${baseUrl}/api/v1/teach/sessions`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to create teach session");
  return res.json();
}

export async function startTeachSession(baseUrl: string, teachSessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/teach/sessions/${encodeURIComponent(teachSessionId)}/start`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to start teach session");
  return res.json();
}

export async function stopTeachSession(baseUrl: string, teachSessionId: string) {
  const res = await fetch(`${baseUrl}/api/v1/teach/sessions/${encodeURIComponent(teachSessionId)}/stop`, {
    method: "POST",
    headers: { ...authHeaders() },
  });
  if (!res.ok) throw new Error("Failed to stop teach session");
  return res.json();
}

export async function captureTeachStep(baseUrl: string, teachSessionId: string, payload: { command: any }) {
  const res = await fetch(`${baseUrl}/api/v1/teach/sessions/${encodeURIComponent(teachSessionId)}/capture-step`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to capture teach step");
  return res.json();
}

export async function saveMissionFromTeach(baseUrl: string, teachSessionId: string, payload: { missionId: string; name: string; version: string; metadata?: any }) {
  const res = await fetch(`${baseUrl}/api/v1/teach/sessions/${encodeURIComponent(teachSessionId)}/save-mission`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to save mission");
  return res.json();
}
