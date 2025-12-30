export interface Robot {
  name: string;
  ip: string;
  x?: number;
  y?: number;
  state: string;
  battery: number;
  connected: boolean;
  lastActive?: string;
  isMock?: boolean;
  mapId?: number | null;
}

export interface RobotCommand {
  ip: string;
  command: string;
  data?: unknown;
}

export interface Point {
  x: number;
  y: number;
}

export type NodeStatus = "active" | "maintenance";
export type PathStatus = "active" | "maintenance";
export type PathDirection = "bidirectional" | "one-way";
export type PointType = "charging" | "drop" | "rest";

export interface MapNode {
  id: string;
  x: number;
  y: number;
  label?: string;
  status: NodeStatus;
}

export interface MapPath {
  id: string;
  sourceId: string;
  targetId: string;
  length: number;
  direction: PathDirection;
  status: PathStatus;
}

export interface MapPointMarker {
  id: string;
  pathId: string;
  distance: number;
  type: PointType;
  label?: string;
}

export interface MapQR {
  id: string;
  pathId: string;
  distance: number;
  data: string;
}

export interface MapData {
  id: string;
  name: string;
  width?: number;
  height?: number;
  imageUrl?: string;
  nodes: MapNode[];
  paths: MapPath[];
  points: MapPointMarker[];
  qrcodes: MapQR[];
}

export interface Route {
  points: Point[];
  distance: number;
  estimatedTime: number;
}
