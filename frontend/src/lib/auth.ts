type LoginResponse = { accessToken: string };

type MeResponse = {
  userId: string;
  roles: string[];
  allowedRobotIds?: string[];
};

function decodePayload(token: string | null) {
  if (!token) return null;
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    let b64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const pad = b64.length % 4;
    if (pad) b64 += "=".repeat(4 - pad);
    return JSON.parse(atob(b64));
  } catch {
    return null;
  }
}

export function decodeRolesFromToken(token?: string | null) {
  const json = decodePayload(token ?? getTokenFromStorage());
  if (!json) return [];
  const val = json.roles;
  if (Array.isArray(val)) return val as string[];
  if (typeof val === "string" && val.length > 0) return val.split(",").map((s) => s.trim());
  return [];
}

function getTokenFromStorage() {
  if (typeof window === "undefined") return null;
  return localStorage.getItem("auth_token");
}

export async function login(baseUrl: string, username: string, password: string) {
  const res = await fetch(`${baseUrl}/api/v1/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, password }),
  });
  if (!res.ok) throw new Error("Login failed");
  const data = (await res.json()) as LoginResponse;
  return data.accessToken;
}

export async function fetchMe(baseUrl: string, token?: string) {
  const accessToken = token || getTokenAny();
  const res = await fetch(`${baseUrl}/api/v1/auth/me`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    cache: "no-store",
  });
  if (!res.ok) {
    throw new Error("Fetch me failed");
  }
  const data = (await res.json()) as MeResponse;
  const roles = Array.isArray(data.roles) && data.roles.length > 0 ? data.roles : decodeRolesFromToken(accessToken);
  return { ...data, roles };
}

export function setToken(token: string) {
  if (typeof window !== "undefined") {
    localStorage.setItem("auth_token", token);
    document.cookie = `auth_token=${token}; Path=/; SameSite=Lax`;
  }
}

export function getToken() {
  return getTokenFromStorage();
}

function getTokenFromCookie() {
  if (typeof document === "undefined") return null;
  const m = document.cookie.match(/(?:^|;\s*)auth_token=([^;]+)/);
  return m ? decodeURIComponent(m[1]) : null;
}

export function getTokenAny() {
  return getTokenFromStorage() || getTokenFromCookie();
}

export function clearToken() {
  if (typeof window !== "undefined") {
    localStorage.removeItem("auth_token");
    document.cookie = "auth_token=; Path=/; Max-Age=0";
  }
}

export async function registerUser(baseUrl: string, username: string, displayName: string, password: string) {
  const res = await fetch(`${baseUrl}/api/v1/auth/register`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username, displayName, password }),
  });
  if (!res.ok) {
    let msg = "Registration failed";
    try {
      const data = await res.json();
      msg = data?.error || msg;
    } catch {}
    throw new Error(msg);
  }
  return res.json();
}
