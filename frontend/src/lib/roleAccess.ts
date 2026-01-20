export type Role = "Admin" | "Planner" | "Operator" | "Viewer";

export const publicPaths = ["/login", "/logout", "/register", "/favicon.ico", "/_next", "/public"];

export type AccessRule = { prefix: string; roles: Role[] };

export const accessRules: AccessRule[] = [
  { prefix: "/users", roles: ["Admin"] },
  { prefix: "/ops", roles: ["Admin"] },
  { prefix: "/config", roles: ["Admin"] },

  { prefix: "/control", roles: ["Operator", "Admin"] },
  { prefix: "/tasks", roles: ["Operator", "Admin"] },
  { prefix: "/replay", roles: ["Operator", "Admin"] },

  { prefix: "/mission", roles: ["Planner", "Admin"] },
  { prefix: "/sim", roles: ["Planner", "Admin"] },
  { prefix: "/simulation", roles: ["Planner", "Admin"] },

  { prefix: "/fleet", roles: ["Viewer", "Operator", "Planner", "Admin"] },
  { prefix: "/robot", roles: ["Viewer", "Operator", "Planner", "Admin"] },
  { prefix: "/map", roles: ["Viewer", "Operator", "Planner", "Admin"] },
  { prefix: "/traffic", roles: ["Viewer", "Operator", "Planner", "Admin"] },
  { prefix: "/visualise", roles: ["Viewer", "Operator", "Planner", "Admin"] },
  { prefix: "/profile", roles: ["Viewer", "Operator", "Planner", "Admin"] },
  { prefix: "/", roles: ["Viewer", "Operator", "Planner", "Admin"] }
];

export function isPublicPath(pathname: string) {
  return publicPaths.some((p) => pathname === p || pathname.startsWith(p));
}

export function isRouteAllowed(pathname: string, roles: string[]) {
  const rule = accessRules.find((r) => pathname === r.prefix || pathname.startsWith(r.prefix + "/"));
  const allowedRoles = rule ? rule.roles : ["Viewer", "Operator", "Planner", "Admin"];
  return roles.some((role) => allowedRoles.includes(role as Role));
}
