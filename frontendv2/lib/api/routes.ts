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
  maps: {
    base: "/api/v1/maps",
    byId: (mapId: string) => `/api/v1/maps/${mapId}`,
    draft: (mapId: string) => `/api/v1/maps/${mapId}/draft`,
    versions: (mapId: string) => `/api/v1/maps/${mapId}/versions`,
    versionById: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}`,
    snapshot: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}/snapshot`,
    clone: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}/clone`,
    publish: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}/publish`,
    nodes: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}/nodes`,
    nodeById: (mapId: string, mapVersionId: string, nodeId: string) =>
      `/api/v1/maps/${mapId}/versions/${mapVersionId}/nodes/${nodeId}`,
    nodeMaintenance: (mapId: string, mapVersionId: string, nodeId: string) =>
      `/api/v1/maps/${mapId}/versions/${mapVersionId}/nodes/${nodeId}/maintenance`,
    paths: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}/paths`,
    pathById: (mapId: string, mapVersionId: string, pathId: string) =>
      `/api/v1/maps/${mapId}/versions/${mapVersionId}/paths/${pathId}`,
    pathMaintenance: (mapId: string, mapVersionId: string, pathId: string) =>
      `/api/v1/maps/${mapId}/versions/${mapVersionId}/paths/${pathId}/maintenance`,
    pathRest: (mapId: string, mapVersionId: string, pathId: string) =>
      `/api/v1/maps/${mapId}/versions/${mapVersionId}/paths/${pathId}/rest`,
    points: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}/points`,
    pointById: (mapId: string, mapVersionId: string, pointId: string) =>
      `/api/v1/maps/${mapId}/versions/${mapVersionId}/points/${pointId}`,
    qrs: (mapId: string, mapVersionId: string) => `/api/v1/maps/${mapId}/versions/${mapVersionId}/qrs`,
    qrById: (mapId: string, mapVersionId: string, qrId: string) =>
      `/api/v1/maps/${mapId}/versions/${mapVersionId}/qrs/${qrId}`,
  },
} as const;
