import "server-only";

import { cookies } from "next/headers";
import { AUTH_COOKIE_NAME } from "./constants";
import { decodeJwtPayload, getJwtRoles } from "./jwt";

export type Session = {
  accessToken: string;
  roles: string[];
  userId?: string;
  username?: string;
  expiresAt?: number;
};

export async function getAccessToken(): Promise<string | null> {
  const store = await cookies();
  return store.get(AUTH_COOKIE_NAME)?.value ?? null;
}

export async function getSession(): Promise<Session | null> {
  const token = await getAccessToken();
  if (!token) return null;
  const payload = decodeJwtPayload(token);
  if (!payload) return null;
  const roles = getJwtRoles(payload);
  return {
    accessToken: token,
    roles,
    userId: payload.sub,
    username: payload.unique_name,
    expiresAt: payload.exp,
  };
}
