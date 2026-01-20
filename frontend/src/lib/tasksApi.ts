import { getToken } from "./auth";

export async function createTask(baseUrl: string, payload: any) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/tasks`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Authorization: `Bearer ${token}` },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to create task");
  return res.json();
}

export async function pauseTask(baseUrl: string, taskId: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/tasks/${encodeURIComponent(taskId)}/pause`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to pause task");
  return res.json();
}

export async function resumeTask(baseUrl: string, taskId: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/tasks/${encodeURIComponent(taskId)}/resume`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to resume task");
  return res.json();
}

export async function cancelTask(baseUrl: string, taskId: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/tasks/${encodeURIComponent(taskId)}/cancel`, {
    method: "POST",
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!res.ok) throw new Error("Failed to cancel task");
  return res.json();
}
