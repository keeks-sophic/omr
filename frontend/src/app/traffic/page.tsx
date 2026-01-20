"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { useSignalR } from "../../hooks/useSignalR";
import { fetchTrafficConflicts, createTrafficHold, deleteTrafficHold } from "../../lib/trafficApi";

export default function TrafficPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const { connection, isConnected } = useSignalR();
  const [overview, setOverview] = useState<any | null>(null);
  const [scheduleSummary, setScheduleSummary] = useState<any | null>(null);
  const [routeUpdate, setRouteUpdate] = useState<any | null>(null);
  const [conflicts, setConflicts] = useState<any[]>([]);
  const [holdNodeId, setHoldNodeId] = useState("");
  const [holdPathId, setHoldPathId] = useState("");
  const [holdDurationMs, setHoldDurationMs] = useState(60000);
  const [holdReason, setHoldReason] = useState("");
  const [deleteHoldId, setDeleteHoldId] = useState("");
  const [status, setStatus] = useState<string | null>(null);

  useEffect(() => {
    if (!connection) return;
    connection.on("traffic.overview.snapshot", (payload: any) => {
      setOverview(payload);
    });
    connection.on("traffic.overview.updated", (payload: any) => {
      setOverview(payload);
    });
    connection.on("traffic.schedule.summary.updated", (payload: any) => {
      setScheduleSummary(payload);
    });
    connection.on("route.updated", (payload: any) => {
      setRouteUpdate(payload);
    });
    return () => {
      connection.off("traffic.overview.snapshot");
      connection.off("traffic.overview.updated");
      connection.off("traffic.schedule.summary.updated");
      connection.off("route.updated");
    };
  }, [connection]);

  async function loadConflicts() {
    setStatus("Loading conflicts...");
    try {
      const data = await fetchTrafficConflicts(baseUrl);
      setConflicts(Array.isArray(data) ? data : []);
      setStatus("Conflicts loaded");
    } catch {
      setStatus("Failed to load conflicts");
    }
  }

  async function createHold() {
    if (!holdNodeId && !holdPathId) return;
    setStatus("Creating hold...");
    try {
      await createTrafficHold(baseUrl, {
        nodeId: holdNodeId || undefined,
        pathId: holdPathId || undefined,
        durationMs: holdDurationMs,
        reason: holdReason || undefined,
      });
      setStatus("Hold created");
    } catch {
      setStatus("Failed to create hold");
    }
  }

  async function removeHold() {
    if (!deleteHoldId) return;
    setStatus("Removing hold...");
    try {
      await deleteTrafficHold(baseUrl, deleteHoldId);
      setStatus("Hold removed");
    } catch {
      setStatus("Failed to remove hold");
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Traffic</h1>
          <p style={{ color: "#a1a1aa" }}>Live overlays, holds, conflicts, and schedules</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Overview</h2>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8, overflowX: "auto" }}>
            {overview ? JSON.stringify(overview, null, 2) : "No overview"}
          </div>
        </div>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Schedule Summary</h2>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8, overflowX: "auto" }}>
            {scheduleSummary ? JSON.stringify(scheduleSummary, null, 2) : "No summary"}
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Conflicts</h2>
          <button onClick={loadConflicts} style={buttonStyle}>Load Conflicts</button>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8, overflowX: "auto" }}>
            {conflicts.length ? JSON.stringify(conflicts, null, 2) : "No conflicts"}
          </div>
        </div>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Route Updates</h2>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8, overflowX: "auto" }}>
            {routeUpdate ? JSON.stringify(routeUpdate, null, 2) : "No route updates"}
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Apply Hold</h2>
          <label style={{ color: "white" }}>nodeId</label>
          <input value={holdNodeId} onChange={(e) => setHoldNodeId(e.target.value)} style={inputStyle} />
          <label style={{ color: "white" }}>pathId</label>
          <input value={holdPathId} onChange={(e) => setHoldPathId(e.target.value)} style={inputStyle} />
          <label style={{ color: "white" }}>durationMs</label>
          <input type="number" value={holdDurationMs} onChange={(e) => setHoldDurationMs(Number(e.target.value))} style={inputStyle} />
          <label style={{ color: "white" }}>reason</label>
          <input value={holdReason} onChange={(e) => setHoldReason(e.target.value)} style={inputStyle} />
          <button onClick={createHold} style={buttonStyle}>Create Hold</button>
        </div>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Remove Hold</h2>
          <label style={{ color: "white" }}>holdId</label>
          <input value={deleteHoldId} onChange={(e) => setDeleteHoldId(e.target.value)} style={inputStyle} />
          <button onClick={removeHold} style={buttonStyle}>Remove Hold</button>
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
  width: 160,
};
