"use client";

import {
  ArrowRightLeft,
  Circle,
  Construction,
  Hand,
  Maximize,
  MousePointer2,
  Plus,
  Save,
  ZoomIn,
  ZoomOut,
  QrCode,
  Package,
  Coffee,
} from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import type {
  CreateNodeRequest,
  CreatePathRequest,
  CreatePointRequest,
  CreateQrRequest,
  MapSnapshotDto,
  MapVersionDto,
  NodeDto,
  PathDto,
  PublishMapRequest,
  QrDto,
  UpdateNodeRequest,
  UpdatePathRequest,
  UpdatePointRequest,
  UpdateQrRequest,
  MapPointDto,
  SetMaintenanceRequest,
  SetRestOptionsRequest,
} from "@/lib/api/types";
import { useSignalR } from "@/lib/realtime/useSignalR";
import type { EditorTool, SelectedElement } from "./types";
import { clamp, getPathPolyline, positionAlongPolyline, projectPointToPolyline } from "./geom";

const GRID_SIZE_PX = 20;
const PIXELS_PER_METER = 20;
const MIN_ZOOM = 0.2;
const MAX_ZOOM = 6;
const ZOOM_SENSITIVITY = 0.001;

type View = { x: number; y: number; k: number };

type MapEntityUpdatedPayload = { mapId?: string; mapVersionId?: string };

function isMapEntityUpdatedPayload(v: unknown): v is MapEntityUpdatedPayload {
  return typeof v === "object" && v !== null;
}

function screenToWorldPx(view: View, rect: DOMRect, clientX: number, clientY: number) {
  const x = (clientX - rect.left - view.x) / view.k;
  const y = (clientY - rect.top - view.y) / view.k;
  return { x, y };
}

function pxToWorldMeters(p: { x: number; y: number }) {
  return { x: p.x / PIXELS_PER_METER, y: p.y / PIXELS_PER_METER };
}

function shortId() {
  return Math.random().toString(36).slice(2, 8);
}

function approxEq(a: number, b: number, eps = 1e-6) {
  return Math.abs(a - b) <= eps;
}

