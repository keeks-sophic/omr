"use client";

import React, { useState, useRef, useEffect, useMemo } from "react";
import {
  Maximize,
  ZoomIn,
  ZoomOut,
  Plus,
  Bot,
  Move,
  MousePointer2,
  Navigation,
  Layers,
  Trash2,
  FolderOpen,
  Coffee,
  Zap,
  ArrowDownToLine
} from "lucide-react";
import { useRobotFleet } from "../../hooks/useRobotFleet";

// --- Types ---

interface Position {
  x: number;
  y: number;
}

interface RobotEntity {
  id: string;
  name: string;
  ip?: string;
  x: number; // Meters
  y: number; // Meters
  color: string;
  status: "idle" | "moving" | "error";
  state?: string;
  mapId?: number | null;
}

interface MapNode {
  id: string;
  x: number; // Meters
  y: number; // Meters
}

interface MapPath {
  id: string;
  sourceId: string;
  targetId: string;
  status?: "active" | "maintenance";
  rest?: boolean;
  direction?: "bidirectional" | "one-way";
  points?: Position[]; // Intermediate points (including start/end or just intermediate? Usually backend sends full geometry or intermediate)
}

interface MapPoint {
  id: string;
  pathId: string;
  distance: number; // meters from source
  type: "charging" | "drop" | "rest";
}
// --- Constants ---

const PIXELS_PER_METER = 20; // 20px = 1m
const GRID_SIZE = 50; // Visual grid size in pixels
const DOT_SIZE = 1;
const ROBOT_SIZE_PX = 24;
const API_BASE = "http://localhost:5067";

