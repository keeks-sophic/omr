import type { GeomDto, NodeDto, PathDto } from "@/lib/api/types";

export type ViewTransform = { x: number; y: number; k: number };

export function clamp(n: number, min: number, max: number): number {
  return Math.max(min, Math.min(max, n));
}

export function distance(a: GeomDto, b: GeomDto): number {
  const dx = a.x - b.x;
  const dy = a.y - b.y;
  return Math.hypot(dx, dy);
}

export function getPathPolyline(path: PathDto, nodes: NodeDto[]): GeomDto[] {
  if (path.points && path.points.length >= 2) return path.points;
  const from = nodes.find((n) => n.nodeId === path.fromNodeId)?.geom;
  const to = nodes.find((n) => n.nodeId === path.toNodeId)?.geom;
  if (!from || !to) return [];
  return [from, to];
}

export function polylineLength(points: GeomDto[]): number {
  let total = 0;
  for (let i = 1; i < points.length; i++) {
    total += distance(points[i - 1], points[i]);
  }
  return total;
}

export function positionAlongPolyline(points: GeomDto[], dist: number): GeomDto | null {
  if (points.length === 0) return null;
  if (points.length === 1) return points[0];

  let remaining = Math.max(0, dist);
  for (let i = 1; i < points.length; i++) {
    const a = points[i - 1];
    const b = points[i];
    const seg = distance(a, b);
    if (seg <= 0) continue;
    if (remaining <= seg) {
      const t = remaining / seg;
      return { x: a.x + (b.x - a.x) * t, y: a.y + (b.y - a.y) * t };
    }
    remaining -= seg;
  }
  return points[points.length - 1];
}

export function projectPointToSegment(p: GeomDto, a: GeomDto, b: GeomDto): { t: number; closest: GeomDto; dist2: number } {
  const abx = b.x - a.x;
  const aby = b.y - a.y;
  const apx = p.x - a.x;
  const apy = p.y - a.y;
  const ab2 = abx * abx + aby * aby;
  const t = ab2 > 0 ? clamp((apx * abx + apy * aby) / ab2, 0, 1) : 0;
  const closest = { x: a.x + abx * t, y: a.y + aby * t };
  const dx = p.x - closest.x;
  const dy = p.y - closest.y;
  return { t, closest, dist2: dx * dx + dy * dy };
}

export function projectPointToPolyline(p: GeomDto, points: GeomDto[]): { closest: GeomDto; distanceAlong: number; dist2: number } | null {
  if (points.length < 2) return null;
  let bestDist2 = Number.POSITIVE_INFINITY;
  let bestClosest: GeomDto | null = null;
  let bestDistanceAlong = 0;

  let accumulated = 0;
  for (let i = 1; i < points.length; i++) {
    const a = points[i - 1];
    const b = points[i];
    const segLen = distance(a, b);
    const proj = projectPointToSegment(p, a, b);
    if (proj.dist2 < bestDist2) {
      bestDist2 = proj.dist2;
      bestClosest = proj.closest;
      bestDistanceAlong = accumulated + segLen * proj.t;
    }
    accumulated += segLen;
  }

  if (!bestClosest) return null;
  return { closest: bestClosest, distanceAlong: bestDistanceAlong, dist2: bestDist2 };
}

