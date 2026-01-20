"use client";

import React, { useState, useRef, useEffect } from "react";
import {
  MousePointer2,
  Circle,
  Move,
  Trash2,
  Save,
  FolderOpen,
  Plus,
  Grid3X3,
  Hand,
  ArrowRightLeft,
  ArrowRight,
  Construction,
  ZoomIn,
  ZoomOut,
  Maximize,
  Zap,
  Octagon,
  Coffee,
  QrCode,
  Package,
  ArrowDownToLine,
  CheckCircle2
} from "lucide-react";

// --- Types ---

type NodeStatus = "active" | "maintenance";
type PathStatus = "active" | "maintenance";
type PathDirection = "bidirectional" | "one-way";
type PointType = "charging" | "drop" | "rest";

type NodeType = {
  id: string;
  x: number;
  y: number;
  label?: string;
  status: NodeStatus;
};

type PathType = {
  id: string;
  sourceId: string;
  targetId: string;
  length: number; // Visual length (pixels)
  direction: PathDirection;
  status: PathStatus;
  rest: boolean;
};

type MapPoint = {
  id: string;
  pathId: string;
  distance: number; // Pixels from source node
  type: PointType;
  label?: string;
};

type MapQR = {
  id: string;
  pathId: string;
  distance: number; // Pixels from source node
  data: string;
};

type MapData = {
  id: string;
  name: string;
  nodes: NodeType[];
  paths: PathType[];
  points: MapPoint[];
  qrcodes: MapQR[];
};

// --- Constants ---
const GRID_SIZE = 20;
const API_BASE = "http://localhost:5067";
const PIXELS_PER_METER = 20; // 20px = 1m
const ZOOM_SENSITIVITY = 0.001;
const MIN_ZOOM = 0.1;
const MAX_ZOOM = 5;

// --- Helper Functions ---

const generateId = () => Math.random().toString(36).substr(2, 9);

const snapToGrid = (val: number) => Math.round(val / GRID_SIZE) * GRID_SIZE;

const getDistance = (n1: NodeType, n2: NodeType) => {
  return Math.sqrt(Math.pow(n2.x - n1.x, 2) + Math.pow(n2.y - n1.y, 2));
};

const isAligned = (n1: NodeType, n2: NodeType) => {
  return Math.abs(n1.x - n2.x) < 1 || Math.abs(n1.y - n2.y) < 1;
};

// Recursive function to move a node and propagate movement to maintain orthogonality
const cascadeMoveNode = (
    map: MapData, 
    startNodeId: string, 
    deltaX: number, 
    deltaY: number
  ): MapData => {
    const visited = new Set<string>();
    const nodesMap = new Map(map.nodes.map(n => [n.id, { ...n }])); // Clone nodes
  
    const queue: { id: string, dx: number, dy: number }[] = [];
    queue.push({ id: startNodeId, dx: deltaX, dy: deltaY });
    visited.add(startNodeId);
  
    while (queue.length > 0) {
        const { id, dx, dy } = queue.shift()!;
        const node = nodesMap.get(id)!;
        
        // Apply move
        node.x += dx;
        node.y += dy;
  
        // Propagate to neighbors
        if (dx !== 0) {
            // Moving Horizontally: Propagate to neighbors connected via VERTICAL paths
            const verticalPaths = map.paths.filter(p => 
               (p.sourceId === id || p.targetId === id)
            );
            
            for (const path of verticalPaths) {
                const neighborId = path.sourceId === id ? path.targetId : path.sourceId;
                if (visited.has(neighborId)) continue;
  
                const neighbor = nodesMap.get(neighborId)!;
                // Check if path WAS vertical (before this move step, node was at x-dx)
                // We use current neighbor pos and old node pos
                if (Math.abs((node.x - dx) - neighbor.x) < 1) {
                    visited.add(neighborId);
                    queue.push({ id: neighborId, dx: dx, dy: 0 });
                }
            }
        }
  
        if (dy !== 0) {
            // Moving Vertically: Propagate to neighbors connected via HORIZONTAL paths
            const horizontalPaths = map.paths.filter(p => 
               (p.sourceId === id || p.targetId === id)
            );
            
            for (const path of horizontalPaths) {
                const neighborId = path.sourceId === id ? path.targetId : path.sourceId;
                if (visited.has(neighborId)) continue;
  
                const neighbor = nodesMap.get(neighborId)!;
                // Check if path WAS horizontal
                if (Math.abs((node.y - dy) - neighbor.y) < 1) {
                    visited.add(neighborId);
                    queue.push({ id: neighborId, dx: 0, dy: dy });
                }
            }
        }
    }
  
    // Reconstruct map
    const newNodes = Array.from(nodesMap.values());
    
    // Recalculate lengths
    const newPaths = map.paths.map(p => {
        const source = newNodes.find(n => n.id === p.sourceId);
        const target = newNodes.find(n => n.id === p.targetId);
        if (source && target) {
            return { ...p, length: getDistance(source, target) };
        }
        return p;
    });
  
    return { ...map, nodes: newNodes, paths: newPaths };
  };

// --- Main Component ---

