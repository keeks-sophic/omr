import { AdminPolicy, OperatorPolicy, ViewerPolicy, hasAnyRole } from "./roles";

export function canRead(roles: string[]): boolean {
  return hasAnyRole(roles, ViewerPolicy);
}

export function canWrite(roles: string[]): boolean {
  return hasAnyRole(roles, OperatorPolicy);
}

export function isAdmin(roles: string[]): boolean {
  return hasAnyRole(roles, AdminPolicy);
}

