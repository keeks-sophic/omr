export const ViewerPolicy = ["Viewer", "Operator", "Admin"] as const;
export const OperatorPolicy = ["Operator", "Admin"] as const;
export const AdminPolicy = ["Admin"] as const;

export type Role = (typeof ViewerPolicy)[number] | "Pending";

export function hasRole(userRoles: string[], role: string): boolean {
  return userRoles.includes(role);
}

export function hasAnyRole(userRoles: string[], required: readonly string[]): boolean {
  return required.some((r) => userRoles.includes(r));
}

