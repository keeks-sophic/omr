import { getToken } from "./auth";

function authHeaders() {
  const token = getToken();
  return { Authorization: `Bearer ${token}` };
}

export async function createMap(baseUrl: string, payload: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to create map version");
  return res.json();
}

export async function cloneMap(baseUrl: string, mapVersionId: string, payload: any = {}) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/clone`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to clone map version");
  return res.json();
}

export async function publishMap(baseUrl: string, mapVersionId: string, payload: any = {}) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/publish`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to publish map version");
  return res.json();
}

export async function createNode(baseUrl: string, mapVersionId: string, node: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/nodes`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(node),
  });
  if (!res.ok) throw new Error("Failed to create node");
  return res.json();
}

export async function updateNode(baseUrl: string, mapVersionId: string, nodeId: string, node: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/nodes/${encodeURIComponent(nodeId)}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(node),
  });
  if (!res.ok) throw new Error("Failed to update node");
  return res.json();
}

export async function setNodeMaintenance(baseUrl: string, mapVersionId: string, nodeId: string, isMaintenance: boolean) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/nodes/${encodeURIComponent(nodeId)}/maintenance`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ isMaintenance }),
  });
  if (!res.ok) throw new Error("Failed to set node maintenance");
  return res.json();
}

export async function createPath(baseUrl: string, mapVersionId: string, path: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/paths`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(path),
  });
  if (!res.ok) throw new Error("Failed to create path");
  return res.json();
}

export async function updatePath(baseUrl: string, mapVersionId: string, pathId: string, path: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/paths/${encodeURIComponent(pathId)}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(path),
  });
  if (!res.ok) throw new Error("Failed to update path");
  return res.json();
}

export async function setPathMaintenance(baseUrl: string, mapVersionId: string, pathId: string, isMaintenance: boolean) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/paths/${encodeURIComponent(pathId)}/maintenance`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify({ isMaintenance }),
  });
  if (!res.ok) throw new Error("Failed to set path maintenance");
  return res.json();
}

export async function setPathRest(baseUrl: string, mapVersionId: string, pathId: string, restOptions: { isRestPath: boolean; restCapacity?: number; restDwellPolicy?: string }) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/paths/${encodeURIComponent(pathId)}/rest`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(restOptions),
  });
  if (!res.ok) throw new Error("Failed to set path rest options");
  return res.json();
}

export async function createPoint(baseUrl: string, mapVersionId: string, point: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/points`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(point),
  });
  if (!res.ok) throw new Error("Failed to create point");
  return res.json();
}

export async function updatePoint(baseUrl: string, mapVersionId: string, pointId: string, point: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/points/${encodeURIComponent(pointId)}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(point),
  });
  if (!res.ok) throw new Error("Failed to update point");
  return res.json();
}

export async function createQr(baseUrl: string, mapVersionId: string, qr: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/qrs`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(qr),
  });
  if (!res.ok) throw new Error("Failed to create QR anchor");
  return res.json();
}

export async function updateQr(baseUrl: string, mapVersionId: string, qrId: string, qr: any) {
  const res = await fetch(`${baseUrl}/api/v1/maps/${encodeURIComponent(mapVersionId)}/qrs/${encodeURIComponent(qrId)}`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(qr),
  });
  if (!res.ok) throw new Error("Failed to update QR anchor");
  return res.json();
}
