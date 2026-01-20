"use client";

import { Hand, Maximize, MousePointer2, ZoomIn, ZoomOut } from "lucide-react";
import { useMemo, useRef, useState } from "react";

import type { MapSnapshotDto, NodeDto, PathDto, MapPointDto, QrDto } from "@/lib/api/types";
import type { EditorTool, SelectedElement } from "./types";
import { clamp, getPathPolyline, positionAlongPolyline } from "./geom";

const GRID_SIZE_PX = 20;
const PIXELS_PER_METER = 20;
const MIN_ZOOM = 0.2;
const MAX_ZOOM = 6;
const ZOOM_SENSITIVITY = 0.001;

type View = { x: number; y: number; k: number };

export default function MapViewer(props: { snapshot: MapSnapshotDto }) {
  const snapshot = props.snapshot;
  const [selected, setSelected] = useState<SelectedElement | null>(null);
  const [activeTool, setActiveTool] = useState<EditorTool>("select");
  const [view, setView] = useState<View>({ x: 120, y: 120, k: 1 });
  const [isPanning, setIsPanning] = useState(false);
  const [lastMouse, setLastMouse] = useState<{ x: number; y: number } | null>(null);

  const canvasRef = useRef<HTMLDivElement>(null);

  const nodes = snapshot.nodes;
  const paths = snapshot.paths;
  const points = snapshot.points;
  const qrs = snapshot.qrs;

  const selectedNode = useMemo(() => (selected?.type === "node" ? nodes.find((n) => n.nodeId === selected.id) ?? null : null), [selected, nodes]);
  const selectedPath = useMemo(() => (selected?.type === "path" ? paths.find((p) => p.pathId === selected.id) ?? null : null), [selected, paths]);
  const selectedPoint = useMemo(() => (selected?.type === "point" ? points.find((p) => p.pointId === selected.id) ?? null : null), [selected, points]);
  const selectedQr = useMemo(() => (selected?.type === "qr" ? qrs.find((q) => q.qrId === selected.id) ?? null : null), [selected, qrs]);

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

  function handleMouseUp() {
    setIsPanning(false);
    setLastMouse(null);
  }

  function resetView() {
    setView({ x: 120, y: 120, k: 1 });
  }

  function renderToolbarButton(tool: EditorTool, label: string, icon: React.ReactNode) {
    const active = activeTool === tool;
    return (
      <button
        type="button"
        onClick={() => setActiveTool(tool)}
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
      <div className="flex flex-wrap items-center gap-2">
        {renderToolbarButton("select", "Select", <MousePointer2 size={14} />)}
        {renderToolbarButton("pan", "Pan", <Hand size={14} />)}
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
          onMouseMove={handleMouseMove}
          onMouseUp={handleMouseUp}
          onMouseLeave={handleMouseUp}
          onClick={() => {
            if (activeTool === "select") setSelected(null);
          }}
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
              {paths.map((p) => {
                const poly = getPathPolyline(p, nodes);
                if (poly.length < 2) return null;
                const pointsStr = poly.map((pt) => `${pt.x * PIXELS_PER_METER},${pt.y * PIXELS_PER_METER}`).join(" ");
                const isSelected = selected?.type === "path" && selected.id === p.pathId;
                const isMaint = p.isMaintenance;
                const stroke = isMaint ? "#f59e0b" : "#52525b";
                return (
                  <polyline
                    key={p.pathId}
                    points={pointsStr}
                    stroke={stroke}
                    strokeWidth={20}
                    strokeLinecap="round"
                    fill="none"
                    opacity={isSelected ? 0.5 : 0.2}
                    onClick={(e) => {
                      e.preventDefault();
                      e.stopPropagation();
                      setSelected({ type: "path", id: p.pathId });
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
            </svg>

            {nodes.map((n) => {
              const isSelected = selected?.type === "node" && selected.id === n.nodeId;
              const x = n.geom.x * PIXELS_PER_METER;
              const y = n.geom.y * PIXELS_PER_METER;
              const bg = n.isMaintenance ? "bg-amber-500" : "bg-emerald-500";
              return (
                <div
                  key={n.nodeId}
                  className={`absolute -ml-3 -mt-3 h-6 w-6 rounded-full ${bg} cursor-pointer transition-transform ${
                    isSelected ? "ring-2 ring-white scale-125" : "hover:scale-125"
                  }`}
                  style={{ left: x, top: y }}
                  onClick={(e) => {
                    e.stopPropagation();
                    setSelected({ type: "node", id: n.nodeId });
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

          <div className="mt-4 flex flex-col gap-3 text-sm">
            {selectedNode ? <NodeProps node={selectedNode} /> : null}
            {selectedPath ? <PathProps path={selectedPath} /> : null}
            {selectedPoint ? <PointProps point={selectedPoint} /> : null}
            {selectedQr ? <QrProps qr={selectedQr} /> : null}
            {!selected ? <div className="text-sm text-zinc-600 dark:text-zinc-400">Select an element.</div> : null}
          </div>
        </div>
      </div>
    </div>
  );
}

function NodeProps({ node }: { node: NodeDto }) {
  return (
    <div className="flex flex-col gap-2">
      <div className="text-xs text-zinc-500">Node</div>
      <div className="rounded-xl border border-zinc-200 p-3 dark:border-zinc-800">
        <div className="text-sm font-medium">{node.label}</div>
        <div className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">
          x: {node.geom.x} • y: {node.geom.y}
        </div>
        <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">maintenance: {String(node.isMaintenance)}</div>
        <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">
          junctionSpeedLimit: {node.junctionSpeedLimit ?? "(none)"}
        </div>
      </div>
    </div>
  );
}

function PathProps({ path }: { path: PathDto }) {
  return (
    <div className="flex flex-col gap-2">
      <div className="text-xs text-zinc-500">Path</div>
      <div className="rounded-xl border border-zinc-200 p-3 dark:border-zinc-800">
        <div className="text-xs text-zinc-600 dark:text-zinc-400">direction: {path.direction}</div>
        <div className="text-xs text-zinc-600 dark:text-zinc-400">speedLimit: {path.speedLimit ?? "(none)"}</div>
        <div className="text-xs text-zinc-600 dark:text-zinc-400">maintenance: {String(path.isMaintenance)}</div>
        <div className="text-xs text-zinc-600 dark:text-zinc-400">rest: {String(path.isRestPath)}</div>
      </div>
    </div>
  );
}

function PointProps({ point }: { point: MapPointDto }) {
  return (
    <div className="flex flex-col gap-2">
      <div className="text-xs text-zinc-500">Point</div>
      <div className="rounded-xl border border-zinc-200 p-3 dark:border-zinc-800">
        <div className="text-sm font-medium">{point.label}</div>
        <div className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">type: {point.type}</div>
        <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">
          x: {point.geom.x} • y: {point.geom.y}
        </div>
        <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">attachedNodeId: {point.attachedNodeId ?? "(none)"}</div>
      </div>
    </div>
  );
}

function QrProps({ qr }: { qr: QrDto }) {
  return (
    <div className="flex flex-col gap-2">
      <div className="text-xs text-zinc-500">QR</div>
      <div className="rounded-xl border border-zinc-200 p-3 dark:border-zinc-800">
        <div className="text-sm font-medium">{qr.qrCode}</div>
        <div className="mt-2 text-xs text-zinc-600 dark:text-zinc-400">pathId: {qr.pathId.slice(0, 8)}</div>
        <div className="mt-1 text-xs text-zinc-600 dark:text-zinc-400">distanceAlongPath: {qr.distanceAlongPath}</div>
      </div>
    </div>
  );
}