export default function MapEditor(props: {
  mapId: string;
  draftVersion: MapVersionDto;
  initialSnapshot: MapSnapshotDto;
}) {
  const { mapId } = props;
  const mapVersionId = props.draftVersion.mapVersionId;

  const [snapshot, setSnapshot] = useState<MapSnapshotDto>(props.initialSnapshot);
  const [selected, setSelected] = useState<SelectedElement | null>(null);
  const [activeTool, setActiveTool] = useState<EditorTool>("select");
  const [view, setView] = useState<View>({ x: 120, y: 120, k: 1 });
  const [isPanning, setIsPanning] = useState(false);
  const [lastMouse, setLastMouse] = useState<{ x: number; y: number } | null>(null);
  const [pathStartNodeId, setPathStartNodeId] = useState<string | null>(null);
  const [autoConnectFromNodeId, setAutoConnectFromNodeId] = useState<string | null>(null);
  const [autoConnectEnabled, setAutoConnectEnabled] = useState(true);
  const [hoverWorldMeters, setHoverWorldMeters] = useState<{ x: number; y: number } | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [autosave, setAutosave] = useState(true);
  type PendingOp =
    | { kind: "node"; value: NodeDto }
    | { kind: "path"; value: PathDto }
    | { kind: "point"; value: MapPointDto }
    | { kind: "qr"; value: QrDto }
    | { kind: "nodeMaint"; nodeId: string; isMaintenance: boolean }
    | { kind: "pathMaint"; pathId: string; isMaintenance: boolean }
    | { kind: "rest"; pathId: string; isRestPath: boolean; restCapacity: number | null; restDwellPolicy: string | null };

  const [pending, setPending] = useState<Record<string, PendingOp>>({});
  const [dragNodeId, setDragNodeId] = useState<string | null>(null);

  const canvasRef = useRef<HTMLDivElement>(null);
  const dragContextRef = useRef<{
    pathOrientation: Record<string, "h" | "v">;
    adjacency: Record<string, { pathId: string; otherId: string }[]>;
    initialNodes: Record<string, { x: number; y: number }>;
  } | null>(null);
  const lastDraggedNodeIdsRef = useRef<Set<string>>(new Set());
  const prevToolRef = useRef<EditorTool>("select");
  const spacePanActiveRef = useRef(false);

  const { connection, isConnected } = useSignalR();

  const nodes = snapshot.nodes;
  const paths = snapshot.paths;
  const points = snapshot.points;
  const qrs = snapshot.qrs;

  const reloadSnapshot = useCallback(async () => {
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/snapshot`, { cache: "no-store" });
    if (!res.ok) return;
    const data = (await res.json()) as MapSnapshotDto;
    setSnapshot(data);
  }, [mapId, mapVersionId]);

  function gridSnapMeters(value: number) {
    const gridMeters = GRID_SIZE_PX / PIXELS_PER_METER;
    return Math.round(value / gridMeters) * gridMeters;
  }

  function isOrthogonal(a: { x: number; y: number }, b: { x: number; y: number }) {
    return approxEq(a.x, b.x) || approxEq(a.y, b.y);
  }

  function buildCascadeContext(s: MapSnapshotDto) {
    const initialNodes: Record<string, { x: number; y: number }> = {};
    for (const n of s.nodes) {
      initialNodes[n.nodeId] = { x: n.geom.x, y: n.geom.y };
    }

    const pathOrientation: Record<string, "h" | "v"> = {};
    const adjacency: Record<string, { pathId: string; otherId: string }[]> = {};
    for (const p of s.paths) {
      const from = s.nodes.find((n) => n.nodeId === p.fromNodeId)?.geom ?? null;
      const to = s.nodes.find((n) => n.nodeId === p.toNodeId)?.geom ?? null;
      if (!from || !to) continue;
      const orient: "h" | "v" = approxEq(from.y, to.y) || Math.abs(from.x - to.x) >= Math.abs(from.y - to.y) ? "h" : "v";
      pathOrientation[p.pathId] = orient;

      adjacency[p.fromNodeId] = adjacency[p.fromNodeId] ?? [];
      adjacency[p.toNodeId] = adjacency[p.toNodeId] ?? [];
      adjacency[p.fromNodeId].push({ pathId: p.pathId, otherId: p.toNodeId });
      adjacency[p.toNodeId].push({ pathId: p.pathId, otherId: p.fromNodeId });
    }

    return { pathOrientation, adjacency, initialNodes };
  }

  function applyCascadeMove(base: MapSnapshotDto, movedNodeId: string, newGeom: { x: number; y: number }) {
    const ctx = buildCascadeContext(base);
    const nodesById = new Map(base.nodes.map((node) => [node.nodeId, { ...node, geom: { ...node.geom } }]));
    const start = nodesById.get(movedNodeId);
    if (!start) return { snapshot: base, changedNodeIds: new Set<string>() };
    start.geom = { x: gridSnapMeters(newGeom.x), y: gridSnapMeters(newGeom.y) };
    nodesById.set(movedNodeId, start);

    const changedIds = new Set<string>([movedNodeId]);
    const queue: string[] = [movedNodeId];
    const visited = new Set<string>();
    while (queue.length > 0) {
      const curId = queue.shift()!;
      if (visited.has(curId)) continue;
      visited.add(curId);
      const cur = nodesById.get(curId);
      if (!cur) continue;
      const edges = ctx.adjacency[curId] ?? [];
      for (const edge of edges) {
        const orient = ctx.pathOrientation[edge.pathId];
        const other = nodesById.get(edge.otherId);
        if (!other || !orient) continue;
        const nextGeom = { ...other.geom };
        if (orient === "h") nextGeom.y = cur.geom.y;
        else nextGeom.x = cur.geom.x;
        nextGeom.x = gridSnapMeters(nextGeom.x);
        nextGeom.y = gridSnapMeters(nextGeom.y);
        if (!approxEq(nextGeom.x, other.geom.x) || !approxEq(nextGeom.y, other.geom.y)) {
          other.geom = nextGeom;
          nodesById.set(other.nodeId, other);
          changedIds.add(other.nodeId);
          queue.push(other.nodeId);
        }
      }
    }

    return { snapshot: { ...base, nodes: base.nodes.map((node) => nodesById.get(node.nodeId) ?? node) }, changedNodeIds: changedIds };
  }

  function alignNodeToTarget(moving: { x: number; y: number }, target: { x: number; y: number }) {
    const dx = target.x - moving.x;
    const dy = target.y - moving.y;
    if (Math.abs(dx) <= Math.abs(dy)) {
      return { x: target.x, y: moving.y };
    }
    return { x: moving.x, y: target.y };
  }

  function snapNodePlacement(worldMeters: { x: number; y: number }) {
    const snapped = { x: gridSnapMeters(worldMeters.x), y: gridSnapMeters(worldMeters.y) };
    if (!autoConnectEnabled) return snapped;
    const from = autoConnectFromNodeId ? nodes.find((n) => n.nodeId === autoConnectFromNodeId)?.geom ?? null : null;
    if (!from) return snapped;

    const dx = snapped.x - from.x;
    const dy = snapped.y - from.y;
    if (Math.abs(dx) >= Math.abs(dy)) {
      return { x: snapped.x, y: from.y };
    }
    return { x: from.x, y: snapped.y };
  }

  const hoveredNodeId = useMemo(() => {
    if (!hoverWorldMeters) return null;
    const thresholdMeters = 0.25;
    let best: { id: string; dist2: number } | null = null;
    for (const n of nodes) {
      const dx = n.geom.x - hoverWorldMeters.x;
      const dy = n.geom.y - hoverWorldMeters.y;
      const d2 = dx * dx + dy * dy;
      if (d2 > thresholdMeters * thresholdMeters) continue;
      if (!best || d2 < best.dist2) best = { id: n.nodeId, dist2: d2 };
    }
    return best?.id ?? null;
  }, [hoverWorldMeters, nodes]);

  useEffect(() => {
    const handlerDown = (e: KeyboardEvent) => {
      const tag = (document.activeElement?.tagName ?? "").toLowerCase();
      if (tag === "input" || tag === "textarea" || tag === "select") return;

      if (e.code === "Space") {
        if (!spacePanActiveRef.current) {
          spacePanActiveRef.current = true;
          prevToolRef.current = activeTool;
          setActiveTool("pan");
        }
        e.preventDefault();
        return;
      }

      if (e.key === "Escape") {
        setPathStartNodeId(null);
        setStatus(null);
        return;
      }

      const key = e.key.toLowerCase();
      if (key === "v") setActiveTool("select");
      if (key === "g") setActiveTool("pan");
      if (key === "n") setActiveTool("node");
      if (key === "p") setActiveTool("path");
      if (key === "a") setAutoConnectEnabled((x) => !x);
    };

    const handlerUp = (e: KeyboardEvent) => {
      if (e.code === "Space") {
        if (spacePanActiveRef.current) {
          spacePanActiveRef.current = false;
          setActiveTool(prevToolRef.current);
        }
        e.preventDefault();
      }
    };

    window.addEventListener("keydown", handlerDown);
    window.addEventListener("keyup", handlerUp);
    return () => {
      window.removeEventListener("keydown", handlerDown);
      window.removeEventListener("keyup", handlerUp);
    };
  }, [activeTool]);

  useEffect(() => {
    const onEntityUpdated = (payload: unknown) => {
      if (!isMapEntityUpdatedPayload(payload)) return;
      if (String(payload.mapId ?? "") !== mapId) return;
      if (String(payload.mapVersionId ?? "") !== mapVersionId) return;
      void reloadSnapshot();
    };

    if (!connection) return;
    connection.on("map.entity.updated", onEntityUpdated);
    return () => {
      connection.off("map.entity.updated", onEntityUpdated);
    };
  }, [connection, mapId, mapVersionId, reloadSnapshot]);

  const selectedNode = useMemo(() => (selected?.type === "node" ? nodes.find((n) => n.nodeId === selected.id) ?? null : null), [selected, nodes]);
  const selectedPath = useMemo(() => (selected?.type === "path" ? paths.find((p) => p.pathId === selected.id) ?? null : null), [selected, paths]);
  const selectedPoint = useMemo(() => (selected?.type === "point" ? points.find((p) => p.pointId === selected.id) ?? null : null), [selected, points]);
  const selectedQr = useMemo(() => (selected?.type === "qr" ? qrs.find((q) => q.qrId === selected.id) ?? null : null), [selected, qrs]);
  const selectedPathLength = useMemo(() => {
    if (!selectedPath) return null;
    const from = nodes.find((n) => n.nodeId === selectedPath.fromNodeId)?.geom ?? null;
    const to = nodes.find((n) => n.nodeId === selectedPath.toNodeId)?.geom ?? null;
    if (!from || !to) return null;
    return Math.hypot(to.x - from.x, to.y - from.y);
  }, [selectedPath, nodes]);

  async function createNodeAt(worldMeters: { x: number; y: number }) {
    const body: CreateNodeRequest = { geom: worldMeters, label: `N${nodes.length + 1}` };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/nodes`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to create node");
      return;
    }
    const node = (await res.json()) as NodeDto;
    setSnapshot((s) => ({ ...s, nodes: [...s.nodes, node] }));
    setSelected({ type: "node", id: node.nodeId });
    setAutoConnectFromNodeId(node.nodeId);
    setStatus("Node created");

    if (autoConnectEnabled && autoConnectFromNodeId && autoConnectFromNodeId !== node.nodeId) {
      await createPathBetween(autoConnectFromNodeId, node.nodeId, false);
    }
  }

  async function createPathBetween(fromNodeId: string, toNodeId: string, selectCreated = true, skipOrthCheck = false) {
    const from = nodes.find((n) => n.nodeId === fromNodeId)?.geom ?? null;
    const to = nodes.find((n) => n.nodeId === toNodeId)?.geom ?? null;
    if (!skipOrthCheck && from && to && !isOrthogonal(from, to)) {
      setStatus("Only 90° paths are allowed");
      return;
    }

    const body: CreatePathRequest = {
      fromNodeId,
      toNodeId,
      direction: "TWO_WAY",
      speedLimit: null,
    };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/paths`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to create path");
      return;
    }
    const path = (await res.json()) as PathDto;
    setSnapshot((s) => ({ ...s, paths: [...s.paths, path] }));
    if (selectCreated) {
      setSelected({ type: "path", id: path.pathId });
    }
    setStatus("Path created");
  }

  async function connectWithAutoAlign(fromNodeId: string, toNodeId: string, continueFromTo = false) {
    const fromNode = snapshot.nodes.find((n) => n.nodeId === fromNodeId) ?? null;
    const toNode = snapshot.nodes.find((n) => n.nodeId === toNodeId) ?? null;
    if (!fromNode || !toNode) return;

    if (!isOrthogonal(fromNode.geom, toNode.geom)) {
      const aligned = alignNodeToTarget(fromNode.geom, toNode.geom);
      const moved = applyCascadeMove(snapshot, fromNodeId, aligned);
      setSnapshot(moved.snapshot);
      for (const id of moved.changedNodeIds) {
        const node = moved.snapshot.nodes.find((n) => n.nodeId === id);
        if (!node) continue;
        queueOrRun(`node:${node.nodeId}`, { kind: "node", value: node }, async () => saveNode(node));
      }
      setStatus("Aligned node to complete 90° path");
    }

    await createPathBetween(fromNodeId, toNodeId, false, true);
    if (continueFromTo) {
      setPathStartNodeId(toNodeId);
      setAutoConnectFromNodeId(toNodeId);
    }
  }

  function applyPathLength(path: PathDto, lengthMeters: number) {
    if (!Number.isFinite(lengthMeters) || lengthMeters <= 0) return;
    const from = snapshot.nodes.find((n) => n.nodeId === path.fromNodeId) ?? null;
    const to = snapshot.nodes.find((n) => n.nodeId === path.toNodeId) ?? null;
    if (!from || !to) return;

    const isH = approxEq(from.geom.y, to.geom.y);
    const isV = approxEq(from.geom.x, to.geom.x);
    if (!isH && !isV) {
      setStatus("Path is not orthogonal; length edit is not supported");
      return;
    }

    if (isH) {
      const sign = Math.sign(to.geom.x - from.geom.x) || 1;
      const next = { x: from.geom.x + sign * lengthMeters, y: from.geom.y };
      const moved = applyCascadeMove(snapshot, to.nodeId, next);
      setSnapshot(moved.snapshot);
      for (const id of moved.changedNodeIds) {
        const node = moved.snapshot.nodes.find((n) => n.nodeId === id);
        if (!node) continue;
        queueOrRun(`node:${node.nodeId}`, { kind: "node", value: node }, async () => saveNode(node));
      }
      setStatus("Path length updated");
      return;
    }

    const sign = Math.sign(to.geom.y - from.geom.y) || 1;
    const next = { x: from.geom.x, y: from.geom.y + sign * lengthMeters };
    const moved = applyCascadeMove(snapshot, to.nodeId, next);
    setSnapshot(moved.snapshot);
    for (const id of moved.changedNodeIds) {
      const node = moved.snapshot.nodes.find((n) => n.nodeId === id);
      if (!node) continue;
      queueOrRun(`node:${node.nodeId}`, { kind: "node", value: node }, async () => saveNode(node));
    }
    setStatus("Path length updated");
  }

  async function createPointAt(worldMeters: { x: number; y: number }) {
    const body: CreatePointRequest = { type: "PICK_DROP", label: `P${points.length + 1}`, geom: worldMeters, attachedNodeId: null };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/points`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to create point");
      return;
    }
    const point = (await res.json()) as MapPointDto;
    setSnapshot((s) => ({ ...s, points: [...s.points, point] }));
    setSelected({ type: "point", id: point.pointId });
    setStatus("Point created");
  }

  async function createQrAt(worldMeters: { x: number; y: number }) {
    let best: { path: PathDto; distanceAlong: number; dist2: number } | null = null;
    for (const path of paths) {
      const poly = getPathPolyline(path, nodes);
      const proj = projectPointToPolyline(worldMeters, poly);
      if (!proj) continue;
      if (!best || proj.dist2 < best.dist2) {
        best = { path, distanceAlong: proj.distanceAlong, dist2: proj.dist2 };
      }
    }
    if (!best) {
      setStatus("Select a path (QR must be on a path)");
      return;
    }

    const body: CreateQrRequest = {
      pathId: best.path.pathId,
      distanceAlongPath: best.distanceAlong,
      qrCode: `QR-${shortId()}`,
    };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/qrs`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to create QR");
      return;
    }
    const qr = (await res.json()) as QrDto;
    setSnapshot((s) => ({ ...s, qrs: [...s.qrs, qr] }));
    setSelected({ type: "qr", id: qr.qrId });
    setStatus("QR created");
  }

  async function publish() {
    const body: PublishMapRequest = { changeSummary: "Publish" };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/publish`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Publish failed");
      return;
    }
    setStatus("Published");
  }

  function resetView() {
    setView({ x: 120, y: 120, k: 1 });
  }

  function handleWheel(e: React.WheelEvent) {
    if (!canvasRef.current) return;
    e.preventDefault();
    const rect = canvasRef.current.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;
    const k = view.k;
    const delta = -e.deltaY * ZOOM_SENSITIVITY;
    const nextK = clamp(k * (1 + delta), MIN_ZOOM, MAX_ZOOM);
    const scale = nextK / k;
    const nextX = mouseX - (mouseX - view.x) * scale;
    const nextY = mouseY - (mouseY - view.y) * scale;
    setView({ x: nextX, y: nextY, k: nextK });
  }

  function handleMouseDown(e: React.MouseEvent) {
    if (!canvasRef.current) return;
    if (activeTool !== "pan" && !e.shiftKey) return;
    setIsPanning(true);
    setLastMouse({ x: e.clientX, y: e.clientY });
  }

  function handleMouseMove(e: React.MouseEvent) {
    if (!isPanning || !lastMouse) return;
    const dx = e.clientX - lastMouse.x;
    const dy = e.clientY - lastMouse.y;
    setView((v) => ({ ...v, x: v.x + dx, y: v.y + dy }));
    setLastMouse({ x: e.clientX, y: e.clientY });
  }

  function updateHoverFromMouseEvent(e: React.MouseEvent) {
    if (!canvasRef.current) return;
    const rect = canvasRef.current.getBoundingClientRect();
    const wp = screenToWorldPx(view, rect, e.clientX, e.clientY);
    const wm = pxToWorldMeters(wp);
    setHoverWorldMeters(wm);
  }

  function handleMouseUp() {
    setIsPanning(false);
    setLastMouse(null);
  }

  async function handleCanvasClick(e: React.MouseEvent) {
    if (!canvasRef.current) return;
    const rect = canvasRef.current.getBoundingClientRect();
    const worldPx = screenToWorldPx(view, rect, e.clientX, e.clientY);
    const worldMeters = pxToWorldMeters(worldPx);

    if (activeTool === "select") {
      setSelected(null);
      setPathStartNodeId(null);
      return;
    }

    if (activeTool === "node") {
      const snapped = snapNodePlacement(worldMeters);
      await createNodeAt(snapped);
      return;
    }

    if (activeTool === "point") {
      await createPointAt(worldMeters);
      return;
    }

    if (activeTool === "qr") {
      await createQrAt(worldMeters);
      return;
    }
  }

  function beginNodeDrag(nodeId: string) {
    setSelected({ type: "node", id: nodeId });
    setDragNodeId(nodeId);
    dragContextRef.current = buildCascadeContext(snapshot);
    lastDraggedNodeIdsRef.current = new Set([nodeId]);
  }

  function endNodeDrag() {
    setDragNodeId(null);
  }

  async function saveNode(node: NodeDto) {
    const body: UpdateNodeRequest = { geom: node.geom, label: node.label, junctionSpeedLimit: node.junctionSpeedLimit ?? null };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/nodes/${node.nodeId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to save node");
      return;
    }
    setStatus("Node saved");
    void reloadSnapshot();
  }

  async function savePath(path: PathDto) {
    const body: UpdatePathRequest = { direction: path.direction, speedLimit: path.speedLimit ?? null };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/paths/${path.pathId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to save path");
      return;
    }
    setStatus("Path saved");
    void reloadSnapshot();
  }

  async function savePathMaintenance(pathId: string, isMaintenance: boolean) {
    const body: SetMaintenanceRequest = { isMaintenance };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/paths/${pathId}/maintenance`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to update path maintenance");
      return;
    }
    setStatus("Path maintenance updated");
    void reloadSnapshot();
  }

  async function saveNodeMaintenance(nodeId: string, isMaintenance: boolean) {
    const body: SetMaintenanceRequest = { isMaintenance };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/nodes/${nodeId}/maintenance`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to update node maintenance");
      return;
    }
    setStatus("Node maintenance updated");
    void reloadSnapshot();
  }

  async function savePathRest(pathId: string, isRestPath: boolean, restCapacity: number | null, restDwellPolicy: string | null) {
    const body: SetRestOptionsRequest = { isRestPath, restCapacity, restDwellPolicy };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/paths/${pathId}/rest`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to update rest options");
      return;
    }
    setStatus("Rest options updated");
    void reloadSnapshot();
  }

  async function savePoint(point: MapPointDto) {
    const body: UpdatePointRequest = { type: point.type, label: point.label, geom: point.geom, attachedNodeId: point.attachedNodeId ?? null };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/points/${point.pointId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to save point");
      return;
    }
    setStatus("Point saved");
    void reloadSnapshot();
  }

  async function saveQr(qr: QrDto) {
    const body: UpdateQrRequest = { pathId: qr.pathId, distanceAlongPath: qr.distanceAlongPath, qrCode: qr.qrCode };
    const res = await fetch(`/api/maps/${mapId}/versions/${mapVersionId}/qrs/${qr.qrId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    });
    if (!res.ok) {
      setStatus("Failed to save QR");
      return;
    }
    setStatus("QR saved");
    void reloadSnapshot();
  }

  async function savePending() {
    const ops = Object.values(pending);
    if (ops.length === 0) return;
    setStatus("Saving...");
    for (const op of ops) {
      switch (op.kind) {
        case "node":
          await saveNode(op.value);
          break;
        case "path":
          await savePath(op.value);
          break;
        case "point":
          await savePoint(op.value);
          break;
        case "qr":
          await saveQr(op.value);
          break;
        case "nodeMaint":
          await saveNodeMaintenance(op.nodeId, op.isMaintenance);
          break;
        case "pathMaint":
          await savePathMaintenance(op.pathId, op.isMaintenance);
          break;
        case "rest":
          await savePathRest(op.pathId, op.isRestPath, op.restCapacity, op.restDwellPolicy);
          break;
      }
    }
    setPending({});
    setStatus("Saved");
  }

  function queueOrRun(key: string, op: PendingOp, run: () => Promise<void>) {
    if (autosave) {
      void run();
      return;
    }
    setPending((p) => ({ ...p, [key]: op }));
    setStatus("Unsaved changes");
  }

  function renderToolbarButton(tool: EditorTool, label: string, icon: React.ReactNode) {
    const active = activeTool === tool;
    return (
      <button
        type="button"
        onClick={() => {
          setActiveTool(tool);
          setPathStartNodeId(null);
          if (tool !== "node") setAutoConnectFromNodeId(null);
        }}
        className={`flex items-center gap-2 rounded-xl px-3 py-2 text-xs font-medium transition-colors ${
          active ? "bg-black text-white dark:bg-white dark:text-black" : "border border-zinc-200 text-zinc-700 hover:bg-zinc-100 dark:border-zinc-800 dark:text-zinc-200 dark:hover:bg-zinc-900"
        }`}
      >
        {icon}
        <span>{label}</span>
      </button>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-col">
          <h1 className="text-2xl font-semibold">Map Editor</h1>
          <div className="text-xs text-zinc-600 dark:text-zinc-400">
            mapId: {mapId} • draft: {mapVersionId}
          </div>
        </div>
        <div className="flex items-center gap-3">
          <div
            className={`rounded-full border px-3 py-1 text-xs ${
              isConnected ? "border-emerald-500/30 text-emerald-500" : "border-red-500/30 text-red-500"
            }`}
          >
            {isConnected ? "Realtime connected" : "Realtime offline"}
          </div>
          <label className="flex items-center gap-2 text-xs">
            <input type="checkbox" checked={autosave} onChange={(e) => setAutosave(e.target.checked)} />
            Autosave
          </label>
          <button
            type="button"
            onClick={savePending}
            disabled={Object.keys(pending).length === 0 || autosave}
            className="flex items-center gap-2 rounded-xl border border-zinc-200 px-3 py-2 text-xs font-medium disabled:opacity-60 dark:border-zinc-800"
          >
            <Save size={14} />
            Save now
          </button>
          <button
            type="button"
            onClick={publish}
            className="flex items-center gap-2 rounded-xl bg-emerald-600 px-3 py-2 text-xs font-medium text-white hover:bg-emerald-500"
          >
            <Plus size={14} />
            Publish
          </button>
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {renderToolbarButton("select", "Select", <MousePointer2 size={14} />)}
        {renderToolbarButton("pan", "Pan", <Hand size={14} />)}
        {renderToolbarButton("node", "Node", <Circle size={14} />)}
        {renderToolbarButton("path", "Path", <ArrowRightLeft size={14} />)}
        {renderToolbarButton("point", "Point", <Package size={14} />)}
        {renderToolbarButton("qr", "QR", <QrCode size={14} />)}

        <label className="ml-2 flex items-center gap-2 text-xs text-zinc-600 dark:text-zinc-400">
          <input type="checkbox" checked={autoConnectEnabled} onChange={(e) => setAutoConnectEnabled(e.target.checked)} />
          Auto-connect
        </label>

        <div className="ml-2 text-xs text-zinc-500">
          {activeTool === "node" ? "Click to place nodes • auto-connect makes 90° paths" : null}
          {activeTool === "path" ? "Click nodes to connect • only 90° allowed" : null}
          {activeTool === "select" ? "Drag nodes to move (aligned movement cascades)" : null}
          {" "}
          <span className="text-zinc-400">•</span>{" "}
          <span className="text-zinc-400">V</span> Select{" "}
          <span className="text-zinc-400">N</span> Node{" "}
          <span className="text-zinc-400">P</span> Path{" "}
          <span className="text-zinc-400">G</span> Pan{" "}
          <span className="text-zinc-400">Space</span> Hold-pan{" "}
          <span className="text-zinc-400">A</span> Auto-connect{" "}
          <span className="text-zinc-400">Esc</span> Cancel
        </div>

        <div className="ml-auto flex items-center gap-2">
          <button
            type="button"
            onClick={() => setView((v) => ({ ...v, k: clamp(v.k * 1.2, MIN_ZOOM, MAX_ZOOM) }))}
            className="rounded-xl border border-zinc-200 p-2 text-zinc-700 hover:bg-zinc-100 dark:border-zinc-800 dark:text-zinc-200 dark:hover:bg-zinc-900"
          >
            <ZoomIn size={14} />
          </button>
          <button
            type="button"
            onClick={() => setView((v) => ({ ...v, k: clamp(v.k / 1.2, MIN_ZOOM, MAX_ZOOM) }))}
            className="rounded-xl border border-zinc-200 p-2 text-zinc-700 hover:bg-zinc-100 dark:border-zinc-800 dark:text-zinc-200 dark:hover:bg-zinc-900"
          >
            <ZoomOut size={14} />
          </button>
          <button
            type="button"
            onClick={resetView}
            className="rounded-xl border border-zinc-200 p-2 text-zinc-700 hover:bg-zinc-100 dark:border-zinc-800 dark:text-zinc-200 dark:hover:bg-zinc-900"
          >
            <Maximize size={14} />
          </button>
        </div>
      </div>

      <div className="flex gap-4">
        <div
          ref={canvasRef}
          className={`relative h-[720px] flex-1 overflow-hidden rounded-3xl border border-zinc-200 bg-zinc-50 dark:border-zinc-800 dark:bg-black ${
            activeTool === "pan" || isPanning ? "cursor-grab active:cursor-grabbing" : "cursor-crosshair"
          }`}
          onWheel={handleWheel}
          onMouseDown={handleMouseDown}
          onMouseMove={(e) => {
            handleMouseMove(e);
            updateHoverFromMouseEvent(e);
          }}
          onMouseUp={handleMouseUp}
          onMouseLeave={handleMouseUp}
          onClick={handleCanvasClick}
        >
          <div
            className="absolute inset-[-5000%] opacity-10"
            style={{
              backgroundImage: "radial-gradient(circle at 1px 1px, #71717a 1px, transparent 0)",
              backgroundSize: `${GRID_SIZE_PX}px ${GRID_SIZE_PX}px`,
            }}
          />

          <div className="absolute origin-top-left will-change-transform" style={{ transform: `translate(${view.x}px, ${view.y}px) scale(${view.k})` }}>
            <svg className="overflow-visible">
              <defs>
                <marker id="arrow-one" markerWidth="10" markerHeight="7" refX="18" refY="3.5" orient="auto">
                  <polygon points="0 0, 10 3.5, 0 7" fill="#0ea5e9" />
                </marker>
                <marker id="arrow-bi" markerWidth="10" markerHeight="7" refX="18" refY="3.5" orient="auto">
                  <polygon points="0 0, 10 3.5, 0 7" fill="#52525b" />
                </marker>
              </defs>

              {paths.map((p) => {
                const poly = getPathPolyline(p, nodes);
                if (poly.length < 2) return null;
                const pointsStr = poly.map((pt) => `${pt.x * PIXELS_PER_METER},${pt.y * PIXELS_PER_METER}`).join(" ");
                const selectedPathId = selected?.type === "path" ? selected.id : null;
                const isSelected = selectedPathId === p.pathId;
                const isMaint = p.isMaintenance;
                const stroke = isMaint ? "#f59e0b" : "#52525b";
                const markerEnd = p.direction === "ONE_WAY" ? "url(#arrow-one)" : "url(#arrow-bi)";
                return (
                  <polyline
                    key={p.pathId}
                    points={pointsStr}
                    stroke={stroke}
                    strokeWidth={20}
                    strokeLinecap="round"
                    fill="none"
                    opacity={isSelected ? 0.5 : 0.2}
                    markerEnd={markerEnd}
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      setSelected({ type: "path", id: p.pathId });
                      setPathStartNodeId(null);
                    }}
                    style={{ pointerEvents: "stroke" }}
                  />
                );
              })}

              {paths.map((p) => {
                const poly = getPathPolyline(p, nodes);
                if (poly.length < 2) return null;
                const pointsStr = poly.map((pt) => `${pt.x * PIXELS_PER_METER},${pt.y * PIXELS_PER_METER}`).join(" ");
                const isSelected = selected?.type === "path" && selected.id === p.pathId;
                const isMaint = p.isMaintenance;
                const stroke = isMaint ? "#f59e0b" : "#0ea5e9";
                return (
                  <polyline
                    key={`${p.pathId}-inner`}
                    points={pointsStr}
                    stroke={stroke}
                    strokeWidth={2}
                    strokeLinecap="round"
                    fill="none"
                    opacity={isSelected ? 0.8 : 0.4}
                    style={{ pointerEvents: "none" }}
                  />
                );
              })}

              {activeTool === "node" && autoConnectEnabled && autoConnectFromNodeId && hoverWorldMeters ? (() => {
                const from = nodes.find((n) => n.nodeId === autoConnectFromNodeId)?.geom ?? null;
                if (!from) return null;
                const to = snapNodePlacement(hoverWorldMeters);
                if (!isOrthogonal(from, to)) return null;
                const pointsStr = `${from.x * PIXELS_PER_METER},${from.y * PIXELS_PER_METER} ${to.x * PIXELS_PER_METER},${to.y * PIXELS_PER_METER}`;
                return (
                  <>
                    <polyline
                      points={pointsStr}
                      stroke="#a1a1aa"
                      strokeWidth={2}
                      fill="none"
                      opacity={0.7}
                      strokeDasharray="6 6"
                      style={{ pointerEvents: "none" }}
                    />
                    <circle cx={to.x * PIXELS_PER_METER} cy={to.y * PIXELS_PER_METER} r={5} fill="#a1a1aa" opacity={0.7} />
                  </>
                );
              })() : null}

              {activeTool === "path" && pathStartNodeId && hoveredNodeId && hoveredNodeId !== pathStartNodeId ? (() => {
                const from = nodes.find((n) => n.nodeId === pathStartNodeId)?.geom ?? null;
                const to = nodes.find((n) => n.nodeId === hoveredNodeId)?.geom ?? null;
                if (!from || !to) return null;
                const ok = isOrthogonal(from, to);
                const pointsStr = `${from.x * PIXELS_PER_METER},${from.y * PIXELS_PER_METER} ${to.x * PIXELS_PER_METER},${to.y * PIXELS_PER_METER}`;
                return (
                  <polyline
                    points={pointsStr}
                    stroke={ok ? "#a1a1aa" : "#ef4444"}
                    strokeWidth={2}
                    fill="none"
                    opacity={0.7}
                    strokeDasharray="6 6"
                    style={{ pointerEvents: "none" }}
                  />
                );
              })() : null}
            </svg>

            {nodes.map((n) => {
              const isSelected = selected?.type === "node" && selected.id === n.nodeId;
              const isPathStart = activeTool === "path" && pathStartNodeId === n.nodeId;
              const isAutoFrom = activeTool === "node" && autoConnectEnabled && autoConnectFromNodeId === n.nodeId;
              const x = n.geom.x * PIXELS_PER_METER;
              const y = n.geom.y * PIXELS_PER_METER;
              const bg = n.isMaintenance ? "bg-amber-500" : "bg-emerald-500";
              return (
                <div
                  key={n.nodeId}
                  className={`absolute -ml-3 -mt-3 h-6 w-6 rounded-full ${bg} cursor-pointer transition-transform ${
                    isSelected
                      ? "ring-2 ring-white scale-125"
                      : isPathStart
                        ? "ring-2 ring-sky-400 scale-125"
                        : isAutoFrom
                          ? "ring-2 ring-zinc-300 dark:ring-zinc-700 scale-125"
                          : "hover:scale-125"
                  }`}
                  data-start={isPathStart || isAutoFrom ? "1" : "0"}
                  style={{ left: x, top: y }}
                  onMouseDown={(e) => {
                    e.stopPropagation();
                    if (activeTool !== "select") return;
                    beginNodeDrag(n.nodeId);
                  }}
                  onMouseMove={(e) => {
                    if (dragNodeId !== n.nodeId) return;
                    if (!canvasRef.current) return;
                    const rect = canvasRef.current.getBoundingClientRect();
                    const wp = screenToWorldPx(view, rect, e.clientX, e.clientY);
                    const wm = { x: gridSnapMeters(pxToWorldMeters(wp).x), y: gridSnapMeters(pxToWorldMeters(wp).y) };
                    const ctx = dragContextRef.current;
                    if (!ctx) {
                      setSnapshot((s) => ({
                        ...s,
                        nodes: s.nodes.map((x) => (x.nodeId === n.nodeId ? { ...x, geom: wm } : x)),
                      }));
                      return;
                    }

                    const nodesById = new Map(snapshot.nodes.map((node) => [node.nodeId, { ...node, geom: { ...node.geom } }]));
                    const start = nodesById.get(n.nodeId);
                    if (!start) return;
                    start.geom = wm;
                    nodesById.set(n.nodeId, start);

                    const updatedIds = new Set<string>();
                    const queue: string[] = [n.nodeId];
                    const visited = new Set<string>();
                    while (queue.length > 0) {
                      const curId = queue.shift()!;
                      if (visited.has(curId)) continue;
                      visited.add(curId);
                      const cur = nodesById.get(curId);
                      if (!cur) continue;
                      const edges = ctx.adjacency[curId] ?? [];
                      for (const edge of edges) {
                        const orient = ctx.pathOrientation[edge.pathId];
                        const other = nodesById.get(edge.otherId);
                        if (!other || !orient) continue;
                        const nextGeom = { ...other.geom };
                        if (orient === "h") nextGeom.y = cur.geom.y;
                        else nextGeom.x = cur.geom.x;
                        nextGeom.x = gridSnapMeters(nextGeom.x);
                        nextGeom.y = gridSnapMeters(nextGeom.y);
                        if (!approxEq(nextGeom.x, other.geom.x) || !approxEq(nextGeom.y, other.geom.y)) {
                          other.geom = nextGeom;
                          nodesById.set(other.nodeId, other);
                          updatedIds.add(other.nodeId);
                          queue.push(other.nodeId);
                        }
                      }
                    }

                    updatedIds.add(n.nodeId);
                    lastDraggedNodeIdsRef.current = updatedIds;

                    setSnapshot((s) => ({
                      ...s,
                      nodes: s.nodes.map((node) => nodesById.get(node.nodeId) ?? node),
                    }));
                  }}
                  onMouseUp={(e) => {
                    e.stopPropagation();
                    if (dragNodeId !== n.nodeId) return;
                    endNodeDrag();
                    const ctx = dragContextRef.current;
                    const ids = lastDraggedNodeIdsRef.current;
                    dragContextRef.current = null;
                    lastDraggedNodeIdsRef.current = new Set();
                    if (!ctx) {
                      const updated = snapshot.nodes.find((x) => x.nodeId === n.nodeId);
                      if (!updated) return;
                      queueOrRun(`node:${n.nodeId}`, { kind: "node", value: updated }, async () => saveNode(updated));
                      return;
                    }

                    const changed = snapshot.nodes.filter((node) => {
                      if (!ids.has(node.nodeId)) return false;
                      const before = ctx.initialNodes[node.nodeId];
                      if (!before) return false;
                      return !approxEq(before.x, node.geom.x) || !approxEq(before.y, node.geom.y);
                    });

                    for (const node of changed) {
                      queueOrRun(`node:${node.nodeId}`, { kind: "node", value: node }, async () => saveNode(node));
                    }
                  }}
                  onClick={(e) => {
                    e.stopPropagation();
                    if (activeTool === "path") {
                      if (!pathStartNodeId) {
                        setPathStartNodeId(n.nodeId);
                        setSelected({ type: "node", id: n.nodeId });
                      } else if (pathStartNodeId !== n.nodeId) {
                        void connectWithAutoAlign(pathStartNodeId, n.nodeId, true);
                      }
                      return;
                    }
                    if (activeTool === "node" && autoConnectEnabled && autoConnectFromNodeId && autoConnectFromNodeId !== n.nodeId) {
                      void connectWithAutoAlign(autoConnectFromNodeId, n.nodeId, true);
                      setSelected({ type: "node", id: n.nodeId });
                      return;
                    }
                    setSelected({ type: "node", id: n.nodeId });
                    setPathStartNodeId(null);
                  }}
                />
              );
            })}

            {points.map((p) => {
              const isSelected = selected?.type === "point" && selected.id === p.pointId;
              const x = p.geom.x * PIXELS_PER_METER;
              const y = p.geom.y * PIXELS_PER_METER;
              const bg = p.type === "CHARGE" ? "bg-amber-500" : "bg-sky-400";
              return (
                <div
                  key={p.pointId}
                  className={`absolute -ml-2 -mt-2 h-4 w-4 rounded-full ${bg} cursor-pointer transition-transform ${
                    isSelected ? "ring-2 ring-white scale-125" : "hover:scale-125"
                  }`}
                  style={{ left: x, top: y }}
                  onClick={(e) => {
                    e.stopPropagation();
                    setSelected({ type: "point", id: p.pointId });
                    setPathStartNodeId(null);
                  }}
                />
              );
            })}

            {qrs.map((q) => {
              const path = paths.find((p) => p.pathId === q.pathId);
              if (!path) return null;
              const poly = getPathPolyline(path, nodes);
              const pos = positionAlongPolyline(poly, q.distanceAlongPath);
              if (!pos) return null;
              const x = pos.x * PIXELS_PER_METER;
              const y = pos.y * PIXELS_PER_METER;
              const isSelected = selected?.type === "qr" && selected.id === q.qrId;
              return (
                <div
                  key={q.qrId}
                  className={`absolute -ml-2 -mt-2 h-4 w-4 rounded-md bg-purple-500 cursor-pointer transition-transform ${
                    isSelected ? "ring-2 ring-white scale-125" : "hover:scale-125"
                  }`}
                  style={{ left: x, top: y }}
                  onClick={(e) => {
                    e.stopPropagation();
                    setSelected({ type: "qr", id: q.qrId });
                    setPathStartNodeId(null);
                  }}
                />
              );
            })}
          </div>
        </div>

        <div className="w-80 shrink-0 rounded-3xl border border-zinc-200 bg-white p-5 dark:border-zinc-800 dark:bg-black">
          <div className="flex items-center justify-between gap-3 border-b border-zinc-200 pb-3 dark:border-zinc-800">
            <div className="text-sm font-medium">Properties</div>
            <div className="text-xs text-zinc-500">{selected ? `${selected.type} • ${selected.id.slice(0, 8)}` : "None"}</div>
          </div>

          <div className="mt-4 flex flex-col gap-4">
            {!selected ? (
              <div className="text-sm text-zinc-600 dark:text-zinc-400">
                Select an element to edit properties. Tools:
                <div className="mt-2 grid grid-cols-2 gap-2 text-xs">
                  <div className="flex items-center gap-2">
                    <Circle size={14} /> Node
                  </div>
                  <div className="flex items-center gap-2">
                    <ArrowRightLeft size={14} /> Path
                  </div>
                  <div className="flex items-center gap-2">
                    <Package size={14} /> Point
                  </div>
                  <div className="flex items-center gap-2">
                    <QrCode size={14} /> QR
                  </div>
                </div>
              </div>
            ) : null}

            {selectedNode ? (
              <div className="flex flex-col gap-3">
                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Label</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    value={selectedNode.label}
                    onChange={(e) => {
                      const label = e.target.value;
                      setSnapshot((s) => ({ ...s, nodes: s.nodes.map((n) => (n.nodeId === selectedNode.nodeId ? { ...n, label } : n)) }));
                      const updated = { ...selectedNode, label };
                      queueOrRun(`node:${selectedNode.nodeId}`, { kind: "node", value: updated }, async () => saveNode(updated));
                    }}
                  />
                </label>

                <div className="grid grid-cols-2 gap-2">
                  <label className="flex flex-col gap-1">
                    <span className="text-xs text-zinc-500">X</span>
                    <input
                      className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                      type="number"
                      step="0.1"
                      value={selectedNode.geom.x}
                      onChange={(e) => {
                        const x = Number(e.target.value);
                        const updated = { ...selectedNode, geom: { ...selectedNode.geom, x } };
                        setSnapshot((s) => ({ ...s, nodes: s.nodes.map((n) => (n.nodeId === selectedNode.nodeId ? updated : n)) }));
                        queueOrRun(`node:${selectedNode.nodeId}`, { kind: "node", value: updated }, async () => saveNode(updated));
                      }}
                    />
                  </label>
                  <label className="flex flex-col gap-1">
                    <span className="text-xs text-zinc-500">Y</span>
                    <input
                      className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                      type="number"
                      step="0.1"
                      value={selectedNode.geom.y}
                      onChange={(e) => {
                        const y = Number(e.target.value);
                        const updated = { ...selectedNode, geom: { ...selectedNode.geom, y } };
                        setSnapshot((s) => ({ ...s, nodes: s.nodes.map((n) => (n.nodeId === selectedNode.nodeId ? updated : n)) }));
                        queueOrRun(`node:${selectedNode.nodeId}`, { kind: "node", value: updated }, async () => saveNode(updated));
                      }}
                    />
                  </label>
                </div>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Junction speed limit</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    type="number"
                    step="0.1"
                    value={selectedNode.junctionSpeedLimit ?? ""}
                    onChange={(e) => {
                      const v = e.target.value.trim();
                      const junctionSpeedLimit = v === "" ? null : Number(v);
                      const updated = { ...selectedNode, junctionSpeedLimit };
                      setSnapshot((s) => ({ ...s, nodes: s.nodes.map((n) => (n.nodeId === selectedNode.nodeId ? updated : n)) }));
                      queueOrRun(`node:${selectedNode.nodeId}`, { kind: "node", value: updated }, async () => saveNode(updated));
                    }}
                  />
                </label>

                <button
                  type="button"
                  className={`flex items-center justify-center gap-2 rounded-xl border px-3 py-2 text-sm ${
                    selectedNode.isMaintenance ? "border-amber-500/40 text-amber-500" : "border-zinc-200 text-zinc-700 dark:border-zinc-800 dark:text-zinc-200"
                  }`}
                  onClick={() => {
                    const next = !selectedNode.isMaintenance;
                    setSnapshot((s) => ({ ...s, nodes: s.nodes.map((n) => (n.nodeId === selectedNode.nodeId ? { ...n, isMaintenance: next } : n)) }));
                    queueOrRun(`nodeMaint:${selectedNode.nodeId}`, { kind: "nodeMaint", nodeId: selectedNode.nodeId, isMaintenance: next }, async () =>
                      saveNodeMaintenance(selectedNode.nodeId, next),
                    );
                  }}
                >
                  <Construction size={14} />
                  {selectedNode.isMaintenance ? "Maintenance: ON" : "Maintenance: OFF"}
                </button>
              </div>
            ) : null}

            {selectedPath ? (
              <div className="flex flex-col gap-3">
                <div className="grid grid-cols-2 gap-2 text-xs text-zinc-500">
                  <div className="truncate">From: {selectedPath.fromNodeId.slice(0, 8)}</div>
                  <div className="truncate">To: {selectedPath.toNodeId.slice(0, 8)}</div>
                </div>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Direction</span>
                  <select
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    value={selectedPath.direction}
                    onChange={(e) => {
                      const direction = e.target.value;
                      const updated = { ...selectedPath, direction };
                      setSnapshot((s) => ({ ...s, paths: s.paths.map((p) => (p.pathId === selectedPath.pathId ? updated : p)) }));
                      queueOrRun(`path:${selectedPath.pathId}`, { kind: "path", value: updated }, async () => savePath(updated));
                    }}
                  >
                    <option value="TWO_WAY">TWO_WAY</option>
                    <option value="ONE_WAY">ONE_WAY</option>
                  </select>
                </label>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Speed limit</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    type="number"
                    step="0.1"
                    value={selectedPath.speedLimit ?? ""}
                    onChange={(e) => {
                      const v = e.target.value.trim();
                      const speedLimit = v === "" ? null : Number(v);
                      const updated = { ...selectedPath, speedLimit };
                      setSnapshot((s) => ({ ...s, paths: s.paths.map((p) => (p.pathId === selectedPath.pathId ? updated : p)) }));
                      queueOrRun(`path:${selectedPath.pathId}`, { kind: "path", value: updated }, async () => savePath(updated));
                    }}
                  />
                </label>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Length (m)</span>
                  <input
                    key={selectedPath.pathId}
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    type="number"
                    step="0.1"
                    defaultValue={selectedPathLength == null ? "" : selectedPathLength.toFixed(2)}
                    onKeyDown={(e) => {
                      if (e.key !== "Enter") return;
                      const v = (e.currentTarget.value ?? "").trim();
                      const len = Number(v);
                      if (!Number.isFinite(len) || len <= 0) return;
                      applyPathLength(selectedPath, len);
                    }}
                    onBlur={(e) => {
                      const v = (e.currentTarget.value ?? "").trim();
                      if (v.length === 0) return;
                      const len = Number(v);
                      if (!Number.isFinite(len) || len <= 0) return;
                      applyPathLength(selectedPath, len);
                    }}
                  />
                </label>

                <button
                  type="button"
                  className={`flex items-center justify-center gap-2 rounded-xl border px-3 py-2 text-sm ${
                    selectedPath.isMaintenance ? "border-amber-500/40 text-amber-500" : "border-zinc-200 text-zinc-700 dark:border-zinc-800 dark:text-zinc-200"
                  }`}
                  onClick={() => {
                    const next = !selectedPath.isMaintenance;
                    setSnapshot((s) => ({ ...s, paths: s.paths.map((p) => (p.pathId === selectedPath.pathId ? { ...p, isMaintenance: next } : p)) }));
                    queueOrRun(`pathMaint:${selectedPath.pathId}`, { kind: "pathMaint", pathId: selectedPath.pathId, isMaintenance: next }, async () =>
                      savePathMaintenance(selectedPath.pathId, next),
                    );
                  }}
                >
                  <Construction size={14} />
                  {selectedPath.isMaintenance ? "Maintenance: ON" : "Maintenance: OFF"}
                </button>

                <div className="h-px bg-zinc-200 dark:bg-zinc-800" />

                <button
                  type="button"
                  className={`flex items-center justify-center gap-2 rounded-xl border px-3 py-2 text-sm ${
                    selectedPath.isRestPath ? "border-sky-500/40 text-sky-500" : "border-zinc-200 text-zinc-700 dark:border-zinc-800 dark:text-zinc-200"
                  }`}
                  onClick={() => {
                    const next = !selectedPath.isRestPath;
                    setSnapshot((s) => ({ ...s, paths: s.paths.map((p) => (p.pathId === selectedPath.pathId ? { ...p, isRestPath: next } : p)) }));
                    queueOrRun(
                      `rest:${selectedPath.pathId}`,
                      { kind: "rest", pathId: selectedPath.pathId, isRestPath: next, restCapacity: selectedPath.restCapacity ?? null, restDwellPolicy: selectedPath.restDwellPolicy ?? null },
                      async () => savePathRest(selectedPath.pathId, next, selectedPath.restCapacity ?? null, selectedPath.restDwellPolicy ?? null),
                    );
                  }}
                >
                  <Coffee size={14} />
                  {selectedPath.isRestPath ? "Rest: ON" : "Rest: OFF"}
                </button>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Rest capacity</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    type="number"
                    value={selectedPath.restCapacity ?? ""}
                    onChange={(e) => {
                      const v = e.target.value.trim();
                      const restCapacity = v === "" ? null : Number(v);
                      const updated = { ...selectedPath, restCapacity };
                      setSnapshot((s) => ({ ...s, paths: s.paths.map((p) => (p.pathId === selectedPath.pathId ? updated : p)) }));
                      queueOrRun(
                        `rest:${selectedPath.pathId}`,
                        { kind: "rest", pathId: selectedPath.pathId, isRestPath: updated.isRestPath, restCapacity, restDwellPolicy: updated.restDwellPolicy ?? null },
                        async () => savePathRest(updated.pathId, updated.isRestPath, restCapacity, updated.restDwellPolicy ?? null),
                      );
                    }}
                  />
                </label>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Rest dwell policy</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    value={selectedPath.restDwellPolicy ?? ""}
                    onChange={(e) => {
                      const v = e.target.value;
                      const restDwellPolicy = v.trim().length === 0 ? null : v;
                      const updated = { ...selectedPath, restDwellPolicy };
                      setSnapshot((s) => ({ ...s, paths: s.paths.map((p) => (p.pathId === selectedPath.pathId ? updated : p)) }));
                      queueOrRun(
                        `rest:${selectedPath.pathId}`,
                        { kind: "rest", pathId: selectedPath.pathId, isRestPath: updated.isRestPath, restCapacity: updated.restCapacity ?? null, restDwellPolicy },
                        async () => savePathRest(updated.pathId, updated.isRestPath, updated.restCapacity ?? null, restDwellPolicy),
                      );
                    }}
                  />
                </label>
              </div>
            ) : null}

            {selectedPoint ? (
              <div className="flex flex-col gap-3">
                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Type</span>
                  <select
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    value={selectedPoint.type}
                    onChange={(e) => {
                      const type = e.target.value;
                      const updated = { ...selectedPoint, type };
                      setSnapshot((s) => ({ ...s, points: s.points.map((p) => (p.pointId === selectedPoint.pointId ? updated : p)) }));
                      queueOrRun(`point:${selectedPoint.pointId}`, { kind: "point", value: updated }, async () => savePoint(updated));
                    }}
                  >
                    <option value="PICK_DROP">PICK_DROP</option>
                    <option value="CHARGE">CHARGE</option>
                  </select>
                </label>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Label</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    value={selectedPoint.label}
                    onChange={(e) => {
                      const label = e.target.value;
                      const updated = { ...selectedPoint, label };
                      setSnapshot((s) => ({ ...s, points: s.points.map((p) => (p.pointId === selectedPoint.pointId ? updated : p)) }));
                      queueOrRun(`point:${selectedPoint.pointId}`, { kind: "point", value: updated }, async () => savePoint(updated));
                    }}
                  />
                </label>

                <div className="grid grid-cols-2 gap-2">
                  <label className="flex flex-col gap-1">
                    <span className="text-xs text-zinc-500">X</span>
                    <input
                      className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                      type="number"
                      step="0.1"
                      value={selectedPoint.geom.x}
                      onChange={(e) => {
                        const x = Number(e.target.value);
                        const updated = { ...selectedPoint, geom: { ...selectedPoint.geom, x } };
                        setSnapshot((s) => ({ ...s, points: s.points.map((p) => (p.pointId === selectedPoint.pointId ? updated : p)) }));
                        queueOrRun(`point:${selectedPoint.pointId}`, { kind: "point", value: updated }, async () => savePoint(updated));
                      }}
                    />
                  </label>
                  <label className="flex flex-col gap-1">
                    <span className="text-xs text-zinc-500">Y</span>
                    <input
                      className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                      type="number"
                      step="0.1"
                      value={selectedPoint.geom.y}
                      onChange={(e) => {
                        const y = Number(e.target.value);
                        const updated = { ...selectedPoint, geom: { ...selectedPoint.geom, y } };
                        setSnapshot((s) => ({ ...s, points: s.points.map((p) => (p.pointId === selectedPoint.pointId ? updated : p)) }));
                        queueOrRun(`point:${selectedPoint.pointId}`, { kind: "point", value: updated }, async () => savePoint(updated));
                      }}
                    />
                  </label>
                </div>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Attached node id</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm font-mono dark:border-zinc-800"
                    value={selectedPoint.attachedNodeId ?? ""}
                    onChange={(e) => {
                      const v = e.target.value.trim();
                      const attachedNodeId = v.length === 0 ? null : v;
                      const updated = { ...selectedPoint, attachedNodeId };
                      setSnapshot((s) => ({ ...s, points: s.points.map((p) => (p.pointId === selectedPoint.pointId ? updated : p)) }));
                      queueOrRun(`point:${selectedPoint.pointId}`, { kind: "point", value: updated }, async () => savePoint(updated));
                    }}
                  />
                </label>
              </div>
            ) : null}

            {selectedQr ? (
              <div className="flex flex-col gap-3">
                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">QR code</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm font-mono dark:border-zinc-800"
                    value={selectedQr.qrCode}
                    onChange={(e) => {
                      const qrCode = e.target.value;
                      const updated = { ...selectedQr, qrCode };
                      setSnapshot((s) => ({ ...s, qrs: s.qrs.map((q) => (q.qrId === selectedQr.qrId ? updated : q)) }));
                      queueOrRun(`qr:${selectedQr.qrId}`, { kind: "qr", value: updated }, async () => saveQr(updated));
                    }}
                  />
                </label>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Path id</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm font-mono dark:border-zinc-800"
                    value={selectedQr.pathId}
                    onChange={(e) => {
                      const pathId = e.target.value.trim();
                      const updated = { ...selectedQr, pathId };
                      setSnapshot((s) => ({ ...s, qrs: s.qrs.map((q) => (q.qrId === selectedQr.qrId ? updated : q)) }));
                      queueOrRun(`qr:${selectedQr.qrId}`, { kind: "qr", value: updated }, async () => saveQr(updated));
                    }}
                  />
                </label>

                <label className="flex flex-col gap-1">
                  <span className="text-xs text-zinc-500">Distance along path</span>
                  <input
                    className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
                    type="number"
                    step="0.1"
                    value={selectedQr.distanceAlongPath}
                    onChange={(e) => {
                      const distanceAlongPath = Number(e.target.value);
                      const updated = { ...selectedQr, distanceAlongPath };
                      setSnapshot((s) => ({ ...s, qrs: s.qrs.map((q) => (q.qrId === selectedQr.qrId ? updated : q)) }));
                      queueOrRun(`qr:${selectedQr.qrId}`, { kind: "qr", value: updated }, async () => saveQr(updated));
                    }}
                  />
                </label>
              </div>
            ) : null}

            {status ? <div className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">{status}</div> : null}
          </div>
        </div>
      </div>
    </div>
  );
}
