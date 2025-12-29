import { MapData, Point, MapNode } from "@/types";

export function snapToPath(point: Point, map: MapData): { point: Point; pathId: string } | null {
  let nearestPoint: Point | null = null;
  let minDistance = Infinity;
  let nearestPathId = "";

  for (const path of map.paths) {
    const source = map.nodes.find((n) => n.id === path.sourceId);
    const target = map.nodes.find((n) => n.id === path.targetId);

    if (!source || !target) continue;

    const { closest, distance } = getClosestPointOnSegment(point, source, target);

    if (distance < minDistance) {
      minDistance = distance;
      nearestPoint = closest;
      nearestPathId = path.id;
    }
  }

  // Threshold for snapping (e.g., 50 pixels)
  // If the user clicks too far, maybe we shouldn't snap? 
  // For now, always snap to closest to ensure "robot only on path".
  if (nearestPoint) {
    return { point: nearestPoint, pathId: nearestPathId };
  }

  return null;
}

function getClosestPointOnSegment(p: Point, a: Point, b: Point): { closest: Point; distance: number } {
  const atob = { x: b.x - a.x, y: b.y - a.y };
  const atop = { x: p.x - a.x, y: p.y - a.y };
  const len2 = atob.x * atob.x + atob.y * atob.y;
  
  let t = (atop.x * atob.x + atop.y * atob.y) / len2;
  t = Math.max(0, Math.min(1, t));

  const closest = {
    x: a.x + atob.x * t,
    y: a.y + atob.y * t,
  };

  const dx = p.x - closest.x;
  const dy = p.y - closest.y;
  const distance = Math.sqrt(dx * dx + dy * dy);

  return { closest, distance };
}

// Simple Dijkstra
export function findPath(start: Point, end: Point, map: MapData): Point[] {
    // 1. Find nearest nodes to start and end
    const startNode = findNearestNode(start, map.nodes);
    const endNode = findNearestNode(end, map.nodes);

    if (!startNode || !endNode) return [start, end]; // Fallback

    // 2. Build Adjacency Graph
    const graph = new Map<string, { id: string; weight: number }[]>();
    
    map.nodes.forEach(n => graph.set(n.id, []));

    map.paths.forEach(p => {
        // Assume all paths are bidirectional for simplicity in this mock, 
        // OR respect direction. Let's respect direction if possible.
        // The MapPath type has 'direction'.
        
        const weight = p.length;
        
        // Forward
        if (graph.has(p.sourceId)) {
            graph.get(p.sourceId)!.push({ id: p.targetId, weight });
        }

        // Backward (if bidirectional)
        if (p.direction === 'bidirectional' && graph.has(p.targetId)) {
            graph.get(p.targetId)!.push({ id: p.sourceId, weight });
        }
    });

    // 3. Dijkstra
    const distances = new Map<string, number>();
    const previous = new Map<string, string>();
    const pq: { id: string; dist: number }[] = [];

    map.nodes.forEach(n => {
        distances.set(n.id, Infinity);
    });
    distances.set(startNode.id, 0);
    pq.push({ id: startNode.id, dist: 0 });

    while (pq.length > 0) {
        pq.sort((a, b) => a.dist - b.dist);
        const { id: u } = pq.shift()!;

        if (u === endNode.id) break;

        const neighbors = graph.get(u) || [];
        for (const neighbor of neighbors) {
            const alt = distances.get(u)! + neighbor.weight;
            if (alt < distances.get(neighbor.id)!) {
                distances.set(neighbor.id, alt);
                previous.set(neighbor.id, u);
                pq.push({ id: neighbor.id, dist: alt });
            }
        }
    }

    // 4. Reconstruct Path
    const path: Point[] = [];
    let curr: string | undefined = endNode.id;
    
    // If no path found
    if (distances.get(endNode.id) === Infinity) {
        return [start, end]; // Straight line fallback
    }

    while (curr) {
        const node = map.nodes.find(n => n.id === curr);
        if (node) path.unshift({ x: node.x, y: node.y });
        curr = previous.get(curr);
    }

    // Prepend actual start point and append actual end point if they differ significantly from nodes
    // (Optional: for now, just snapping to nodes is cleaner for graph navigation)
    
    // But we snapped start/end to path segments, not necessarily nodes.
    // To do this perfectly, we'd need to insert temporary nodes at start/end projection points.
    // For this mock, moving from start -> startNode -> ... -> endNode -> end is acceptable.
    
    if (getDistance(start, path[0]) > 1) path.unshift(start);
    if (getDistance(end, path[path.length - 1]) > 1) path.push(end);

    return path;
}

function findNearestNode(p: Point, nodes: MapNode[]): MapNode | null {
    let nearest: MapNode | null = null;
    let minDst = Infinity;

    for (const node of nodes) {
        const dst = Math.sqrt(Math.pow(node.x - p.x, 2) + Math.pow(node.y - p.y, 2));
        if (dst < minDst) {
            minDst = dst;
            nearest = node;
        }
    }
    return nearest;
}

function getDistance(a: Point, b: Point) {
    return Math.sqrt(Math.pow(b.x - a.x, 2) + Math.pow(b.y - a.y, 2));
}
