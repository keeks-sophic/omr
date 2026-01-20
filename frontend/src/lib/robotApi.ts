import { getToken } from "./auth";
import { getRobots as fetchRobots } from "./configApi";

export const getRobots = fetchRobots;

function authHeaders() {
  const token = getToken();
  return { Authorization: `Bearer ${token}` };
}

export async function getRobot(baseUrl: string, robotId: string) {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}`, {
    headers: { ...authHeaders() },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch robot");
  return res.json();
}

export async function getRobotSession(baseUrl: string, robotId: string) {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/session`, {
    headers: { ...authHeaders() },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch robot session");
  return res.json();
}

export async function commandMode(baseUrl: string, robotId: string, mode: "IDLE" | "MANUAL" | "PAUSED") {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/commands/mode`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ mode }),
  });
  if (!res.ok) throw new Error("Failed to set mode");
  return res.json();
}

export async function commandGrip(baseUrl: string, robotId: string, payload: { open: boolean }) {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/commands/grip`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to grip");
  return res.json();
}

export async function commandHoist(baseUrl: string, robotId: string, payload: { height?: number; steps?: number }) {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/commands/hoist`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to hoist");
  return res.json();
}

export async function commandTelescope(baseUrl: string, robotId: string, payload: { extension?: number; steps?: number }) {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/commands/telescope`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to telescope");
  return res.json();
}

export async function commandCamToggle(baseUrl: string, robotId: string, payload: { side: "left" | "right" | "center" }) {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/commands/cam-toggle`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to cam toggle");
  return res.json();
}

export async function commandRotate(baseUrl: string, robotId: string, payload: { degrees: number }) {
  const res = await fetch(`${baseUrl}/api/v1/robots/${encodeURIComponent(robotId)}/commands/rotate`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to rotate");
  return res.json();
}
