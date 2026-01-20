export const ApiRoutes = {
  auth: {
    login: "/api/v1/auth/login",
    register: "/api/v1/auth/register",
    me: "/api/v1/auth/me",
    logout: "/api/v1/auth/logout",
  },
  adminUsers: {
    base: "/api/v1/admin/users",
    byId: (userId: string) => `/api/v1/admin/users/${userId}`,
    disable: (userId: string) => `/api/v1/admin/users/${userId}/disable`,
    roles: (userId: string) => `/api/v1/admin/users/${userId}/roles`,
  },
} as const;