export default function MapPage() {
  // --- State ---
  const [maps, setMaps] = useState<MapData[]>([
    { id: "default", name: "New Map 1", nodes: [], paths: [], points: [], qrcodes: [] },
  ]);
  const [currentMapId, setCurrentMapId] = useState<string>("default");
  
  // Tools
  const [activeTool, setActiveTool] = useState<"select" | "node" | "path" | "pan" | "point" | "qr">("select");
  const [qrBatchConfig, setQrBatchConfig] = useState({ interval: 1, startOffset: 0.5 });

  const handleBatchGenerateQRs = (pathId: string) => {
      const path = currentMap.paths.find(p => p.id === pathId);
      if (!path) return;

      const pathLengthMeters = path.length / PIXELS_PER_METER;
      const newQRs: MapQR[] = [];
      
      let currentDist = qrBatchConfig.startOffset;
      let count = 0;

      while (currentDist < pathLengthMeters) {
          newQRs.push({
              id: generateId(),
              pathId: path.id,
              distance: currentDist * PIXELS_PER_METER,
              data: `QR-${path.id.substring(0,4)}-${count}`
          });
          currentDist += qrBatchConfig.interval;
          count++;
      }

      if (newQRs.length > 0) {
          updateCurrentMap(map => ({
              ...map,
              qrcodes: [...map.qrcodes, ...newQRs]
          }));
          // alert(`Generated ${newQRs.length} QR codes.`);
      }
  };
  
  // View State
  const [view, setView] = useState({ x: 0, y: 0, k: 1 });
  const [isPanning, setIsPanning] = useState(false);
  const [lastMousePos, setLastMousePos] = useState({ x: 0, y: 0 });

  // Interaction State
  const [selectedElement, setSelectedElement] = useState<{
    type: "node" | "path" | "point" | "qr";
    id: string;
  } | null>(null);
  
  const [dragNodeId, setDragNodeId] = useState<string | null>(null);
  const [pathStartNodeId, setPathStartNodeId] = useState<string | null>(null);
  const [mousePos, setMousePos] = useState({ x: 0, y: 0 }); // World coordinates
  const canvasRef = useRef<HTMLDivElement>(null);

  // Derived State
  const currentMap = maps.find((m) => m.id === currentMapId) || maps[0];
  
  const updateCurrentMap = (updater: (map: MapData) => MapData) => {
    setMaps((prev) =>
      prev.map((m) => (m.id === currentMapId ? updater(m) : m))
    );
  };

  // --- Effects ---

  // Clear selection when changing maps
  useEffect(() => {
    setSelectedElement(null);
    const map = maps.find(m => m.id === currentMapId);
    if (map && map.nodes.length > 0) {
        const origin = map.nodes[0];
        setView({ x: 200 - origin.x, y: 200 - origin.y, k: 1 });
    } else {
        setView({ x: 200, y: 200, k: 1 });
    }
  }, [currentMapId]);

  useEffect(() => {
    const loadMaps = async () => {
      try {
        const res = await fetch(`${API_BASE}/maps`, { credentials: "include" });
        if (!res.ok) return;
        const data: { id: number; name: string }[] = await res.json();
        const mapped: MapData[] = data.map(m => ({ id: String(m.id), name: m.name, nodes: [], paths: [], points: [], qrcodes: [] }));
        setMaps(prev => {
          const ids = new Set(prev.map(p => p.id));
          const merged = mapped.map(m => prev.find(p => p.id === m.id) ?? m);
          return merged.length > 0 ? merged : prev;
        });
        if (data.length > 0) {
          setCurrentMapId(String(data[0].id));
        }
      } catch {}
    };
    loadMaps();
  }, []);

  useEffect(() => {
    const idNum = parseInt(currentMapId);
    if (!Number.isFinite(idNum) || idNum <= 0) return;
    const loadGraph = async () => {
      try {
        const res = await fetch(`${API_BASE}/maps/${idNum}/graph`, { credentials: "include" });
        if (!res.ok) return;
        const graph = await res.json();
        const nodes = (graph.nodes as any[]).map((n: any, i: number) => ({
          id: String(n.id ?? i + 1),
          x: n.x * PIXELS_PER_METER,
          y: -n.y * PIXELS_PER_METER,
          label: `N${i + 1}`,
          status: (n.status as NodeStatus) ?? "active"
        })) as NodeType[];
        const nodeIdIndex = new Map<string, number>();
        nodes.forEach((n, i) => nodeIdIndex.set(n.id, i));
        const paths = (graph.paths as any[]).map((p: any, i: number) => ({
          id: String(p.id ?? i + 1),
          sourceId: String(p.startNodeId),
          targetId: String(p.endNodeId),
          length: p.length * PIXELS_PER_METER,
          direction: p.twoWay ? "bidirectional" : "one-way",
          status: (p.status as PathStatus) ?? "active",
          rest: Boolean(p.rest ?? p.Rest ?? false)
        })) as PathType[];
        const pathIdIndex = new Map<string, number>();
        paths.forEach((p, i) => pathIdIndex.set(p.id, i));
        const points = (graph.points as any[]).map((pt: any, i: number) => ({
          id: String(pt.id ?? i + 1),
          pathId: String(pt.pathId ?? 0),
          distance: pt.offset * PIXELS_PER_METER,
          type: (pt.type as PointType) ?? "rest",
          label: pt.name ?? "Point"
        })) as MapPoint[];
        const qrcodes = (graph.qrs as any[]).map((q: any, i: number) => ({
          id: String(q.id ?? i + 1),
          pathId: String(q.pathId ?? 0),
          distance: q.offsetStart * PIXELS_PER_METER,
          data: q.data ?? ""
        })) as MapQR[];
        setMaps(prev => prev.map(m => m.id === currentMapId ? { ...m, nodes, paths, points, qrcodes } : m));
      } catch {}
    };
    loadGraph();
  }, [currentMapId]);


  // Keyboard shortcuts
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.target as HTMLElement).tagName === 'INPUT') return;

      switch (e.key.toLowerCase()) {
        case 'v': setActiveTool('select'); break;
        case 'n': setActiveTool('node'); break;
        case 'p': setActiveTool('path'); break;
        case 'h': setActiveTool('pan'); break;
        case 'o': setActiveTool('point'); break;
        case 'q': setActiveTool('qr'); break;
        case 'delete':
        case 'backspace': handleDeleteElement(); break;
      }
    };

    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [selectedElement, currentMapId, maps]);


  // --- Actions ---

  const showToast = (message: string, type: 'success' | 'error' = 'success') => {
      setToast({ message, type });
      setTimeout(() => setToast(null), 3000);
  };

  const handleSaveMap = () => {
      // In a real app, this would make an API call
      console.log("Saving map:", currentMap);
      showToast("Map saved successfully!");
  };

  const handleCreateMap = () => {
    const newMap: MapData = {
      id: generateId(),
      name: `New Map ${maps.length + 1}`,
      nodes: [],
      paths: [],
      points: [],
      qrcodes: []
    };
    setMaps([...maps, newMap]);
    setCurrentMapId(newMap.id);
  };

  const handleDeleteElement = () => {
    if (!selectedElement) return;

    if (selectedElement.type === "node") {
      updateCurrentMap((map) => ({
        ...map,
        nodes: map.nodes.filter((n) => n.id !== selectedElement.id),
        paths: map.paths.filter(
          (p) =>
            p.sourceId !== selectedElement.id && p.targetId !== selectedElement.id
        ),
      }));
    } else if (selectedElement.type === "path") {
      updateCurrentMap((map) => ({
        ...map,
        paths: map.paths.filter((p) => p.id !== selectedElement.id),
        points: map.points.filter((p) => p.pathId !== selectedElement.id),
        qrcodes: map.qrcodes.filter((q) => q.pathId !== selectedElement.id),
      }));
    } else if (selectedElement.type === "point") {
        updateCurrentMap((map) => ({
            ...map,
            points: map.points.filter((p) => p.id !== selectedElement.id),
        }));
    } else if (selectedElement.type === "qr") {
        updateCurrentMap((map) => ({
            ...map,
            qrcodes: map.qrcodes.filter((q) => q.id !== selectedElement.id),
        }));
    }
    setSelectedElement(null);
  };

  const handleUpdatePathLength = (pathId: string, newLengthMeters: number) => {
    const newLengthPx = newLengthMeters * PIXELS_PER_METER;
    updateCurrentMap((map) => {
        const path = map.paths.find((p) => p.id === pathId);
        if (!path) return map;
        
        const source = map.nodes.find(n => n.id === path.sourceId);
        const target = map.nodes.find(n => n.id === path.targetId);
        
        if (!source || !target) return map;

        // Determine direction and delta for TARGET node
        let deltaX = 0;
        let deltaY = 0;

        if (Math.abs(source.x - target.x) < 1) {
            // Vertical alignment
            const direction = target.y > source.y ? 1 : -1;
            const newTargetY = source.y + (newLengthPx * direction);
            deltaY = newTargetY - target.y;
        } else {
            // Horizontal alignment
            const direction = target.x > source.x ? 1 : -1;
            const newTargetX = source.x + (newLengthPx * direction);
            deltaX = newTargetX - target.x;
        }

        return cascadeMoveNode(map, target.id, deltaX, deltaY);
    });
  };

  const handleSplitPath = (path: PathType, e: React.MouseEvent) => {
    const { x, y } = screenToWorld(e.clientX, e.clientY);
    const snappedX = snapToGrid(x);
    const snappedY = snapToGrid(y);

    const source = currentMap.nodes.find(n => n.id === path.sourceId);
    const target = currentMap.nodes.find(n => n.id === path.targetId);
    if (!source || !target) return;

    // Check if point is strictly on the segment (not endpoints)
    const isVertical = Math.abs(source.x - target.x) < 1;
    const isHorizontal = Math.abs(source.y - target.y) < 1;

    let isValid = false;
    if (isVertical) {
        if (Math.abs(snappedX - source.x) < 1 && 
            snappedY > Math.min(source.y, target.y) && 
            snappedY < Math.max(source.y, target.y)) {
            isValid = true;
        }
    } else if (isHorizontal) {
        if (Math.abs(snappedY - source.y) < 1 && 
            snappedX > Math.min(source.x, target.x) && 
            snappedX < Math.max(source.x, target.x)) {
            isValid = true;
        }
    }

    if (!isValid) {
        alert("Please click strictly on the path line to add a node.");
        return;
    }

    const newNode: NodeType = {
        id: generateId(),
        x: snappedX,
        y: snappedY,
        label: `N${currentMap.nodes.length + 1}`,
        status: 'active'
    };

    const path1: PathType = {
        id: generateId(),
        sourceId: source.id,
        targetId: newNode.id,
        length: getDistance(source, newNode),
        direction: path.direction,
        status: path.status
    };

    const path2: PathType = {
        id: generateId(),
        sourceId: newNode.id,
        targetId: target.id,
        length: getDistance(newNode, target),
        direction: path.direction,
        status: path.status
    };

    updateCurrentMap(map => ({
        ...map,
        nodes: [...map.nodes, newNode],
        paths: [...map.paths.filter(p => p.id !== path.id), path1, path2]
    }));
  };

  const handleAddPointOnPath = (path: PathType, e: React.MouseEvent, type: 'point' | 'qr') => {
      const { x, y } = screenToWorld(e.clientX, e.clientY);
      // Don't snap to grid for points, snap to path projection
      const source = currentMap.nodes.find(n => n.id === path.sourceId);
      const target = currentMap.nodes.find(n => n.id === path.targetId);
      if (!source || !target) return;

      let distance = 0;
      if (Math.abs(source.x - target.x) < 1) {
          // Vertical
          distance = Math.abs(y - source.y);
      } else {
          // Horizontal
          distance = Math.abs(x - source.x);
      }
      
      // Clamp distance
      distance = Math.max(0, Math.min(distance, path.length));

      if (type === 'point') {
          const newPoint: MapPoint = {
              id: generateId(),
              pathId: path.id,
              distance: distance,
              type: 'drop', // Default
              label: 'Point'
          };
          updateCurrentMap(map => ({ ...map, points: [...map.points, newPoint] }));
      } else {
          const newQR: MapQR = {
              id: generateId(),
              pathId: path.id,
              distance: distance,
              data: 'QR-DATA'
          };
          updateCurrentMap(map => ({ ...map, qrcodes: [...map.qrcodes, newQR] }));
      }
  };

  const updateElementStatus = (status: NodeStatus | PathStatus) => {
      if (!selectedElement) return;
      if (selectedElement.type === "node") {
          updateCurrentMap(map => ({
              ...map,
              nodes: map.nodes.map(n => n.id === selectedElement.id ? { ...n, status: status as NodeStatus } : n)
          }));
      } else if (selectedElement.type === "path") {
          updateCurrentMap(map => ({
            ...map,
            paths: map.paths.map(p => p.id === selectedElement.id ? { ...p, status: status as PathStatus } : p)
        }));
      }
  };

  const updatePathDirection = (direction: PathDirection) => {
    if (!selectedElement || selectedElement.type !== 'path') return;
    updateCurrentMap(map => ({
        ...map,
        paths: map.paths.map(p => p.id === selectedElement.id ? { ...p, direction } : p)
    }));
  };

  const handleUpdateNodePosition = (nodeId: string, axis: 'x' | 'y', valueMeters: number) => {
      updateCurrentMap(map => {
          const originNode = map.nodes[0];
          const node = map.nodes.find(n => n.id === nodeId);
          if (!originNode || !node) return map;

          // Convert meter value back to absolute pixels
          let newX = node.x;
          let newY = node.y;
          let deltaX = 0;
          let deltaY = 0;

          if (axis === 'x') {
              const targetX = originNode.x + (valueMeters * PIXELS_PER_METER);
              deltaX = targetX - node.x;
              newX = targetX;
          } else {
              const targetY = originNode.y - (valueMeters * PIXELS_PER_METER);
              deltaY = targetY - node.y;
              newY = targetY;
          }
          
          // Use cascade move if we are moving the node itself to keep connections aligned
          // BUT: cascadeMoveNode is relative delta.
          return cascadeMoveNode(map, nodeId, deltaX, deltaY);
      });
  };

  const getRelativeCoords = (node: NodeType) => {
      const originNode = currentMap.nodes[0];
      if (!originNode) return { x: 0, y: 0 };
      return {
          x: Math.round(((node.x - originNode.x) / PIXELS_PER_METER) * 1000) / 1000,
          y: Math.round(((originNode.y - node.y) / PIXELS_PER_METER) * 1000) / 1000
      };
  };

  // --- Coordinate Systems ---

  // Screen (Client) -> World
  const screenToWorld = (clientX: number, clientY: number) => {
    if (!canvasRef.current) return { x: 0, y: 0 };
    const rect = canvasRef.current.getBoundingClientRect();
    const x = (clientX - rect.left - view.x) / view.k;
    const y = (clientY - rect.top - view.y) / view.k;
    return { x, y };
  };

  // --- Event Handlers ---

  const handleWheel = (e: React.WheelEvent) => {
    e.preventDefault();
    if (e.ctrlKey || e.metaKey) {
        // Zoom
        const zoomIntensity = 0.001;
        const newK = Math.min(Math.max(view.k - e.deltaY * zoomIntensity * view.k, MIN_ZOOM), MAX_ZOOM);
        
        // Zoom towards mouse pointer
        const rect = canvasRef.current!.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        const newX = mouseX - (mouseX - view.x) * (newK / view.k);
        const newY = mouseY - (mouseY - view.y) * (newK / view.k);

        setView({ x: newX, y: newY, k: newK });
    } else {
        // Pan
        setView(v => ({ ...v, x: v.x - e.deltaX, y: v.y - e.deltaY }));
    }
  };

  const handleMouseDown = (e: React.MouseEvent) => {
     // Middle click or Pan tool
     if (e.button === 1 || activeTool === 'pan') {
         setIsPanning(true);
         setLastMousePos({ x: e.clientX, y: e.clientY });
         return;
     }

     const worldPos = screenToWorld(e.clientX, e.clientY);
     const snappedX = snapToGrid(worldPos.x);
     const snappedY = snapToGrid(worldPos.y);

     if (activeTool === "node") {
        const newNode: NodeType = {
            id: generateId(),
            x: snappedX,
            y: snappedY,
            label: `N${currentMap.nodes.length + 1}`,
            status: "active"
        };
        updateCurrentMap((map) => ({ ...map, nodes: [...map.nodes, newNode] }));
        // Keep tool active
     } else if (activeTool === "path") {
         if (pathStartNodeId) {
             // Create aligned node
             const startNode = currentMap.nodes.find(n => n.id === pathStartNodeId);
             if (startNode) {
                 const dx = Math.abs(snappedX - startNode.x);
                 const dy = Math.abs(snappedY - startNode.y);
                 let finalX = dx > dy ? snappedX : startNode.x;
                 let finalY = dx > dy ? startNode.y : snappedY;
                 
                 const newNode: NodeType = {
                    id: generateId(),
                    x: finalX,
                    y: finalY,
                    label: `N${currentMap.nodes.length + 1}`,
                    status: "active"
                };
                const newPath: PathType = {
                    id: generateId(),
                    sourceId: startNode.id,
                    targetId: newNode.id,
                    length: getDistance(startNode, newNode),
                    direction: 'one-way',
                    status: 'active'
                };
                updateCurrentMap(map => ({ ...map, nodes: [...map.nodes, newNode], paths: [...map.paths, newPath] }));
                setPathStartNodeId(newNode.id);
             }
         } else {
             const newNode: NodeType = {
                 id: generateId(),
                 x: snappedX,
                 y: snappedY,
                 label: `N${currentMap.nodes.length + 1}`,
                 status: "active"
             };
             updateCurrentMap((map) => ({ ...map, nodes: [...map.nodes, newNode] }));
             setPathStartNodeId(newNode.id);
         }
     } else if (activeTool === "select") {
         setSelectedElement(null);
     }
  };

  const handleMouseMove = (e: React.MouseEvent) => {
      const worldPos = screenToWorld(e.clientX, e.clientY);
      setMousePos(worldPos);

      if (isPanning) {
          const dx = e.clientX - lastMousePos.x;
          const dy = e.clientY - lastMousePos.y;
          setView(v => ({ ...v, x: v.x + dx, y: v.y + dy }));
          setLastMousePos({ x: e.clientX, y: e.clientY });
          return;
      }

      if (activeTool === "select" && dragNodeId) {
          const snappedX = snapToGrid(worldPos.x);
          const snappedY = snapToGrid(worldPos.y);
          
          updateCurrentMap((map) => {
            const node = map.nodes.find(n => n.id === dragNodeId);
            if (!node) return map;

            const dx = snappedX - node.x;
            const dy = snappedY - node.y;

            if (dx === 0 && dy === 0) return map;

            return cascadeMoveNode(map, dragNodeId, dx, dy);
          });
      }
  };

  const handleMouseUp = () => {
      if (dragNodeId) {
          const draggedNode = currentMap.nodes.find(n => n.id === dragNodeId);
          if (draggedNode) {
              // Find overlapping node
              const targetNode = currentMap.nodes.find(n => 
                  n.id !== dragNodeId && 
                  Math.abs(n.x - draggedNode.x) < 1 && 
                  Math.abs(n.y - draggedNode.y) < 1
              );

              if (targetNode) {
                  // Merge draggedNode INTO targetNode
                  updateCurrentMap(map => {
                      // 1. Redirect paths
                      const redirectedPaths = map.paths.map(p => {
                          let newSource = p.sourceId;
                          let newTarget = p.targetId;
                          if (newSource === dragNodeId) newSource = targetNode.id;
                          if (newTarget === dragNodeId) newTarget = targetNode.id;
                          
                          // Recalculate length for the redirected path
                          let newLength = p.length;
                          const pSourceNode = map.nodes.find(n => n.id === (newSource === targetNode.id ? targetNode.id : newSource));
                          const pTargetNode = map.nodes.find(n => n.id === (newTarget === targetNode.id ? targetNode.id : newTarget));
                          
                          if (pSourceNode && pTargetNode) {
                              newLength = getDistance(pSourceNode, pTargetNode);
                          }

                          return { ...p, sourceId: newSource, targetId: newTarget, length: newLength };
                      });

                      // 2. Remove self-loops and duplicates
                      const uniquePaths: PathType[] = [];
                      const seen = new Set<string>();
                      
                      for (const p of redirectedPaths) {
                          if (p.sourceId === p.targetId) continue; // Skip self-loops

                          const key = `${p.sourceId}-${p.targetId}`;
                          if (!seen.has(key)) {
                              seen.add(key);
                              uniquePaths.push(p);
                          }
                      }
                      
                      // 3. Remove dragged node
                      const newNodes = map.nodes.filter(n => n.id !== dragNodeId);

                      return { ...map, nodes: newNodes, paths: uniquePaths };
                  });
                  
                  if (selectedElement?.id === dragNodeId) {
                      setSelectedElement(null);
                  }
              }
          }
      }

      setIsPanning(false);
      setDragNodeId(null);
  };

  const handleNodeMouseDown = (e: React.MouseEvent, nodeId: string) => {
      e.stopPropagation();
      if (activeTool === 'pan') return;

      if (activeTool === "select") {
          setDragNodeId(nodeId);
          setSelectedElement({ type: "node", id: nodeId });
      } else if (activeTool === "path") {
          if (!pathStartNodeId) {
              setPathStartNodeId(nodeId);
          } else {
              if (pathStartNodeId === nodeId) return;
              const startNode = currentMap.nodes.find(n => n.id === pathStartNodeId);
              const endNode = currentMap.nodes.find(n => n.id === nodeId);
              if (startNode && endNode && isAligned(startNode, endNode)) {
                  const newPath: PathType = {
                      id: generateId(),
                      sourceId: startNode.id,
                      targetId: endNode.id,
                      length: getDistance(startNode, endNode),
                      direction: 'one-way',
                      status: 'active'
                  };
                  updateCurrentMap(map => ({ ...map, paths: [...map.paths, newPath] }));
                  setPathStartNodeId(null);
              } else {
                  alert("Nodes must be aligned to connect.");
              }
          }
      }
  };

  // --- Render Helpers ---

  const renderGhostPath = () => {
    if (activeTool !== "path" || !pathStartNodeId) return null;
    const startNode = currentMap.nodes.find((n) => n.id === pathStartNodeId);
    if (!startNode) return null;

    const dx = Math.abs(mousePos.x - startNode.x);
    const dy = Math.abs(mousePos.y - startNode.y);
    let targetX = dx > dy ? mousePos.x : startNode.x;
    let targetY = dx > dy ? startNode.y : mousePos.y;

    return (
      <line x1={startNode.x} y1={startNode.y} x2={targetX} y2={targetY} 
        stroke="#38bdf8" strokeWidth="2" strokeDasharray="5,5" className="opacity-60" />
    );
  };

  const getPointPosition = (pathId: string, distance: number) => {
      const path = currentMap.paths.find(p => p.id === pathId);
      if (!path) return { x: 0, y: 0 };
      const source = currentMap.nodes.find(n => n.id === path.sourceId);
      const target = currentMap.nodes.find(n => n.id === path.targetId);
      if (!source || !target) return { x: 0, y: 0 };

      const ratio = distance / path.length;
      return {
          x: source.x + (target.x - source.x) * ratio,
          y: source.y + (target.y - source.y) * ratio
      };
  };

  return (
    <div className="flex flex-col h-[calc(100vh-140px)] gap-4">
      {/* Top Toolbar */}
      <div className="flex items-center justify-between glass p-3 rounded-2xl">
        <div className="flex items-center gap-4">
          <div className="flex items-center gap-2 px-2">
            <FolderOpen size={18} className="text-zinc-400" />
            <select 
              value={currentMapId}
              onChange={(e) => setCurrentMapId(e.target.value)}
              className="bg-transparent border-none text-sm font-medium focus:ring-0 text-zinc-200 cursor-pointer"
            >
              {maps.map(m => (
                <option key={m.id} value={m.id} className="bg-zinc-900">{m.name}</option>
              ))}
            </select>
            <input
              value={currentMap.name}
              onChange={(e) => {
                const val = e.target.value;
                setMaps(prev => prev.map(m => m.id === currentMapId ? { ...m, name: val } : m));
              }}
              className="bg-transparent border border-zinc-800 rounded-md px-2 py-1 text-xs text-zinc-200 focus:outline-none focus:border-primary/50 transition-colors"
              placeholder="Map Name"
              style={{ width: 160 }}
            />
            <button onClick={handleCreateMap} className="p-1 hover:bg-white/10 rounded-full transition-colors text-zinc-400 hover:text-white">
                <Plus size={14} />
            </button>
          </div>

          <div className="h-6 w-px bg-white/10" />

          {/* Tools */}
          <div className="flex items-center gap-1 bg-zinc-900/50 p-1 rounded-xl">
            <button onClick={() => setActiveTool("select")} className={`p-2 rounded-lg transition-all ${activeTool === "select" ? "bg-zinc-800 text-primary shadow-lg" : "text-zinc-400 hover:text-white hover:bg-white/5"}`} title="Select & Move (V)">
              <MousePointer2 size={18} />
            </button>
            <button onClick={() => setActiveTool("pan")} className={`p-2 rounded-lg transition-all ${activeTool === "pan" ? "bg-zinc-800 text-primary shadow-lg" : "text-zinc-400 hover:text-white hover:bg-white/5"}`} title="Pan (H)">
              <Hand size={18} />
            </button>
            <button onClick={() => setActiveTool("node")} className={`p-2 rounded-lg transition-all ${activeTool === "node" ? "bg-zinc-800 text-primary shadow-lg" : "text-zinc-400 hover:text-white hover:bg-white/5"}`} title="Add Node (N)">
              <Circle size={18} />
            </button>
            <button onClick={() => setActiveTool("path")} className={`p-2 rounded-lg transition-all ${activeTool === "path" ? "bg-zinc-800 text-primary shadow-lg" : "text-zinc-400 hover:text-white hover:bg-white/5"}`} title="Add Path (P)">
              <Move size={18} />
            </button>
            <div className="w-px h-6 bg-white/10 mx-1" />
            <button onClick={() => setActiveTool("point")} className={`p-2 rounded-lg transition-all ${activeTool === "point" ? "bg-zinc-800 text-primary shadow-lg" : "text-zinc-400 hover:text-white hover:bg-white/5"}`} title="Add Point (O)">
              <Octagon size={18} />
            </button>
            <button onClick={() => setActiveTool("qr")} className={`p-2 rounded-lg transition-all ${activeTool === "qr" ? "bg-zinc-800 text-primary shadow-lg" : "text-zinc-400 hover:text-white hover:bg-white/5"}`} title="Add QR Code (Q)">
              <QrCode size={18} />
            </button>
          </div>
        </div>

        <div className="flex items-center gap-4">
            <div className="flex items-center gap-2 bg-zinc-900/50 p-1 rounded-xl px-2">
                <button onClick={() => setView(v => ({...v, k: Math.max(v.k * 0.9, MIN_ZOOM)}))} className="p-1 hover:text-white text-zinc-400"><ZoomOut size={14} /></button>
                <span className="text-[10px] font-mono w-8 text-center">{Math.round(view.k * 100)}%</span>
                <button onClick={() => setView(v => ({...v, k: Math.min(v.k * 1.1, MAX_ZOOM)}))} className="p-1 hover:text-white text-zinc-400"><ZoomIn size={14} /></button>
                <button onClick={() => setView({ x: 0, y: 0, k: 1 })} className="p-1 hover:text-white text-zinc-400 ml-1" title="Reset View"><Maximize size={14} /></button>
            </div>
            <button onClick={async () => {
                if (currentMap.nodes.length === 0) return;
                const origin = currentMap.nodes[0];
                const nodes = currentMap.nodes.map((n, i) => ({
                  id: i + 1,
                  mapId: 0,
                  name: n.label ?? `N${i + 1}`,
                  x: (n.x - origin.x) / PIXELS_PER_METER,
                  y: (origin.y - n.y) / PIXELS_PER_METER,
                  status: n.status
                }));
                const nodeIndex = new Map(currentMap.nodes.map((n, i) => [n.id, i + 1]));
                const paths = currentMap.paths.map((p) => ({
                  id: 0,
                  mapId: 0,
                  startNodeId: nodeIndex.get(p.sourceId) ?? 1,
                  endNodeId: nodeIndex.get(p.targetId) ?? 1,
                  twoWay: p.direction === "bidirectional",
                  length: p.length / PIXELS_PER_METER,
                  status: p.status,
                  rest: p.rest
                }));
                const pathIndex = new Map(currentMap.paths.map((p, i) => [p.id, i]));
                const points = currentMap.points.map(pt => ({
                  id: 0,
                  mapId: 0,
                  pathId: pathIndex.get(pt.pathId) ?? 0,
                  offset: pt.distance / PIXELS_PER_METER,
                  type: pt.type,
                  name: pt.label ?? "Point"
                }));
                const qrs = currentMap.qrcodes.map(q => ({
                  id: 0,
                  mapId: 0,
                  pathId: pathIndex.get(q.pathId) ?? 0,
                  data: q.data,
                  offsetStart: q.distance / PIXELS_PER_METER
                }));
                const idNum = parseInt(currentMapId);
                const payload = { map: { id: Number.isFinite(idNum) ? idNum : 0, name: currentMap.name }, nodes, paths, points, qrs };
                const res = await fetch(`${API_BASE}/maps/graph`, { method: "POST", headers: { "Content-Type": "application/json" }, credentials: "include", body: JSON.stringify(payload) });
                if (res.ok) {
                  const js = await res.json();
                  console.log("Saved map id:", js.id);
                }
            }} className="glass px-4 py-2 rounded-xl text-xs font-medium text-emerald-400 hover:bg-emerald-500/10 transition-colors flex items-center gap-2">
              <Save size={14} />
              Save Map
            </button>
        </div>
      </div>

      <div className="flex-1 flex gap-4 overflow-hidden">
        {/* Canvas Area */}
        <div 
            ref={canvasRef}
            className={`flex-1 relative rounded-3xl border border-zinc-800 bg-zinc-950 overflow-hidden group ${activeTool === 'pan' || isPanning ? 'cursor-grab active:cursor-grabbing' : 'cursor-crosshair'}`}
            onWheel={handleWheel}
            onMouseDown={handleMouseDown}
            onMouseMove={handleMouseMove}
            onMouseUp={handleMouseUp}
            onMouseLeave={handleMouseUp}
        >
            {/* Transform Container */}
            <div 
                className="absolute origin-top-left will-change-transform"
                style={{ transform: `translate(${view.x}px, ${view.y}px) scale(${view.k})` }}
            >
                {/* Infinite Grid Simulation (approximate) */}
                <div 
                    className="absolute inset-[-5000%] opacity-10 pointer-events-none" 
                    style={{ 
                        backgroundImage: `radial-gradient(circle at 1px 1px, #71717a 1px, transparent 0)`, 
                        backgroundSize: `${GRID_SIZE}px ${GRID_SIZE}px` 
                    }} 
                />

                <svg className="overflow-visible w-[1px] h-[1px]" style={{ overflow: 'visible' }}>
                    <defs>
                        <marker id="arrow-bi" markerWidth="10" markerHeight="7" refX="19" refY="3.5" orient="auto">
                            <polygon points="0 0, 10 3.5, 0 7" fill="#52525b" />
                        </marker>
                        <marker id="arrow-one" markerWidth="10" markerHeight="7" refX="19" refY="3.5" orient="auto">
                            <polygon points="0 0, 10 3.5, 0 7" fill="#38bdf8" />
                        </marker>
                    </defs>
                    
                    {currentMap.paths.map(path => {
                        const source = currentMap.nodes.find(n => n.id === path.sourceId);
                        const target = currentMap.nodes.find(n => n.id === path.targetId);
                        if (!source || !target) return null;
                        
                        const isSelected = selectedElement?.type === "path" && selectedElement.id === path.id;
                        const isMaint = path.status === "maintenance";
                        const isRest = !!path.rest;
                        const isOneWay = path.direction === "one-way";
                        const qrCount = currentMap.qrcodes.filter(q => q.pathId === path.id).length;

                        return (
                            <g 
                                key={path.id} 
                                onMouseDown={(e) => { e.stopPropagation(); }}
                                onClick={(e) => { 
                                    e.stopPropagation(); 
                                    if (activeTool === "node") {
                                        handleSplitPath(path, e);
                                    } else if (activeTool === "point" || activeTool === "qr") {
                                        handleAddPointOnPath(path, e, activeTool);
                                    } else {
                                        setSelectedElement({ type: "path", id: path.id }); 
                                    }
                                }} 
                                className={`pointer-events-auto ${['node', 'point', 'qr'].includes(activeTool) ? "cursor-copy" : "cursor-pointer"}`}
                            >
                                <line x1={source.x} y1={source.y} x2={target.x} y2={target.y} stroke="transparent" strokeWidth="20" />
                                <line 
                                    x1={source.x} y1={source.y} x2={target.x} y2={target.y} 
                                    stroke={isSelected ? "#38bdf8" : isMaint ? "#fbbf24" : isRest ? "#22c55e" : "#52525b"} 
                                    strokeWidth={isSelected ? "4" : "2"}
                                    strokeDasharray={isMaint ? "2,6" : "none"}
                                    strokeLinecap="round"
                                    markerEnd={isOneWay ? "url(#arrow-one)" : "none"}
                                    className="transition-colors duration-200"
                                />
                                {path.direction === 'bidirectional' && !isMaint && !isRest && (
                                     <circle cx={(source.x+target.x)/2} cy={(source.y+target.y)/2} r="2" fill="#52525b" />
                                )}
                                
                                {/* Label */}
                                <g transform={`translate(${(source.x + target.x) / 2}, ${(source.y + target.y) / 2})`}>
                                    <rect x="-24" y="-12" width="48" height="24" rx="6" fill="#18181b" className="opacity-90" stroke={isMaint ? "#fbbf24" : isRest ? "#22c55e" : "#27272a"} />
                                    <text dy=".3em" textAnchor="middle" fill={isSelected ? "#38bdf8" : isMaint ? "#fbbf24" : isRest ? "#22c55e" : "#a1a1aa"} fontSize="10" fontFamily="monospace">
                                        {(path.length / PIXELS_PER_METER).toFixed(1)}m
                                    </text>
                                    {qrCount > 0 && (
                                        <g transform="translate(0, 18)">
                                            <rect x="-14" y="-6" width="28" height="12" rx="4" fill="#5b21b6" className="opacity-90" />
                                            <text dy=".3em" textAnchor="middle" fill="#ddd6fe" fontSize="8" fontFamily="monospace" fontWeight="bold">
                                                QR:{qrCount}
                                            </text>
                                        </g>
                                    )}
                                </g>
                            </g>
                        );
                    })}
                    {renderGhostPath()}
                </svg>

                {currentMap.nodes.map(node => {
                    const isSelected = selectedElement?.type === "node" && selectedElement.id === node.id;
                    const isPathStart = pathStartNodeId === node.id;
                    const isMaint = node.status === "maintenance";

                    return (
                        <div
                            key={node.id}
                            onMouseDown={(e) => handleNodeMouseDown(e, node.id)}
                            className={`absolute w-4 h-4 -ml-2 -mt-2 rounded-full border-2 transition-all duration-200 z-30 
                                ${isSelected ? "bg-primary border-white shadow-[0_0_15px_rgba(139,92,246,0.5)] scale-150" : 
                                  isPathStart ? "bg-accent border-white animate-pulse" :
                                  isMaint ? "bg-zinc-900 border-amber-500 shadow-[0_0_10px_rgba(251,191,36,0.3)]" :
                                  "bg-zinc-900 border-zinc-500 hover:border-zinc-300 hover:scale-125"}`}
                            style={{ left: node.x, top: node.y }}
                        >
                            {isMaint && <div className="absolute -top-3 left-1/2 -translate-x-1/2 text-amber-500"><Construction size={10} /></div>}
                            <div className="absolute top-5 left-1/2 -translate-x-1/2 text-[10px] font-mono text-zinc-500 whitespace-nowrap pointer-events-none select-none">
                                {node.label}
                            </div>
                        </div>
                    );
                })}

                {/* Points */}
                {currentMap.points.map(point => {
                    const pos = getPointPosition(point.pathId, point.distance);
                    const isSelected = selectedElement?.type === "point" && selectedElement.id === point.id;
                    
                    let color = "bg-sky-400"; // Rest
                    let icon = <Coffee size={10} />;
                    if (point.type === 'charging') {
                        color = "bg-emerald-400 shadow-[0_0_10px_rgba(52,211,153,0.5)]";
                        icon = <Zap size={10} />;
                    } else if (point.type === 'drop') {
                        color = "bg-rose-500";
                        icon = <ArrowDownToLine size={10} />;
                    } else if (point.type === 'stop') { // Keep backward compatibility just in case
                        color = "bg-rose-500";
                        icon = <ArrowDownToLine size={10} />;
                    }

                    return (
                        <div
                            key={point.id}
                            onClick={(e) => { e.stopPropagation(); setSelectedElement({ type: 'point', id: point.id }); }}
                            className={`absolute w-5 h-5 -ml-2.5 -mt-2.5 rounded-full flex items-center justify-center text-zinc-950 transition-transform hover:scale-125 cursor-pointer z-20
                                ${color} ${isSelected ? 'ring-2 ring-white scale-125' : ''}`}
                            style={{ left: pos.x, top: pos.y }}
                        >
                            {icon}
                        </div>
                    );
                })}

                {/* QR Codes */}
                {currentMap.qrcodes.map(qr => {
                    const isSelected = selectedElement?.type === "qr" && selectedElement.id === qr.id;
                    const isQRTool = activeTool === 'qr';
                    const isVisible = isSelected || isQRTool;

                    if (!isVisible) return null;

                    const pos = getPointPosition(qr.pathId, qr.distance);

                    return (
                        <div
                            key={qr.id}
                            onClick={(e) => { e.stopPropagation(); setSelectedElement({ type: 'qr', id: qr.id }); }}
                            className={`absolute w-1.5 h-1.5 -ml-0.75 -mt-0.75 bg-zinc-600 rounded-[1px] transition-all duration-200 cursor-pointer z-10 group
                                ${isSelected ? 'bg-violet-500 scale-[2.5] ring-2 ring-white/50 z-40' : 'hover:scale-[2.5] hover:bg-zinc-200 hover:z-40 opacity-60 hover:opacity-100'}`}
                            style={{ left: pos.x, top: pos.y }}
                        >
                            <div className="hidden group-hover:flex absolute -top-5 left-1/2 -translate-x-1/2 items-center justify-center bg-zinc-900 text-zinc-300 text-[8px] px-1.5 py-0.5 rounded border border-zinc-800 whitespace-nowrap shadow-xl pointer-events-none">
                                <QrCode size={8} className="mr-1" />
                                <span className="font-mono">{(qr.distance / PIXELS_PER_METER).toFixed(3)}m</span>
                            </div>
                        </div>
                    );
                })}

            </div>
        </div>

        {/* Right Properties Panel */}
        <div className="w-72 glass rounded-3xl p-5 flex flex-col gap-6 overflow-y-auto">
            <div className="flex items-center gap-2 pb-4 border-b border-white/5">
                <Grid3X3 size={18} className="text-zinc-400" />
                <span className="font-medium text-sm">Properties</span>
            </div>

            {selectedElement ? (
                <div className="flex flex-col gap-6 animate-in slide-in-from-right-2 duration-200">
                    {selectedElement.type === "node" ? (
                        <>
                            <div className="flex items-center justify-between">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Status</span>
                                <div className="flex bg-zinc-900 rounded-lg p-1">
                                    <button 
                                        onClick={() => updateElementStatus('active')}
                                        className={`px-3 py-1 rounded-md text-[10px] font-medium transition-all ${currentMap.nodes.find(n => n.id === selectedElement.id)?.status === 'active' ? 'bg-emerald-500/20 text-emerald-400' : 'text-zinc-500 hover:text-zinc-300'}`}
                                    >
                                        Active
                                    </button>
                                    <button 
                                        onClick={() => updateElementStatus('maintenance')}
                                        className={`px-3 py-1 rounded-md text-[10px] font-medium transition-all ${currentMap.nodes.find(n => n.id === selectedElement.id)?.status === 'maintenance' ? 'bg-amber-500/20 text-amber-400' : 'text-zinc-500 hover:text-zinc-300'}`}
                                    >
                                        Maint
                                    </button>
                                </div>
                            </div>
                            
                            <div className="space-y-3">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Position (Meters)</span>
                                <div className="grid grid-cols-2 gap-2">
                                    <div className="glass px-3 py-2 rounded-lg">
                                        <div className="text-[10px] text-zinc-500 mb-1">X Axis</div>
                                        <input 
                                            type="number"
                                            value={getRelativeCoords(currentMap.nodes.find(n => n.id === selectedElement.id)!).x}
                                            onChange={(e) => handleUpdateNodePosition(selectedElement.id, 'x', Number(e.target.value))}
                                            step="0.1"
                                            className="w-full bg-transparent border-none p-0 text-sm font-mono focus:ring-0 text-zinc-200"
                                        />
                                    </div>
                                    <div className="glass px-3 py-2 rounded-lg">
                                        <div className="text-[10px] text-zinc-500 mb-1">Y Axis</div>
                                        <input 
                                            type="number"
                                            value={getRelativeCoords(currentMap.nodes.find(n => n.id === selectedElement.id)!).y}
                                            onChange={(e) => handleUpdateNodePosition(selectedElement.id, 'y', Number(e.target.value))}
                                            step="0.1"
                                            className="w-full bg-transparent border-none p-0 text-sm font-mono focus:ring-0 text-zinc-200"
                                        />
                                    </div>
                                </div>
                                <p className="text-[10px] text-zinc-500">Relative to Node 1 (Origin)</p>
                            </div>

                            <div className="space-y-2">
                                <label className="text-xs text-zinc-500 uppercase tracking-wider">Label</label>
                                <input 
                                    type="text" 
                                    value={currentMap.nodes.find(n => n.id === selectedElement.id)?.label || ""}
                                    onChange={(e) => updateCurrentMap(map => ({
                                        ...map,
                                        nodes: map.nodes.map(n => n.id === selectedElement.id ? { ...n, label: e.target.value } : n)
                                    }))}
                                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-sm text-zinc-200 focus:outline-none focus:border-primary/50 transition-colors"
                                />
                            </div>
                        </>
                    ) : selectedElement.type === "path" ? (
                        <>
                            <div className="flex items-center justify-between">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Status</span>
                                <div className="flex bg-zinc-900 rounded-lg p-1">
                                    <button 
                                        onClick={() => updateElementStatus('active')}
                                        className={`px-3 py-1 rounded-md text-[10px] font-medium transition-all ${currentMap.paths.find(p => p.id === selectedElement.id)?.status === 'active' ? 'bg-emerald-500/20 text-emerald-400' : 'text-zinc-500 hover:text-zinc-300'}`}
                                    >
                                        Active
                                    </button>
                                    <button 
                                        onClick={() => updateElementStatus('maintenance')}
                                        className={`px-3 py-1 rounded-md text-[10px] font-medium transition-all ${currentMap.paths.find(p => p.id === selectedElement.id)?.status === 'maintenance' ? 'bg-amber-500/20 text-amber-400' : 'text-zinc-500 hover:text-zinc-300'}`}
                                    >
                                        Maint
                                    </button>
                                </div>
                            </div>

                            <div className="space-y-3">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Direction</span>
                                <div className="flex flex-col gap-2">
                                    <button 
                                        onClick={() => updatePathDirection('bidirectional')}
                                        className={`flex items-center gap-3 px-3 py-2 rounded-lg border transition-all ${currentMap.paths.find(p => p.id === selectedElement.id)?.direction === 'bidirectional' ? 'bg-primary/10 border-primary/50 text-white' : 'border-zinc-800 text-zinc-400 hover:bg-white/5'}`}
                                    >
                                        <ArrowRightLeft size={16} />
                                        <span className="text-xs font-medium">Two-Way (Bidirectional)</span>
                                    </button>
                                    <button 
                                        onClick={() => updatePathDirection('one-way')}
                                        className={`flex items-center gap-3 px-3 py-2 rounded-lg border transition-all ${currentMap.paths.find(p => p.id === selectedElement.id)?.direction === 'one-way' ? 'bg-primary/10 border-primary/50 text-white' : 'border-zinc-800 text-zinc-400 hover:bg-white/5'}`}
                                    >
                                        <ArrowRight size={16} />
                                        <span className="text-xs font-medium">One-Way (Source → Target)</span>
                                    </button>
                                    {currentMap.paths.find(p => p.id === selectedElement.id)?.direction === 'one-way' && (
                                        <button
                                            onClick={() => {
                                                const pid = selectedElement.id;
                                                updateCurrentMap(map => {
                                                    const path = map.paths.find(p => p.id === pid);
                                                    if (!path) return map;
                                                    const newSource = path.targetId;
                                                    const newTarget = path.sourceId;
                                                    const sourceNode = map.nodes.find(n => n.id === newSource);
                                                    const targetNode = map.nodes.find(n => n.id === newTarget);
                                                    const newLength = (sourceNode && targetNode) ? getDistance(sourceNode, targetNode) : path.length;
                                                    const adjPoints = map.points.map(pt => pt.pathId === pid ? { ...pt, distance: (newLength - pt.distance) } : pt);
                                                    const adjQrs = map.qrcodes.map(q => q.pathId === pid ? { ...q, distance: (newLength - q.distance) } : q);
                                                    return {
                                                        ...map,
                                                        paths: map.paths.map(p => p.id === pid ? { ...p, sourceId: newSource, targetId: newTarget, length: newLength } : p),
                                                        points: adjPoints,
                                                        qrcodes: adjQrs
                                                    };
                                                });
                                            }}
                                            className="flex items-center gap-3 px-3 py-2 rounded-lg border transition-all border-zinc-800 text-zinc-400 hover:text-white hover:bg-white/5"
                                        >
                                            <ArrowRight size={16} />
                                            <span className="text-xs font-medium">Reverse Orientation</span>
                                        </button>
                                    )}
                                </div>
                            </div>

                            <div className="space-y-3">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Rest Path</span>
                                <div className="flex items-center gap-2">
                                    <button
                                        onClick={() => {
                                            const pid = selectedElement.id;
                                            updateCurrentMap(map => ({
                                                ...map,
                                                paths: map.paths.map(p => p.id === pid ? { ...p, rest: !p.rest } : p)
                                            }));
                                        }}
                                        className={`px-3 py-1 rounded-md text-[10px] font-medium transition-all ${
                                            currentMap.paths.find(p => p.id === selectedElement.id)?.rest ? 'bg-sky-500/20 text-sky-400' : 'text-zinc-500 hover:text-zinc-300'
                                        }`}
                                    >
                                        {currentMap.paths.find(p => p.id === selectedElement.id)?.rest ? 'Rest: On' : 'Rest: Off'}
                                    </button>
                                </div>
                            </div>

                            <div className="space-y-2">
                                <label className="text-xs text-zinc-500 uppercase tracking-wider">Length (Meters)</label>
                                <div className="flex items-center gap-2">
                                    <input 
                                        type="number" 
                                        value={(currentMap.paths.find(p => p.id === selectedElement.id)?.length || 0) / PIXELS_PER_METER}
                                        onChange={(e) => handleUpdatePathLength(selectedElement.id, Number(e.target.value))}
                                        step="0.1"
                                        className="flex-1 bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-sm text-zinc-200 focus:outline-none focus:border-primary/50 transition-colors font-mono"
                                    />
                                    <div className="text-xs text-zinc-600">m</div>
                                </div>
                                <p className="text-[10px] text-zinc-500 leading-relaxed">
                                    1m = {PIXELS_PER_METER}px. Adjusting length moves the target node.
                                </p>
                            </div>

                            <div className="h-px bg-white/5 my-2" />
                            
                            <div className="space-y-3">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Batch Add QR Codes</span>
                                <div className="grid grid-cols-2 gap-2">
                                    <div className="glass px-3 py-2 rounded-lg">
                                        <div className="text-[10px] text-zinc-500 mb-1">Interval (m)</div>
                                        <input 
                                            type="number"
                                            value={qrBatchConfig.interval}
                                            onChange={(e) => setQrBatchConfig(c => ({ ...c, interval: Number(e.target.value) }))}
                                            step="0.5"
                                            min="0.1"
                                            className="w-full bg-transparent border-none p-0 text-sm font-mono focus:ring-0 text-zinc-200"
                                        />
                                    </div>
                                    <div className="glass px-3 py-2 rounded-lg">
                                        <div className="text-[10px] text-zinc-500 mb-1">Start Offset (m)</div>
                                        <input 
                                            type="number"
                                            value={qrBatchConfig.startOffset}
                                            onChange={(e) => setQrBatchConfig(c => ({ ...c, startOffset: Number(e.target.value) }))}
                                            step="0.1"
                                            min="0"
                                            className="w-full bg-transparent border-none p-0 text-sm font-mono focus:ring-0 text-zinc-200"
                                        />
                                    </div>
                                </div>
                                <button 
                                    onClick={() => handleBatchGenerateQRs(selectedElement.id)}
                                    className="w-full py-2 bg-violet-500/10 hover:bg-violet-500/20 text-violet-400 border border-violet-500/20 rounded-lg text-xs font-medium transition-colors flex items-center justify-center gap-2"
                                >
                                    <QrCode size={14} />
                                    Generate QRs
                                </button>
                            </div>

                            <div className="h-px bg-white/5 my-2" />

                            <div className="space-y-2">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Existing QR Codes</span>
                                <div className="flex flex-col gap-2 max-h-40 overflow-y-auto pr-1 custom-scrollbar">
                                    {currentMap.qrcodes
                                        .filter(q => q.pathId === selectedElement.id)
                                        .sort((a, b) => a.distance - b.distance)
                                        .map(qr => (
                                        <div key={qr.id} className="flex items-center justify-between glass p-2 rounded-lg group hover:bg-white/5 transition-colors">
                                            <div className="flex flex-col min-w-0 gap-0.5">
                                                <div className="flex items-center gap-2">
                                                    <span className="text-[10px] text-zinc-400 font-mono bg-zinc-900 px-1 rounded">{(qr.distance / PIXELS_PER_METER).toFixed(1)}m</span>
                                                </div>
                                                <input 
                                                    className="text-xs text-zinc-300 bg-transparent border-none p-0 focus:ring-0 w-full truncate placeholder:text-zinc-700"
                                                    value={qr.data}
                                                    onChange={(e) => updateCurrentMap(m => ({ ...m, qrcodes: m.qrcodes.map(q => q.id === qr.id ? { ...q, data: e.target.value } : q) }))}
                                                    placeholder="QR Data"
                                                />
                                            </div>
                                            <button 
                                                onClick={(e) => {
                                                    e.stopPropagation();
                                                    updateCurrentMap(m => ({ ...m, qrcodes: m.qrcodes.filter(q => q.id !== qr.id) }));
                                                }}
                                                className="p-1.5 text-zinc-500 hover:text-red-400 hover:bg-red-500/10 rounded-md opacity-0 group-hover:opacity-100 transition-all"
                                            >
                                                <Trash2 size={12} />
                                            </button>
                                        </div>
                                    ))}
                                    {currentMap.qrcodes.filter(q => q.pathId === selectedElement.id).length === 0 && (
                                        <div className="text-[10px] text-zinc-600 italic text-center py-4 border border-dashed border-zinc-800 rounded-lg">
                                            No QR codes on this path
                                        </div>
                                    )}
                                </div>
                            </div>
                        </>
                    ) : selectedElement.type === "point" ? (
                        <>
                            <div className="flex items-center justify-between">
                                <span className="text-xs text-zinc-500 uppercase tracking-wider">Point Type</span>
                            </div>
                            <div className="grid grid-cols-3 gap-2">
                                <button onClick={() => updateCurrentMap(m => ({ ...m, points: m.points.map(p => p.id === selectedElement.id ? { ...p, type: 'charging' } : p) }))} className={`p-2 rounded-lg border flex flex-col items-center gap-1 ${currentMap.points.find(p => p.id === selectedElement.id)?.type === 'charging' ? 'border-emerald-500 bg-emerald-500/10 text-emerald-400' : 'border-zinc-800 text-zinc-500 hover:text-zinc-300'}`}>
                                    <Zap size={16} />
                                    <span className="text-[10px]">Charge</span>
                                </button>
                                <button onClick={() => updateCurrentMap(m => ({ ...m, points: m.points.map(p => p.id === selectedElement.id ? { ...p, type: 'drop' } : p) }))} className={`p-2 rounded-lg border flex flex-col items-center gap-1 ${currentMap.points.find(p => p.id === selectedElement.id)?.type === 'drop' ? 'border-rose-500 bg-rose-500/10 text-rose-400' : 'border-zinc-800 text-zinc-500 hover:text-zinc-300'}`}>
                                    <ArrowDownToLine size={16} />
                                    <span className="text-[10px]">Drop</span>
                                </button>
                                <button onClick={() => updateCurrentMap(m => ({ ...m, points: m.points.map(p => p.id === selectedElement.id ? { ...p, type: 'rest' } : p) }))} className={`p-2 rounded-lg border flex flex-col items-center gap-1 ${currentMap.points.find(p => p.id === selectedElement.id)?.type === 'rest' ? 'border-sky-400 bg-sky-400/10 text-sky-400' : 'border-zinc-800 text-zinc-500 hover:text-zinc-300'}`}>
                                    <Coffee size={16} />
                                    <span className="text-[10px]">Rest</span>
                                </button>
                            </div>

                            <div className="space-y-2">
                                <label className="text-xs text-zinc-500 uppercase tracking-wider">Distance from Source (m)</label>
                                <input 
                                    type="number" 
                                    value={(currentMap.points.find(p => p.id === selectedElement.id)?.distance || 0) / PIXELS_PER_METER}
                                    onChange={(e) => {
                                        const val = Number(e.target.value) * PIXELS_PER_METER;
                                        const pt = currentMap.points.find(p => p.id === selectedElement.id);
                                        const path = currentMap.paths.find(path => path.id === pt?.pathId);
                                        if (path && val >= 0 && val <= path.length) {
                                            updateCurrentMap(m => ({ ...m, points: m.points.map(p => p.id === selectedElement.id ? { ...p, distance: val } : p) }));
                                        }
                                    }}
                                    step="0.1"
                                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-sm text-zinc-200 focus:outline-none focus:border-primary/50 transition-colors font-mono"
                                />
                            </div>
                        </>
                    ) : (
                        <>
                            <div className="flex items-center gap-2 mb-2">
                                <QrCode size={16} className="text-violet-500" />
                                <span className="text-sm font-medium">QR Code Marker</span>
                            </div>
                            <div className="space-y-2">
                                <label className="text-xs text-zinc-500 uppercase tracking-wider">QR Data</label>
                                <input 
                                    type="text" 
                                    value={currentMap.qrcodes.find(q => q.id === selectedElement.id)?.data || ""}
                                    onChange={(e) => updateCurrentMap(m => ({ ...m, qrcodes: m.qrcodes.map(q => q.id === selectedElement.id ? { ...q, data: e.target.value } : q) }))}
                                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-sm text-zinc-200 focus:outline-none focus:border-primary/50 transition-colors"
                                />
                            </div>
                            <div className="space-y-2">
                                <label className="text-xs text-zinc-500 uppercase tracking-wider">Distance from Source (m)</label>
                                <input 
                                    type="number" 
                                    value={(currentMap.qrcodes.find(q => q.id === selectedElement.id)?.distance || 0) / PIXELS_PER_METER}
                                    onChange={(e) => {
                                        const val = Number(e.target.value) * PIXELS_PER_METER;
                                        const qr = currentMap.qrcodes.find(q => q.id === selectedElement.id);
                                        const path = currentMap.paths.find(path => path.id === qr?.pathId);
                                        if (path && val >= 0 && val <= path.length) {
                                            updateCurrentMap(m => ({ ...m, qrcodes: m.qrcodes.map(q => q.id === selectedElement.id ? { ...q, distance: val } : q) }));
                                        }
                                    }}
                                    step="0.1"
                                    className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-sm text-zinc-200 focus:outline-none focus:border-primary/50 transition-colors font-mono"
                                />
                            </div>
                        </>
                    )}

                    <div className="h-px bg-white/5 my-2" />

                    <button 
                        onClick={handleDeleteElement}
                        className="flex items-center justify-center gap-2 w-full py-2.5 rounded-xl border border-red-500/20 text-red-400 hover:bg-red-500/10 transition-colors text-xs font-medium"
                    >
                        <Trash2 size={14} />
                        Delete {selectedElement.type.charAt(0).toUpperCase() + selectedElement.type.slice(1)}
                    </button>
                </div>
            ) : (
                <div className="flex flex-col items-center justify-center h-40 text-zinc-600 gap-2">
                    <MousePointer2 size={24} className="opacity-20" />
                    <span className="text-xs text-center">Select an element<br/>to edit properties</span>
                </div>
            )}
        </div>
      </div>
    </div>
  );
}
