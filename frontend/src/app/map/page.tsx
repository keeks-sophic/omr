"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import {
  createMap,
  cloneMap,
  publishMap,
  createNode,
  updateNode,
  setNodeMaintenance,
  createPath,
  updatePath,
  setPathMaintenance,
  setPathRest,
  createPoint,
  updatePoint,
  createQr,
  updateQr,
} from "../../lib/mapApi";
import { useSignalR } from "../../hooks/useSignalR";

export default function MapPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const [mapVersionId, setMapVersionId] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const { connection, isConnected } = useSignalR();

  useEffect(() => {
    if (!connection) return;
    connection.on("map.version.created", (payload: any) => {
      setStatus(`Map version created: ${payload?.mapVersionId || "new"}`);
    });
    connection.on("map.version.published", (payload: any) => {
      setStatus(`Map version published: ${payload?.mapVersionId || mapVersionId}`);
    });
    connection.on("map.entity.updated", (payload: any) => {
      const entity = payload?.entityType || "entity";
      setStatus(`Map ${entity} updated`);
    });
    return () => {
      connection.off("map.version.created");
      connection.off("map.version.published");
      connection.off("map.entity.updated");
    };
  }, [connection, mapVersionId]);

  async function handleCreateMap() {
    setStatus("Creating map version...");
    try {
      const res = await createMap(baseUrl, { name: "New Map Version" });
      const id = res?.mapVersionId || res?.id || "";
      setMapVersionId(String(id));
      setStatus(`Map version created: ${id}`);
    } catch {
      setStatus("Failed to create map version");
    }
  }

  async function handleCloneMap() {
    if (!mapVersionId) return;
    setStatus("Cloning map version...");
    try {
      const res = await cloneMap(baseUrl, mapVersionId, {});
      const id = res?.mapVersionId || res?.id || "";
      setMapVersionId(String(id));
      setStatus(`Cloned to new version: ${id}`);
    } catch {
      setStatus("Failed to clone map version");
    }
  }

  async function handlePublishMap() {
    if (!mapVersionId) return;
    setStatus("Publishing map version...");
    try {
      await publishMap(baseUrl, mapVersionId, { changeSummary: "Initial publish" });
      setStatus(`Published map version: ${mapVersionId}`);
    } catch {
      setStatus("Failed to publish map version");
    }
  }

  const [nodeCreate, setNodeCreate] = useState({ nodeId: "", x: 0, y: 0, label: "" });
  const [nodeUpdate, setNodeUpdate] = useState({ nodeId: "", x: 0, y: 0, label: "" });
  const [nodeMaintenance, setNodeMaintenanceState] = useState({ nodeId: "", isMaintenance: false });

  async function submitNodeCreate() {
    if (!mapVersionId) return;
    setStatus("Creating node...");
    try {
      await createNode(baseUrl, mapVersionId, { nodeId: nodeCreate.nodeId, geom: { x: nodeCreate.x, y: nodeCreate.y }, label: nodeCreate.label });
      setStatus("Node created");
    } catch {
      setStatus("Failed to create node");
    }
  }

  async function submitNodeUpdate() {
    if (!mapVersionId || !nodeUpdate.nodeId) return;
    setStatus("Updating node...");
    try {
      await updateNode(baseUrl, mapVersionId, nodeUpdate.nodeId, { geom: { x: nodeUpdate.x, y: nodeUpdate.y }, label: nodeUpdate.label });
      setStatus("Node updated");
    } catch {
      setStatus("Failed to update node");
    }
  }

  async function submitNodeMaintenance() {
    if (!mapVersionId || !nodeMaintenance.nodeId) return;
    setStatus("Toggling node maintenance...");
    try {
      await setNodeMaintenance(baseUrl, mapVersionId, nodeMaintenance.nodeId, nodeMaintenance.isMaintenance);
      setStatus("Node maintenance updated");
    } catch {
      setStatus("Failed to update node maintenance");
    }
  }

  const [pathCreate, setPathCreate] = useState({ pathId: "", fromNodeId: "", toNodeId: "", direction: "TWO_WAY", speedLimit: 1.0 });
  const [pathUpdate, setPathUpdate] = useState({ pathId: "", direction: "TWO_WAY", speedLimit: 1.0 });
  const [pathMaintenance, setPathMaintenanceState] = useState({ pathId: "", isMaintenance: false });
  const [pathRest, setPathRestState] = useState({ pathId: "", isRestPath: false, restCapacity: 0, restDwellPolicy: "" });

  async function submitPathCreate() {
    if (!mapVersionId) return;
    setStatus("Creating path...");
    try {
      await createPath(baseUrl, mapVersionId, {
        pathId: pathCreate.pathId,
        fromNodeId: pathCreate.fromNodeId,
        toNodeId: pathCreate.toNodeId,
        direction: pathCreate.direction,
        speedLimit: pathCreate.speedLimit,
      });
      setStatus("Path created");
    } catch {
      setStatus("Failed to create path");
    }
  }

  async function submitPathUpdate() {
    if (!mapVersionId || !pathUpdate.pathId) return;
    setStatus("Updating path...");
    try {
      await updatePath(baseUrl, mapVersionId, pathUpdate.pathId, {
        direction: pathUpdate.direction,
        speedLimit: pathUpdate.speedLimit,
      });
      setStatus("Path updated");
    } catch {
      setStatus("Failed to update path");
    }
  }

  async function submitPathMaintenance() {
    if (!mapVersionId || !pathMaintenance.pathId) return;
    setStatus("Toggling path maintenance...");
    try {
      await setPathMaintenance(baseUrl, mapVersionId, pathMaintenance.pathId, pathMaintenance.isMaintenance);
      setStatus("Path maintenance updated");
    } catch {
      setStatus("Failed to update path maintenance");
    }
  }

  async function submitPathRest() {
    if (!mapVersionId || !pathRest.pathId) return;
    setStatus("Updating path rest options...");
    try {
      await setPathRest(baseUrl, mapVersionId, pathRest.pathId, {
        isRestPath: pathRest.isRestPath,
        restCapacity: pathRest.restCapacity || undefined,
        restDwellPolicy: pathRest.restDwellPolicy || undefined,
      });
      setStatus("Path rest options updated");
    } catch {
      setStatus("Failed to update path rest options");
    }
  }

  const [pointCreate, setPointCreate] = useState({ pointId: "", type: "PICK_DROP", label: "", x: 0, y: 0 });
  const [pointUpdate, setPointUpdate] = useState({ pointId: "", type: "PICK_DROP", label: "", x: 0, y: 0 });

  async function submitPointCreate() {
    if (!mapVersionId) return;
    setStatus("Creating point...");
    try {
      await createPoint(baseUrl, mapVersionId, {
        pointId: pointCreate.pointId,
        type: pointCreate.type,
        label: pointCreate.label,
        geom: { x: pointCreate.x, y: pointCreate.y },
      });
      setStatus("Point created");
    } catch {
      setStatus("Failed to create point");
    }
  }

  async function submitPointUpdate() {
    if (!mapVersionId || !pointUpdate.pointId) return;
    setStatus("Updating point...");
    try {
      await updatePoint(baseUrl, mapVersionId, pointUpdate.pointId, {
        type: pointUpdate.type,
        label: pointUpdate.label,
        geom: { x: pointUpdate.x, y: pointUpdate.y },
      });
      setStatus("Point updated");
    } catch {
      setStatus("Failed to update point");
    }
  }

  const [qrCreate, setQrCreate] = useState({ qrId: "", pathId: "", distance: 0, qrCode: "" });
  const [qrUpdate, setQrUpdate] = useState({ qrId: "", pathId: "", distance: 0, qrCode: "" });

  async function submitQrCreate() {
    if (!mapVersionId) return;
    setStatus("Creating QR anchor...");
    try {
      await createQr(baseUrl, mapVersionId, {
        qrId: qrCreate.qrId,
        pathId: qrCreate.pathId,
        distanceAlongPath: qrCreate.distance,
        qrCode: qrCreate.qrCode,
      });
      setStatus("QR anchor created");
    } catch {
      setStatus("Failed to create QR anchor");
    }
  }

  async function submitQrUpdate() {
    if (!mapVersionId || !qrUpdate.qrId) return;
    setStatus("Updating QR anchor...");
    try {
      await updateQr(baseUrl, mapVersionId, qrUpdate.qrId, {
        pathId: qrUpdate.pathId,
        distanceAlongPath: qrUpdate.distance,
        qrCode: qrUpdate.qrCode,
      });
      setStatus("QR anchor updated");
    } catch {
      setStatus("Failed to update QR anchor");
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Map Management</h1>
          <p style={{ color: "#a1a1aa" }}>Versioned map, nodes, paths, points, QR anchors</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Map Version</h2>
        <div style={{ display: "flex", gap: 12 }}>
          <button onClick={handleCreateMap} style={{ padding: 10, borderRadius: 8, border: "1px solid #22c55e", background: "#22c55e", color: "white" }}>
            Create Version
          </button>
          <input
            value={mapVersionId}
            onChange={(e) => setMapVersionId(e.target.value)}
            placeholder="mapVersionId"
            style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white", minWidth: 220 }}
          />
          <button onClick={handleCloneMap} disabled={!mapVersionId} style={{ padding: 10, borderRadius: 8, border: "1px solid #22c55e", background: "#22c55e", color: "white" }}>
            Clone Version
          </button>
          <button onClick={handlePublishMap} disabled={!mapVersionId} style={{ padding: 10, borderRadius: 8, border: "1px solid #22c55e", background: "#22c55e", color: "white" }}>
            Publish Version
          </button>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Nodes</h2>
        <div style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(3, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Create</label>
            <input placeholder="nodeId" value={nodeCreate.nodeId} onChange={(e) => setNodeCreate((s) => ({ ...s, nodeId: e.target.value }))} style={inputStyle} />
            <input placeholder="x" type="number" value={nodeCreate.x} onChange={(e) => setNodeCreate((s) => ({ ...s, x: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="y" type="number" value={nodeCreate.y} onChange={(e) => setNodeCreate((s) => ({ ...s, y: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="label" value={nodeCreate.label} onChange={(e) => setNodeCreate((s) => ({ ...s, label: e.target.value }))} style={inputStyle} />
            <button onClick={submitNodeCreate} disabled={!mapVersionId} style={buttonStyle}>Create Node</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Update</label>
            <input placeholder="nodeId" value={nodeUpdate.nodeId} onChange={(e) => setNodeUpdate((s) => ({ ...s, nodeId: e.target.value }))} style={inputStyle} />
            <input placeholder="x" type="number" value={nodeUpdate.x} onChange={(e) => setNodeUpdate((s) => ({ ...s, x: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="y" type="number" value={nodeUpdate.y} onChange={(e) => setNodeUpdate((s) => ({ ...s, y: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="label" value={nodeUpdate.label} onChange={(e) => setNodeUpdate((s) => ({ ...s, label: e.target.value }))} style={inputStyle} />
            <button onClick={submitNodeUpdate} disabled={!mapVersionId || !nodeUpdate.nodeId} style={buttonStyle}>Update Node</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Maintenance</label>
            <input placeholder="nodeId" value={nodeMaintenance.nodeId} onChange={(e) => setNodeMaintenanceState((s) => ({ ...s, nodeId: e.target.value }))} style={inputStyle} />
            <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
              <input type="checkbox" checked={nodeMaintenance.isMaintenance} onChange={(e) => setNodeMaintenanceState((s) => ({ ...s, isMaintenance: e.target.checked }))} />
              maintenance
            </label>
            <button onClick={submitNodeMaintenance} disabled={!mapVersionId || !nodeMaintenance.nodeId} style={buttonStyle}>Set Maintenance</button>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Paths</h2>
        <div style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Create</label>
            <input placeholder="pathId" value={pathCreate.pathId} onChange={(e) => setPathCreate((s) => ({ ...s, pathId: e.target.value }))} style={inputStyle} />
            <input placeholder="fromNodeId" value={pathCreate.fromNodeId} onChange={(e) => setPathCreate((s) => ({ ...s, fromNodeId: e.target.value }))} style={inputStyle} />
            <input placeholder="toNodeId" value={pathCreate.toNodeId} onChange={(e) => setPathCreate((s) => ({ ...s, toNodeId: e.target.value }))} style={inputStyle} />
            <select value={pathCreate.direction} onChange={(e) => setPathCreate((s) => ({ ...s, direction: e.target.value }))} style={inputStyle}>
              <option value="ONE_WAY">ONE_WAY</option>
              <option value="TWO_WAY">TWO_WAY</option>
            </select>
            <input placeholder="speedLimit" type="number" step="0.1" value={pathCreate.speedLimit} onChange={(e) => setPathCreate((s) => ({ ...s, speedLimit: Number(e.target.value) }))} style={inputStyle} />
            <button onClick={submitPathCreate} disabled={!mapVersionId} style={buttonStyle}>Create Path</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Update / Maintenance / Rest</label>
            <input placeholder="pathId" value={pathUpdate.pathId} onChange={(e) => { setPathUpdate((s) => ({ ...s, pathId: e.target.value })); setPathMaintenanceState((s) => ({ ...s, pathId: e.target.value })); setPathRestState((s) => ({ ...s, pathId: e.target.value })); }} style={inputStyle} />
            <select value={pathUpdate.direction} onChange={(e) => setPathUpdate((s) => ({ ...s, direction: e.target.value }))} style={inputStyle}>
              <option value="ONE_WAY">ONE_WAY</option>
              <option value="TWO_WAY">TWO_WAY</option>
            </select>
            <input placeholder="speedLimit" type="number" step="0.1" value={pathUpdate.speedLimit} onChange={(e) => setPathUpdate((s) => ({ ...s, speedLimit: Number(e.target.value) }))} style={inputStyle} />
            <button onClick={submitPathUpdate} disabled={!mapVersionId || !pathUpdate.pathId} style={buttonStyle}>Update Path</button>
            <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8, marginTop: 8 }}>
              <input type="checkbox" checked={pathMaintenance.isMaintenance} onChange={(e) => setPathMaintenanceState((s) => ({ ...s, isMaintenance: e.target.checked }))} />
              maintenance
            </label>
            <button onClick={submitPathMaintenance} disabled={!mapVersionId || !pathMaintenance.pathId} style={buttonStyle}>Set Maintenance</button>
            <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8, marginTop: 8 }}>
              <input type="checkbox" checked={pathRest.isRestPath} onChange={(e) => setPathRestState((s) => ({ ...s, isRestPath: e.target.checked }))} />
              isRestPath
            </label>
            <input placeholder="restCapacity" type="number" value={pathRest.restCapacity} onChange={(e) => setPathRestState((s) => ({ ...s, restCapacity: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="restDwellPolicy" value={pathRest.restDwellPolicy} onChange={(e) => setPathRestState((s) => ({ ...s, restDwellPolicy: e.target.value }))} style={inputStyle} />
            <button onClick={submitPathRest} disabled={!mapVersionId || !pathRest.pathId} style={buttonStyle}>Set Rest Options</button>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Points</h2>
        <div style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Create</label>
            <input placeholder="pointId" value={pointCreate.pointId} onChange={(e) => setPointCreate((s) => ({ ...s, pointId: e.target.value }))} style={inputStyle} />
            <select value={pointCreate.type} onChange={(e) => setPointCreate((s) => ({ ...s, type: e.target.value }))} style={inputStyle}>
              <option value="PICK_DROP">PICK_DROP</option>
              <option value="CHARGE">CHARGE</option>
            </select>
            <input placeholder="label" value={pointCreate.label} onChange={(e) => setPointCreate((s) => ({ ...s, label: e.target.value }))} style={inputStyle} />
            <input placeholder="x" type="number" value={pointCreate.x} onChange={(e) => setPointCreate((s) => ({ ...s, x: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="y" type="number" value={pointCreate.y} onChange={(e) => setPointCreate((s) => ({ ...s, y: Number(e.target.value) }))} style={inputStyle} />
            <button onClick={submitPointCreate} disabled={!mapVersionId} style={buttonStyle}>Create Point</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Update</label>
            <input placeholder="pointId" value={pointUpdate.pointId} onChange={(e) => setPointUpdate((s) => ({ ...s, pointId: e.target.value }))} style={inputStyle} />
            <select value={pointUpdate.type} onChange={(e) => setPointUpdate((s) => ({ ...s, type: e.target.value }))} style={inputStyle}>
              <option value="PICK_DROP">PICK_DROP</option>
              <option value="CHARGE">CHARGE</option>
            </select>
            <input placeholder="label" value={pointUpdate.label} onChange={(e) => setPointUpdate((s) => ({ ...s, label: e.target.value }))} style={inputStyle} />
            <input placeholder="x" type="number" value={pointUpdate.x} onChange={(e) => setPointUpdate((s) => ({ ...s, x: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="y" type="number" value={pointUpdate.y} onChange={(e) => setPointUpdate((s) => ({ ...s, y: Number(e.target.value) }))} style={inputStyle} />
            <button onClick={submitPointUpdate} disabled={!mapVersionId || !pointUpdate.pointId} style={buttonStyle}>Update Point</button>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>QR Anchors</h2>
        <div style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Create</label>
            <input placeholder="qrId" value={qrCreate.qrId} onChange={(e) => setQrCreate((s) => ({ ...s, qrId: e.target.value }))} style={inputStyle} />
            <input placeholder="pathId" value={qrCreate.pathId} onChange={(e) => setQrCreate((s) => ({ ...s, pathId: e.target.value }))} style={inputStyle} />
            <input placeholder="distanceAlongPath" type="number" value={qrCreate.distance} onChange={(e) => setQrCreate((s) => ({ ...s, distance: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="qrCode" value={qrCreate.qrCode} onChange={(e) => setQrCreate((s) => ({ ...s, qrCode: e.target.value }))} style={inputStyle} />
            <button onClick={submitQrCreate} disabled={!mapVersionId} style={buttonStyle}>Create QR</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Update</label>
            <input placeholder="qrId" value={qrUpdate.qrId} onChange={(e) => setQrUpdate((s) => ({ ...s, qrId: e.target.value }))} style={inputStyle} />
            <input placeholder="pathId" value={qrUpdate.pathId} onChange={(e) => setQrUpdate((s) => ({ ...s, pathId: e.target.value }))} style={inputStyle} />
            <input placeholder="distanceAlongPath" type="number" value={qrUpdate.distance} onChange={(e) => setQrUpdate((s) => ({ ...s, distance: Number(e.target.value) }))} style={inputStyle} />
            <input placeholder="qrCode" value={qrUpdate.qrCode} onChange={(e) => setQrUpdate((s) => ({ ...s, qrCode: e.target.value }))} style={inputStyle} />
            <button onClick={submitQrUpdate} disabled={!mapVersionId || !qrUpdate.qrId} style={buttonStyle}>Update QR</button>
          </div>
        </div>
      </div>

      {status && (
        <div style={{ padding: 10, borderRadius: 8, border: "1px solid rgba(255,255,255,0.08)", color: "#a1a1aa" }}>
          {status}
        </div>
      )}
    </div>
  );
}

const inputStyle: React.CSSProperties = {
  padding: 10,
  borderRadius: 8,
  border: "1px solid #27272a",
  background: "#0a0a0a",
  color: "white",
};

const buttonStyle: React.CSSProperties = {
  padding: 10,
  borderRadius: 8,
  border: "1px solid #22c55e",
  background: "#22c55e",
  color: "white",
  width: 180,
};
