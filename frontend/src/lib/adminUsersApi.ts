import { getTokenAny } from "./auth";
import { getApiBaseUrl } from "./config";

function authHeaders() {
  const token = getTokenAny();
  return { Authorization: `Bearer ${token}`, "Content-Type": "application/json" };
}

export async function listUsers() {
  const baseUrl = getApiBaseUrl();
  const res = await fetch(`${baseUrl}/api/v1/admin/users`, {
    headers: { Authorization: `Bearer ${getTokenAny()}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to list users");
  return res.json();
}

export async function createUser(payload: { username: string; displayName: string; password: string; roles: string[] }) {
  const baseUrl = getApiBaseUrl();
  const res = await fetch(`${baseUrl}/api/v1/admin/users`, {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to create user");
  return res.json();
}

export async function updateUser(userId: string, payload: { displayName: string; password?: string }) {
  const baseUrl = getApiBaseUrl();
  const res = await fetch(`${baseUrl}/api/v1/admin/users/${encodeURIComponent(userId)}`, {
    method: "PUT",
    headers: authHeaders(),
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to update user");
  return res.json();
}

export async function disableUser(userId: string) {
  const baseUrl = getApiBaseUrl();
  const res = await fetch(`${baseUrl}/api/v1/admin/users/${encodeURIComponent(userId)}/disable`, {
    method: "POST",
    headers: { Authorization: `Bearer ${getTokenAny()}` },
  });
  if (!res.ok) throw new Error("Failed to disable user");
  return res.json();
}

export async function assignRoles(userId: string, roles: string[]) {
  const baseUrl = getApiBaseUrl();
  const res = await fetch(`${baseUrl}/api/v1/admin/users/${encodeURIComponent(userId)}/roles`, {
    method: "POST",
    headers: authHeaders(),
    body: JSON.stringify({ roles }),
  });
  if (!res.ok) throw new Error("Failed to assign roles");
  return res.json();
}
