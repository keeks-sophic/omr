export type UserDto = {
  userId: string;
  username: string;
  displayName: string;
  roles: string[];
  isDisabled: boolean;
};

export type AdminCreateUserRequest = {
  username: string;
  displayName: string;
  password: string;
  roles: string[];
};

export type AdminUpdateUserRequest = {
  displayName: string;
  password?: string | null;
  isDisabled?: boolean | null;
};

export type AdminAssignRolesRequest = {
  roles: string[];
};

export type LoginRequest = {
  username: string;
  password: string;
};

export type LoginResponse = {
  accessToken: string;
  expiresAt: string;
  user: UserDto;
};

export type RegisterRequest = {
  username: string;
  displayName: string;
  password: string;
};

export type RegisterResponse = {
  userId: string;
};

export type MeResponse = {
  userId: string;
  username: string;
  roles: string[];
};

export type MapDto = {
  mapId: string;
  name: string;
  createdAt: string;
  archivedAt?: string | null;
  activePublishedMapVersionId?: string | null;
  updatedAt: string;
};

export type MapVersionDto = {
  mapVersionId: string;
  mapId: string;
  version: number;
  status: string;
  createdAt: string;
  publishedAt?: string | null;
  publishedBy?: string | null;
  changeSummary?: string | null;
  derivedFromMapVersionId?: string | null;
  label?: string | null;
};

export type GeomDto = {
  x: number;
  y: number;
};

export type NodeDto = {
  nodeId: string;
  mapVersionId: string;
  label: string;
  geom: GeomDto;
  isMaintenance: boolean;
  junctionSpeedLimit?: number | null;
};

export type PathDto = {
  pathId: string;
  mapVersionId: string;
  fromNodeId: string;
  toNodeId: string;
  direction: "ONE_WAY" | "TWO_WAY" | string;
  speedLimit?: number | null;
  isMaintenance: boolean;
  isRestPath: boolean;
  restCapacity?: number | null;
  restDwellPolicy?: string | null;
  points: GeomDto[];
};

export type MapPointDto = {
  pointId: string;
  mapVersionId: string;
  type: string;
  label: string;
  geom: GeomDto;
  attachedNodeId?: string | null;
};

export type QrDto = {
  qrId: string;
  mapVersionId: string;
  pathId: string;
  qrCode: string;
  distanceAlongPath: number;
};

export type MapSnapshotDto = {
  version: MapVersionDto;
  nodes: NodeDto[];
  paths: PathDto[];
  points: MapPointDto[];
  qrs: QrDto[];
};

export type CreateMapRequest = {
  name: string;
};

export type PublishMapRequest = {
  changeSummary?: string | null;
};

export type CloneMapRequest = {
  label?: string | null;
};

export type CreateNodeRequest = {
  nodeId?: string | null;
  geom: GeomDto;
  label: string;
  junctionSpeedLimit?: number | null;
};

export type UpdateNodeRequest = {
  geom: GeomDto;
  label: string;
  junctionSpeedLimit?: number | null;
};

export type SetMaintenanceRequest = {
  isMaintenance: boolean;
};

export type CreatePathRequest = {
  pathId?: string | null;
  fromNodeId: string;
  toNodeId: string;
  direction: string;
  speedLimit?: number | null;
};

export type UpdatePathRequest = {
  direction: string;
  speedLimit?: number | null;
};

export type SetRestOptionsRequest = {
  isRestPath: boolean;
  restCapacity?: number | null;
  restDwellPolicy?: string | null;
};

export type CreatePointRequest = {
  pointId?: string | null;
  type: string;
  label: string;
  geom: GeomDto;
  attachedNodeId?: string | null;
};

export type UpdatePointRequest = {
  type: string;
  label: string;
  geom: GeomDto;
  attachedNodeId?: string | null;
};

export type CreateQrRequest = {
  qrId?: string | null;
  pathId: string;
  distanceAlongPath: number;
  qrCode: string;
};

export type UpdateQrRequest = {
  pathId: string;
  distanceAlongPath: number;
  qrCode: string;
};