export default function VisualisePage() {
  // --- State ---
  const [view, setView] = useState({ x: 0, y: 0, k: 1 });
  const [isPanning, setIsPanning] = useState(false);
  const [lastMousePos, setLastMousePos] = useState<Position>({ x: 0, y: 0 });
  
  const [mockRobots, setMockRobots] = useState<RobotEntity[]>([]);
  const [selectedRobotId, setSelectedRobotId] = useState<string | null>(null);
  const [draggingRobotId, setDraggingRobotId] = useState<string | null>(null);
  const [colorMap, setColorMap] = useState<Record<string, string>>({});
  const [relocateRobotId, setRelocateRobotId] = useState<string | null>(null);
  const [fleetOverrides, setFleetOverrides] = useState<Record<string, { x: number; y: number }>>({});
  const [pendingRelocates, setPendingRelocates] = useState<Record<string, { x: number; y: number }>>({});
  const [navigateRobotId, setNavigateRobotId] = useState<string | null>(null);
  const [hoveredMapPos, setHoveredMapPos] = useState<Position | null>(null);

  const [maps, setMaps] = useState<{ id: number; name: string }[]>([]);
  const [mapNodes, setMapNodes] = useState<MapNode[]>([]);
  const [mapPaths, setMapPaths] = useState<MapPath[]>([]);
  const [mapPoints, setMapPoints] = useState<MapPoint[]>([]);
  const [currentMapId, setCurrentMapId] = useState<number | null>(null);
  const [selectedUnassignedIp, setSelectedUnassignedIp] = useState<string | null>(null);
  const [unassigned, setUnassigned] = useState<{ name: string; ip: string }[]>([]);
  const prevMapIdRef = useRef<number | null>(null);
  const [recentlyAssigned, setRecentlyAssigned] = useState<Record<string, number>>({});

  const { robots: fleetRobots, joinMap, leaveMap, routes } = useRobotFleet();

  const canvasRef = useRef<HTMLDivElement>(null);

  // --- Helpers ---

  // Returns World Coordinates in PIXELS
  const screenToWorldPx = (clientX: number, clientY: number) => {
    if (!canvasRef.current) return { x: 0, y: 0 };
    const rect = canvasRef.current.getBoundingClientRect();
    const x = (clientX - rect.left - view.x) / view.k;
    const y = (clientY - rect.top - view.y) / view.k;
    return { x, y };
  };

  const generateId = () => Math.random().toString(36).substr(2, 9);

  const getRandomColor = () => {
    const colors = ["#8b5cf6", "#38bdf8", "#10b981", "#f59e0b", "#f43f5e"];
    return colors[Math.floor(Math.random() * colors.length)];
  };

  const getRobotColor = (key: string) => {
    if (!colorMap[key]) {
      const c = getRandomColor();
      setColorMap(prev => ({ ...prev, [key]: c }));
      return c;
    }
    return colorMap[key];
  };

  const getPointPosition = (pathId: string, distanceMeters: number) => {
      const path = mapPaths.find(p => p.id === pathId);
      if (!path) return null;

      if (path.points && path.points.length > 1) {
          let remaining = distanceMeters;
          for (let i = 0; i < path.points.length - 1; i++) {
              const p1 = path.points[i];
              const p2 = path.points[i + 1];
              const dist = Math.sqrt(Math.pow(p2.x - p1.x, 2) + Math.pow(p2.y - p1.y, 2));
              
              if (remaining <= dist) {
                  const ratio = remaining / dist;
                  return {
                      x: p1.x + (p2.x - p1.x) * ratio,
                      y: p1.y + (p2.y - p1.y) * ratio
                  };
              }
              remaining -= dist;
          }
          return path.points[path.points.length - 1];
      }

      const source = mapNodes.find(n => n.id === path.sourceId);
      const target = mapNodes.find(n => n.id === path.targetId);
      if (!source || !target) return null;

      const pathLen = Math.sqrt(Math.pow(target.x - source.x, 2) + Math.pow(target.y - source.y, 2));
      const ratio = distanceMeters / pathLen;
      
      return {
          x: source.x + (target.x - source.x) * ratio,
          y: source.y + (target.y - source.y) * ratio
      };
  };

  // --- Math Helpers for Snapping ---

  const getDistance = (p1: Position, p2: Position) => {
    return Math.sqrt(Math.pow(p2.x - p1.x, 2) + Math.pow(p2.y - p1.y, 2));
  };

  // Project point p onto segment ab. Returns the closest point on segment.
  const getClosestPointOnSegment = (p: Position, a: Position, b: Position) => {
    const atob = { x: b.x - a.x, y: b.y - a.y };
    const atop = { x: p.x - a.x, y: p.y - a.y };
    const len2 = atob.x * atob.x + atob.y * atob.y;
    
    let dot = atop.x * atob.x + atop.y * atob.y;
    let t = Math.min(1, Math.max(0, dot / len2));
    
    return {
      x: a.x + atob.x * t,
      y: a.y + atob.y * t
    };
  };

  const getClosestPointOnMap = (p: Position): Position | null => {
    let closestPoint: Position | null = null;
    let minDist = Infinity;
    const SNAP_RADIUS_METERS = navigateRobotId ? 2.0 : 1.0;

    // 1. Check Points (POIs)
    for (const pt of mapPoints) {
        const pos = getPointPosition(pt.pathId, pt.distance);
        if (pos) {
            const dist = getDistance(p, pos);
            if (dist < SNAP_RADIUS_METERS && dist < minDist) {
                minDist = dist;
                closestPoint = pos;
            }
        }
    }
    if (closestPoint) return closestPoint;

    // Iterate all paths
    for (const path of mapPaths) {
      if (path.points && path.points.length > 1) {
          for (let i = 0; i < path.points.length - 1; i++) {
              const p1 = path.points[i];
              const p2 = path.points[i + 1];
              const proj = getClosestPointOnSegment(p, p1, p2);
              const dist = getDistance(p, proj);
              if (dist < minDist) {
                  minDist = dist;
                  closestPoint = proj;
              }
          }
      } else {
          const source = mapNodes.find(n => n.id === path.sourceId);
          const target = mapNodes.find(n => n.id === path.targetId);
          
          if (source && target) {
            // Source and Target are in Meters
            const proj = getClosestPointOnSegment(p, source, target);
            const dist = getDistance(p, proj);
            
            if (dist < minDist) {
              minDist = dist;
              closestPoint = proj;
            }
          }
      }
    }

    return closestPoint;
  };

  // --- Data Loading ---
  useEffect(() => {
    const loadMaps = async () => {
      try {
        const res = await fetch(`${API_BASE}/maps`, { credentials: "include" });
        if (!res.ok) return;
        const data: { id: number; name: string }[] = await res.json();
        setMaps(data);
        if (data.length > 0 && currentMapId === null) {
          setCurrentMapId(data[0].id);
        }
      } catch {}
    };
    loadMaps();
    const loadUnassigned = async () => {
      try {
        const res = await fetch(`${API_BASE}/robots/unassigned`, { credentials: "include" });
        if (!res.ok) return;
        const data = await res.json();
        setUnassigned(data.map((r: any) => ({ name: r.name, ip: r.ip })));
      } catch {}
    };
    loadUnassigned();
  }, []);

  useEffect(() => {
    const idNum = currentMapId;
    if (!idNum || !Number.isFinite(idNum) || idNum <= 0) return;
    const loadGraph = async () => {
      try {
        const res = await fetch(`${API_BASE}/maps/${idNum}/graph`, { credentials: "include" });
        if (!res.ok) return;
        const graph = await res.json();
        const nodes: MapNode[] = (graph.nodes as any[]).map((n: any, i: number) => ({
          id: String(n.id ?? i + 1),
          x: n.x,
          y: -n.y
        }));
        const paths: MapPath[] = (graph.paths as any[]).map((p: any, i: number) => ({
          id: String(p.id ?? i + 1),
          sourceId: String(p.startNodeId),
          targetId: String(p.endNodeId),
          status: (p.status as "active" | "maintenance") ?? "active",
          rest: Boolean(p.rest ?? p.Rest ?? false),
          direction: p.twoWay ? "bidirectional" : "one-way",
          points: p.points ? p.points.map((pt: any) => ({ x: pt.x, y: -pt.y })) : undefined
        }));
        const points: MapPoint[] = (graph.points as any[]).map((pt: any, i: number) => ({
          id: String(pt.id ?? i + 1),
          pathId: String(pt.pathId ?? 0),
          distance: pt.offset,
          type: (pt.type as MapPoint["type"]) ?? "rest"
        }));
        setMapNodes(nodes);
        setMapPaths(paths);
        setMapPoints(points);
        const origin = nodes[0];
        if (origin) {
          setView(v => ({ ...v, x: 200 - origin.x * PIXELS_PER_METER, y: 200 - origin.y * PIXELS_PER_METER }));
        } else {
          setView(v => ({ ...v, x: 200, y: 200 }));
        }
      } catch {}
    };
    loadGraph();
    if (prevMapIdRef.current && prevMapIdRef.current !== idNum) {
      leaveMap(prevMapIdRef.current);
    }
    if (idNum) {
      joinMap(idNum);
      prevMapIdRef.current = idNum;
    }
  }, [currentMapId]);

  const displayRobots = useMemo(() => {
    const fromFleet = fleetRobots.map(r => {
      const key = r.name || r.ip;
      const color = getRobotColor(key);
      const status: "idle" | "moving" | "error" =
        !r.connected ? "error" :
        (typeof r.state === "string" && r.state.toLowerCase().includes("move")) ? "moving" :
        "idle";
      const ov = fleetOverrides[key];
      return {
        id: key,
        name: r.name ?? r.ip,
        ip: r.ip,
        x: ov ? ov.x : (r.x ?? 0),
        y: ov ? ov.y : (r.y ?? 0),
        color,
        status,
        state: typeof r.state === "string" ? r.state : undefined,
        mapId: r.mapId ?? null
      } as RobotEntity;
    });
    const currentId = currentMapId;
    const filteredFleet = currentId ? fromFleet.filter(r => r.mapId === currentId) : fromFleet.filter(r => r.mapId != null);
    const filteredMocks = currentId ? mockRobots.filter(r => r.mapId === currentId) : mockRobots.filter(r => r.mapId != null);
    const nowTs = Date.now();
    const fleetFilteredFinal = currentId 
      ? fromFleet.filter(r => r.mapId === currentId || (r.ip && recentlyAssigned[r.ip] && (nowTs - recentlyAssigned[r.ip]) < 3000))
      : fromFleet.filter(r => r.mapId != null);
    return [...fleetFilteredFinal, ...filteredMocks.map(m => ({ ...m, y: -m.y }))];
  }, [fleetRobots, mockRobots, colorMap, currentMapId, fleetOverrides]);

  // --- Handlers ---

  const handleAddRobot = () => {
    // Start at Node 1 or first available path start
    const startNode = mapNodes[0];
    
    const newRobot: RobotEntity = {
      id: generateId(),
      name: `R-${mockRobots.length + 1}`,
      x: startNode ? startNode.x : 0,
      y: startNode ? startNode.y : 0,
      color: getRandomColor(),
      status: "idle",
      mapId: currentMapId
    };
    setMockRobots([...mockRobots, newRobot]);
    setSelectedRobotId(newRobot.id);
  };

  const handleDeleteRobot = (robotId: string) => {
      setMockRobots(prev => prev.filter(r => r.id !== robotId));
      if (selectedRobotId === robotId) {
          setSelectedRobotId(null);
      }
  };

  const handleWheel = (e: React.WheelEvent) => {
    e.preventDefault();
    if (e.ctrlKey || e.metaKey) {
      // Zoom
      const zoomIntensity = 0.001;
      const minZoom = 0.1;
      const maxZoom = 5;
      const newK = Math.min(Math.max(view.k - e.deltaY * zoomIntensity * view.k, minZoom), maxZoom);

      const rect = canvasRef.current!.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const mouseY = e.clientY - rect.top;

      const newX = mouseX - (mouseX - view.x) * (newK / view.k);
      const newY = mouseY - (mouseY - view.y) * (newK / view.k);

      setView({ x: newX, y: newY, k: newK });
    } else {
      // Pan
      setView((v) => ({ ...v, x: v.x - e.deltaX, y: v.y - e.deltaY }));
    }
  };

  const handleMouseDown = (e: React.MouseEvent) => {
    // Check if clicked on a robot
    const worldPosPx = screenToWorldPx(e.clientX, e.clientY);
    if (navigateRobotId) {
      const mouseMeters = { 
        x: worldPosPx.x / PIXELS_PER_METER, 
        y: worldPosPx.y / PIXELS_PER_METER 
      };
      const snappedPos = getClosestPointOnMap(mouseMeters);
      if (snappedPos && currentMapId) {
        const robot = displayRobots.find(r => r.id === navigateRobotId);
        if (robot?.ip) {
          const worldX = snappedPos.x;
          const worldY = -snappedPos.y;
          fetch(`${API_BASE}/robots/${robot.ip}/navigate`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify({ mapId: currentMapId, x: worldX, y: worldY })
          }).catch(() => {});
        }
      }
      setNavigateRobotId(null);
      return;
    }
    
    // Simple hit detection for robots
    for (let i = displayRobots.length - 1; i >= 0; i--) {
      const robot = displayRobots[i];
      // Convert robot position to pixels for hit testing
      const rPx = { x: robot.x * PIXELS_PER_METER, y: (-robot.y) * PIXELS_PER_METER };
      const dist = Math.sqrt(Math.pow(worldPosPx.x - rPx.x, 2) + Math.pow(worldPosPx.y - rPx.y, 2));
      
      if (dist < ROBOT_SIZE_PX / 2 + 10) { 
        setSelectedRobotId(robot.id);
        if (relocateRobotId === robot.id) setDraggingRobotId(robot.id);
        e.stopPropagation();
        return;
      }
    }

    if (e.button === 0) { // Left click
        setSelectedRobotId(null);
        setNavigateRobotId(null);
    }
    
    if (e.button === 1 || e.button === 0) {
      setIsPanning(true);
      setLastMousePos({ x: e.clientX, y: e.clientY });
    }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
    const worldPosPx = screenToWorldPx(e.clientX, e.clientY);
    
    // Update hovered position for navigation preview
    if (navigateRobotId) {
       const mouseMeters = { 
        x: worldPosPx.x / PIXELS_PER_METER, 
        y: worldPosPx.y / PIXELS_PER_METER 
      };
      const snapped = getClosestPointOnMap(mouseMeters);
      setHoveredMapPos(snapped);
    } else {
      if (hoveredMapPos) setHoveredMapPos(null);
    }

    if (draggingRobotId) {
      // Convert current mouse position to meters
      const mouseMeters = { 
        x: worldPosPx.x / PIXELS_PER_METER, 
        y: worldPosPx.y / PIXELS_PER_METER 
      };

      // Find closest point on any path
      const snappedPos = getClosestPointOnMap(mouseMeters);

      if (snappedPos) {
        const isMock = mockRobots.some(m => m.id === draggingRobotId);
        if (isMock) {
          setMockRobots((prev) =>
            prev.map((r) =>
              r.id === draggingRobotId ? { ...r, x: snappedPos.x, y: -snappedPos.y } : r
            ));
        } else {
          setFleetOverrides(prev => ({ ...prev, [draggingRobotId]: { x: snappedPos.x, y: -snappedPos.y } }));
        }
      }
      return;
    }

    if (isPanning) {
      const dx = e.clientX - lastMousePos.x;
      const dy = e.clientY - lastMousePos.y;
      setView((v) => ({ ...v, x: v.x + dx, y: v.y + dy }));
      setLastMousePos({ x: e.clientX, y: e.clientY });
    }
  };

  const handleMouseUp = () => {
    setIsPanning(false);
    if (draggingRobotId) {
      const isMock = mockRobots.some(m => m.id === draggingRobotId);
      if (!isMock) {
        const robot = displayRobots.find(r => r.id === draggingRobotId);
        const ov = robot ? fleetOverrides[draggingRobotId] : undefined;
        if (robot && ov && robot.ip) {
          const x = ov.x;
          const y = ov.y;
          console.log(`[Mock Backend] Robot ${robot.ip} relocate to:`, { x, y, mapId: currentMapId });
          fetch(`${API_BASE}/robots/${robot.ip}/relocate`, {
            method: "PUT",
            headers: { "Content-Type": "application/json" },
            credentials: "include",
            body: JSON.stringify({ x, y })
          }).catch(() => {});
          setPendingRelocates(prev => ({ ...prev, [draggingRobotId]: { x, y } }));
        }
        setRelocateRobotId(null);
      }
    }
    setDraggingRobotId(null);
  };

  useEffect(() => {
    if (!Object.keys(pendingRelocates).length) return;
    const epsilon = 0.0001;
    const toClear: string[] = [];
    for (const key of Object.keys(pendingRelocates)) {
      const fleet = fleetRobots.find(r => (r.name || r.ip) === key || r.ip === key);
      const pending = pendingRelocates[key];
      if (fleet && Math.abs((fleet.x ?? 0) - pending.x) < epsilon && Math.abs((fleet.y ?? 0) - pending.y) < epsilon) {
        toClear.push(key);
      }
    }
    if (toClear.length > 0) {
      setPendingRelocates(prev => {
        const next = { ...prev };
        for (const k of toClear) delete next[k];
        return next;
      });
      setFleetOverrides(prev => {
        const next = { ...prev };
        for (const k of toClear) delete next[k];
        return next;
      });
    }
  }, [fleetRobots, pendingRelocates]);
  // --- Render ---

  return (
    <div className="flex flex-col h-[calc(100vh-120px)] gap-4 p-4 font-sans text-zinc-200">
      
      {/* Header / Toolbar */}
      <div className="flex items-center justify-between bg-zinc-900/80 backdrop-blur-md border border-zinc-800 p-4 rounded-3xl shadow-xl">
        <div className="flex items-center gap-4">
          <div className="p-2 bg-violet-500/10 rounded-xl border border-violet-500/20 text-violet-400">
            <Layers size={24} />
          </div>
          <div>
            <h1 className="text-lg font-semibold tracking-tight text-white">Fleet Visualiser</h1>
            <p className="text-xs text-zinc-500">Real-time robot telemetry & control</p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <div className="h-8 w-px bg-zinc-800 mx-2" />
          
          <div className="flex items-center gap-2 px-2">
            <FolderOpen size={18} className="text-zinc-400" />
            <select 
              value={currentMapId ?? ""}
              onChange={(e) => setCurrentMapId(Number(e.target.value))}
              className="bg-transparent border-none text-sm font-medium focus:ring-0 text-zinc-200 cursor-pointer"
            >
              {maps.map(m => (
                <option key={m.id} value={m.id} className="bg-zinc-900">{m.name}</option>
              ))}
            </select>
          </div>

          <div className="h-8 w-px bg-zinc-800 mx-2" />

          <div className="flex items-center gap-2">
            <select
              value={selectedUnassignedIp ?? ""}
              onChange={(e) => setSelectedUnassignedIp(e.target.value || null)}
              className="bg-zinc-900/50 border border-zinc-800 text-xs text-zinc-200 rounded-lg px-2 py-1"
            >
              <option value="" className="bg-zinc-900">Select robot</option>
              {unassigned.map(r => (
                <option key={r.ip} value={r.ip} className="bg-zinc-900">
                  {(r.name || r.ip) + ` (${r.ip})`}
                </option>
              ))}
            </select>
            <button
              onClick={async () => {
                if (!selectedUnassignedIp || !currentMapId) return;
                try {
                  await fetch(`${API_BASE}/robots/${selectedUnassignedIp}/assign`, { 
                    method: "POST", 
                    credentials: "include",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({ mapId: currentMapId })
                  });
                  setRecentlyAssigned(prev => ({ ...prev, [selectedUnassignedIp]: Date.now() }));
                  const res = await fetch(`${API_BASE}/robots/unassigned`, { credentials: "include" });
                  if (res.ok) {
                    const data = await res.json();
                    setUnassigned(data.map((r: any) => ({ name: r.name, ip: r.ip })));
                  }
                } catch {}
              }}
              disabled={!selectedUnassignedIp || !currentMapId}
              className="flex items-center gap-2 px-3 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:bg-zinc-800 disabled:text-zinc-500 text-white rounded-xl text-xs font-medium transition-all"
            >
              Assign to Map
            </button>
          </div>

          <button
            onClick={handleAddRobot}
            className="flex items-center gap-2 px-4 py-2 bg-violet-600 hover:bg-violet-500 text-white rounded-xl text-sm font-medium transition-all shadow-[0_0_15px_rgba(124,58,237,0.3)] hover:shadow-[0_0_20px_rgba(124,58,237,0.5)]"
          >
            <Plus size={16} />
            Add Robot
          </button>
        </div>
      </div>

      <div className="flex-1 flex gap-4 overflow-hidden">
        {/* Main Canvas */}
        <div
          ref={canvasRef}
          className={`flex-1 relative rounded-3xl border border-zinc-800 bg-zinc-950 overflow-hidden shadow-inner group ${
            navigateRobotId ? "cursor-cell" : (isPanning ? "cursor-grabbing" : draggingRobotId ? "cursor-grabbing" : "cursor-crosshair")
          }`}
          onWheel={handleWheel}
          onMouseDown={handleMouseDown}
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          onMouseLeave={handleMouseUp}
          >
          {/* Grid Background */}
          <div
            className="absolute inset-[-5000%] opacity-10 pointer-events-none"
            style={{
              backgroundImage: `radial-gradient(circle at ${DOT_SIZE}px ${DOT_SIZE}px, #71717a ${DOT_SIZE}px, transparent 0)`,
              backgroundSize: `${GRID_SIZE * view.k}px ${GRID_SIZE * view.k}`,
              backgroundPosition: `${view.x}px ${view.y}px`,
            }}
          />

          {/* World Container */}
          <div
            className="absolute origin-top-left will-change-transform"
            style={{ transform: `translate(${view.x}px, ${view.y}px) scale(${view.k})` }}
          >
            {/* Map Rendering (Scaled by PIXELS_PER_METER) */}
            <svg className="overflow-visible pointer-events-none">
              <defs>
                <marker
                  id="arrow"
                  markerWidth="10"
                  markerHeight="7"
                  refX="28"
                  refY="3.5"
                  orient="auto"
                >
                  <polygon points="0 0, 10 3.5, 0 7" fill="#52525b" />
                </marker>
              </defs>

              {/* Paths */}
              {mapPaths.map((path) => {
                const source = mapNodes.find((n) => n.id === path.sourceId);
                const target = mapNodes.find((n) => n.id === path.targetId);
                if (!source || !target) return null;
                const isMaint = path.status === "maintenance";
                const isRest = !!path.rest;

                if (path.points && path.points.length > 0) {
                    const pointsStr = path.points.map(p => `${p.x * PIXELS_PER_METER},${p.y * PIXELS_PER_METER}`).join(" ");
                    return (
                        <polyline
                            key={path.id}
                            points={pointsStr}
                            stroke="#52525b"
                            strokeWidth="20"
                            strokeLinecap="round"
                            fill="none"
                            className="opacity-20"
                        />
                    );
                }

                return (
                  <line
                    key={path.id}
                    x1={source.x * PIXELS_PER_METER}
                    y1={source.y * PIXELS_PER_METER}
                    x2={target.x * PIXELS_PER_METER}
                    y2={target.y * PIXELS_PER_METER}
                    stroke="#52525b"
                    strokeWidth="20" // Thick base for better visibility
                    strokeLinecap="round"
                    className="opacity-20"
                  />
                );
              })}
              
               {/* Path Inner Lines (Decoration) */}
               {mapPaths.map((path) => {
                const source = mapNodes.find((n) => n.id === path.sourceId);
                const target = mapNodes.find((n) => n.id === path.targetId);
                if (!source || !target) return null;
                const isMaint = path.status === "maintenance";
                const isRest = !!path.rest;
                const isOneWay = path.direction === "one-way";

                if (path.points && path.points.length > 0) {
                    const pointsStr = path.points.map(p => `${p.x * PIXELS_PER_METER},${p.y * PIXELS_PER_METER}`).join(" ");
                    return (
                        <polyline
                            key={`inner-${path.id}`}
                            points={pointsStr}
                            stroke={isMaint ? "#fbbf24" : isRest ? "#22c55e" : "#52525b"}
                            strokeWidth="2"
                            strokeDasharray={isMaint ? "2,6" : "none"}
                            fill="none"
                            markerEnd={isOneWay ? "url(#arrow)" : undefined}
                        />
                    );
                }

                return (
                  <line
                    key={`inner-${path.id}`}
                    x1={source.x * PIXELS_PER_METER}
                    y1={source.y * PIXELS_PER_METER}
                    x2={target.x * PIXELS_PER_METER}
                    y2={target.y * PIXELS_PER_METER}
                    stroke={isMaint ? "#fbbf24" : isRest ? "#22c55e" : "#52525b"}
                    strokeWidth="2"
                    strokeDasharray={isMaint ? "2,6" : "none"}
                    markerEnd={isOneWay ? "url(#arrow)" : undefined}
                  />
                );
              })}
            {/* Route Overlay */}
            {Object.entries(routes).map(([ip, route]) => {
              if (route.mapId !== currentMapId || !route.nodes || route.nodes.length < 2) return null;
              
              const pointsStr = route.nodes.map(p => `${p.x * PIXELS_PER_METER},${-p.y * PIXELS_PER_METER}`).join(" ");
              
              return (
                <g key={`route-${ip}`}>
                   {/* Glow effect */}
                   <polyline
                    points={pointsStr}
                    stroke="#38bdf8" // Sky-400
                    strokeWidth="6"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    fill="none"
                    className="opacity-20 blur-md"
                  />
                  {/* Main line */}
                  <polyline
                    points={pointsStr}
                    stroke="#38bdf8" // Sky-400
                    strokeWidth="1.5"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                    fill="none"
                    strokeDasharray="4 2"
                    className="opacity-90"
                  />
                   {/* Start/End Markers */}
                   {route.nodes.length > 0 && (
                      <circle 
                        cx={route.nodes[route.nodes.length - 1].x * PIXELS_PER_METER} 
                        cy={-route.nodes[route.nodes.length - 1].y * PIXELS_PER_METER} 
                        r="3" 
                        fill="#38bdf8"
                        className="animate-ping opacity-75" 
                      />
                   )}
                   {typeof route.length === "number" && route.length > 0 && (
                     <text
                       x={route.nodes[route.nodes.length - 1].x * PIXELS_PER_METER + 8}
                       y={-route.nodes[route.nodes.length - 1].y * PIXELS_PER_METER - 8}
                       fill="#38bdf8"
                       fontSize="10"
                       className="opacity-80"
                     >
                       {`${route.length.toFixed(1)}m`}
                     </text>
                   )}
                </g>
              );
            })}
            </svg>

            {/* Destination Preview While Picking */}
            {navigateRobotId && hoveredMapPos && (
              <div
                className="absolute -ml-2 -mt-2 w-4 h-4 rounded-full bg-emerald-500 shadow-[0_0_10px_rgba(16,185,129,0.6)] border border-white/60 z-30"
                style={{
                  left: hoveredMapPos.x * PIXELS_PER_METER,
                  top: hoveredMapPos.y * PIXELS_PER_METER
                }}
                title="Destination"
              />
            )}

            {/* Nodes */}
            {mapNodes.map((node) => (
              <div
                key={node.id}
                className="absolute w-4 h-4 -ml-2 -mt-2 bg-zinc-900 border-2 border-zinc-500 rounded-full z-10"
                style={{ 
                    left: node.x * PIXELS_PER_METER, 
                    top: node.y * PIXELS_PER_METER 
                }}
              />
            ))}

            {/* Points */}
            {mapPoints.map(point => {
                const pos = getPointPosition(point.pathId, point.distance);
                if (!pos) return null;

                let color = "bg-sky-400"; // Rest
                let icon = <Coffee size={10} />;
                if (point.type === 'charging') {
                    color = "bg-emerald-400 shadow-[0_0_10px_rgba(52,211,153,0.5)]";
                    icon = <Zap size={10} />;
                } else if (point.type === 'drop') {
                    color = "bg-rose-500";
                    icon = <ArrowDownToLine size={10} />;
                }

                return (
                    <div
                        key={point.id}
                        className={`absolute w-5 h-5 -ml-2.5 -mt-2.5 rounded-full flex items-center justify-center text-zinc-950 z-20 ${color}`}
                        style={{ left: pos.x * PIXELS_PER_METER, top: pos.y * PIXELS_PER_METER }}
                    >
                        {icon}
                    </div>
                );
            })}

            {/* Robots */}
            {displayRobots.map((robot) => {
              const isSelected = selectedRobotId === robot.id;
              return (
                <div
                  key={robot.id}
                  className={`absolute flex flex-col items-center justify-center transition-transform duration-75 ${
                    isSelected ? "z-50 scale-110" : "z-40"
                  }`}
                  style={{
                    left: robot.x * PIXELS_PER_METER,
                    top: (-robot.y) * PIXELS_PER_METER,
                    transform: `translate(-50%, -50%)`,
                  }}
                >
                  {/* Robot Body */}
                  <div
                    className={`relative flex items-center justify-center w-12 h-12 rounded-full border-2 shadow-lg backdrop-blur-sm transition-colors ${
                      isSelected
                        ? "border-white bg-violet-500/20 shadow-[0_0_20px_rgba(139,92,246,0.5)]"
                        : "border-zinc-700 bg-zinc-900/80 hover:border-zinc-500"
                    }`}
                  >
                    <Bot
                      size={20}
                      className={isSelected ? "text-white" : "text-zinc-400"}
                      style={{ color: isSelected ? undefined : robot.color }}
                    />
                    
                    {/* Status Indicator */}
                    <div className={`absolute bottom-0 right-0 w-3 h-3 rounded-full border-2 border-zinc-950 ${
                        robot.status === 'error' ? 'bg-rose-500' : 'bg-emerald-500'
                    }`} />
                    {robot.status === 'error' && (
                      <div className="absolute -top-2 -left-2 px-1.5 py-0.5 rounded bg-rose-600 text-white text-[9px] font-bold">OFFLINE</div>
                    )}
                  </div>

                  {/* Robot Label */}
                  <div className={`mt-2 px-2 py-1 rounded-md text-[10px] font-mono font-bold tracking-wider uppercase border ${
                      isSelected ? "bg-zinc-900 text-white border-zinc-700" : "bg-zinc-950/50 text-zinc-500 border-transparent"
                  }`}>
                    {robot.name}
                    {robot.state && (
                      <span className="ml-2 px-1.5 py-0.5 rounded bg-zinc-800 text-zinc-300 border border-zinc-700">
                        {robot.state}
                      </span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Sidebar Controls */}
        <div className="w-80 flex flex-col gap-4">
            
            {/* View Controls */}
            <div className="glass p-4 rounded-3xl border border-zinc-800 bg-zinc-900/50">
                <div className="flex items-center justify-between mb-4">
                    <span className="text-xs font-medium text-zinc-400 uppercase tracking-wider">View Controls</span>
                </div>
                <div className="grid grid-cols-2 gap-2">
                    <button 
                        onClick={() => setView(v => ({ ...v, k: Math.min(v.k * 1.2, 5) }))}
                        className="flex items-center justify-center gap-2 p-3 rounded-xl bg-zinc-800/50 hover:bg-zinc-800 text-zinc-300 transition-colors"
                    >
                        <ZoomIn size={16} />
                        <span className="text-xs">Zoom In</span>
                    </button>
                    <button 
                        onClick={() => setView(v => ({ ...v, k: Math.max(v.k * 0.8, 0.1) }))}
                        className="flex items-center justify-center gap-2 p-3 rounded-xl bg-zinc-800/50 hover:bg-zinc-800 text-zinc-300 transition-colors"
                    >
                        <ZoomOut size={16} />
                        <span className="text-xs">Zoom Out</span>
                    </button>
                    <button 
                        onClick={() => setView({ x: 0, y: 0, k: 1 })}
                        className="col-span-2 flex items-center justify-center gap-2 p-3 rounded-xl bg-zinc-800/50 hover:bg-zinc-800 text-zinc-300 transition-colors"
                    >
                        <Maximize size={16} />
                        <span className="text-xs">Reset View</span>
                    </button>
                </div>
            </div>

            {/* Selected Robot Details */}
            {selectedRobotId ? (
                <div className="flex-1 glass p-5 rounded-3xl border border-zinc-800 bg-zinc-900/50 flex flex-col gap-4 animate-in slide-in-from-right-4 duration-300">
                    {(() => {
                        const robot = displayRobots.find(r => r.id === selectedRobotId);
                        if (!robot) return null;
                        
                        // Check if it is a mock robot to allow deletion
                        const isMock = mockRobots.some(m => m.id === robot.id);

                        return (
                            <>
                                <div className="flex items-center gap-3 pb-4 border-b border-zinc-800">
                                    <div className="w-10 h-10 rounded-full flex items-center justify-center bg-zinc-800">
                                        <Bot size={20} style={{ color: robot.color }} />
                                    </div>
                                    <div>
                                        <h2 className="font-semibold text-white">{robot.name}</h2>
                        <div className="flex items-center gap-1.5 mt-0.5">
                          <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                          <span className="text-xs text-emerald-500 font-medium uppercase">Online</span>
                        </div>
                      </div>
                    </div>

                    <div className="space-y-4">
                      <div className="space-y-2">
                        <label className="text-xs text-zinc-500 uppercase tracking-wider">Position (Meters)</label>
                        <div className="grid grid-cols-2 gap-2">
                          <div className="bg-zinc-950 p-2 rounded-lg border border-zinc-800">
                            <div className="text-[10px] text-zinc-600 mb-1">X Axis</div>
                            <div className="font-mono text-sm text-zinc-300">{robot.x.toFixed(2)}m</div>
                          </div>
                          <div className="bg-zinc-950 p-2 rounded-lg border border-zinc-800">
                            <div className="text-[10px] text-zinc-600 mb-1">Y Axis</div>
                            <div className="font-mono text-sm text-zinc-300">{robot.y.toFixed(2)}m</div>
                          </div>
                        </div>
                      </div>
                      <div className="space-y-2">
                        <label className="text-xs text-zinc-500 uppercase tracking-wider">State</label>
                        <div className="bg-zinc-950 p-2 rounded-lg border border-zinc-800">
                          <div className="font-mono text-sm text-zinc-300">{(robot.state ?? robot.status).toString().toUpperCase()}</div>
                        </div>
                      </div>

                      <div className="space-y-2">
                        <label className="text-xs text-zinc-500 uppercase tracking-wider">Actions</label>
                        <button
                          onClick={() => { setNavigateRobotId(robot.id); }}
                          className="w-full py-2.5 flex items-center justify-center gap-2 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl text-xs font-medium transition-colors"
                        >
                          <Navigation size={14} />
                          Pick Destination
                        </button>
                        <button
                          onClick={() => { setRelocateRobotId(robot.id); }}
                          className="w-full py-2.5 flex items-center justify-center gap-2 bg-zinc-800 hover:bg-zinc-700 text-zinc-200 rounded-xl text-xs font-medium transition-colors"
                        >
                          <Move size={14} />
                          Relocate
                        </button>
                      </div>

                                    {isMock && (
                                        <div className="pt-4 border-t border-zinc-800">
                                            <button 
                                                onClick={() => handleDeleteRobot(robot.id)}
                                                className="w-full py-2.5 flex items-center justify-center gap-2 border border-red-500/20 text-red-400 hover:bg-red-500/10 rounded-xl text-xs font-medium transition-colors"
                                            >
                                                <Trash2 size={14} />
                                                Delete Robot
                                            </button>
                                        </div>
                                    )}
                                </div>
                                
                                <div className="mt-auto pt-4 border-t border-zinc-800">
                                    <p className="text-[10px] text-zinc-600 text-center">
                                        Drag robot on map to relocate manually.
                                    </p>
                                </div>
                            </>
                        );
                    })()}
                </div>
            ) : (
                <div className="flex-1 glass p-5 rounded-3xl border border-zinc-800 bg-zinc-900/50 flex flex-col items-center justify-center text-zinc-600 gap-3">
                    <MousePointer2 size={32} className="opacity-20" />
                    <p className="text-sm font-medium">Select a robot</p>
                    <p className="text-xs text-center opacity-60">Click on any robot on the map<br/>to view details and controls.</p>
                </div>
            )}
        </div>
      </div>
    </div>
  );
}
