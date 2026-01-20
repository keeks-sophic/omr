import { getToken } from "./auth";

function authHeaders() {
  const token = getToken();
  return { Authorization: `Bearer ${token}` };
}

export async function planRoute(baseUrl: string, payload: any) {
  const res = await fetch(`${baseUrl}/api/v1/routes/plan`, {
    method: "POST",
    headers: { "Content-Type": "application/json", ...authHeaders() },
    body: JSON.stringify(payload),
  });
  if (!res.ok) throw new Error("Failed to plan route");
  return res.json();
}
