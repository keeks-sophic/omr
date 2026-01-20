export type JwtPayload = {
  sub?: string;
  unique_name?: string;
  roles?: string[] | string;
  exp?: number;
};

function base64UrlToUtf8(input: string): string {
  const normalized = input.replace(/-/g, "+").replace(/_/g, "/");
  const padLength = (4 - (normalized.length % 4)) % 4;
  const padded = normalized + "=".repeat(padLength);
  return Buffer.from(padded, "base64").toString("utf8");
}

export function decodeJwtPayload(token: string): JwtPayload | null {
  const parts = token.split(".");
  if (parts.length < 2) return null;
  try {
    const json = base64UrlToUtf8(parts[1]);
    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

export function getJwtRoles(payload: JwtPayload | null): string[] {
  if (!payload) return [];
  const roles = payload.roles;
  if (!roles) return [];
  if (Array.isArray(roles)) return roles.filter((r) => typeof r === "string" && r.trim() !== "");
  if (typeof roles === "string" && roles.trim() !== "") return [roles];
  return [];
}

