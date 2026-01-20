"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { createSimSession, startSimSession, stopSimSession, pauseSimSession, resumeSimSession } from "../../lib/simApi";
import { useSignalR } from "../../hooks/useSignalR";
import { Play, Pause, RotateCcw } from "lucide-react";

export default function SimManagerPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const { connection, isConnected } = useSignalR();
  const [mapVersionId, setMapVersionId] = useState("");
  const [robotCount, setRobotCount] = useState(1);
  const [supportsTelescope, setSupportsTelescope] = useState(false);
  const [telescopeEnabled, setTelescopeEnabled] = useState(false);
  const [missionsCsv, setMissionsCsv] = useState("");
  const [speedMultiplier, setSpeedMultiplier] = useState(1);
  const [simSessionId, setSimSessionId] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const [events, setEvents] = useState<any[]>([]);
  const [metrics, setMetrics] = useState<any | null>(null);
  const [isRunning, setIsRunning] = useState(false);

  useEffect(() => {
    if (!connection) return;
    connection.on("sim.session.status", (payload: any) => {
      setStatus(String(payload?.status || ""));
      setIsRunning(payload?.status === "RUNNING");
    });
    connection.on("sim.event", (payload: any) => {
      setEvents((prev) => [payload, ...prev].slice(0, 200));
    });
    connection.on("sim.metrics.updated", (payload: any) => {
      setMetrics(payload);
    });
    return () => {
      connection.off("sim.session.status");
      connection.off("sim.event");
      connection.off("sim.metrics.updated");
    };
  }, [connection]);

  async function handleCreate() {
    if (!mapVersionId || robotCount < 1) return;
    setStatus("Creating simulation session...");
    try {
      const res = await createSimSession(baseUrl, {
        mapVersionId,
        robotCount,
        capabilities: { supportsTelescope },
        featureFlags: { telescopeEnabled },
        missions: missionsCsv ? missionsCsv.split(",").map((s) => s.trim()).filter(Boolean) : [],
        tasks: [],
        speedMultiplier,
      });
      const id = res?.simSessionId || res?.id || "";
      setSimSessionId(String(id));
      setStatus(`Simulation created: ${id}`);
      setEvents([]);
      setMetrics(null);
    } catch {
      setStatus("Failed to create simulation session");
    }
  }

  async function handleStart() {
    if (!simSessionId) return;
    setStatus("Starting simulation...");
    try {
      await startSimSession(baseUrl, simSessionId);
      setStatus("Simulation started");
      setIsRunning(true);
    } catch {
      setStatus("Failed to start simulation");
    }
  }

  async function handleStop() {
    if (!simSessionId) return;
    setStatus("Stopping simulation...");
    try {
      await stopSimSession(baseUrl, simSessionId);
      setStatus("Simulation stopped");
      setIsRunning(false);
    } catch {
      setStatus("Failed to stop simulation");
    }
  }

  async function handlePause() {
    if (!simSessionId) return;
    setStatus("Pausing simulation...");
    try {
      await pauseSimSession(baseUrl, simSessionId);
      setStatus("Simulation paused");
      setIsRunning(false);
    } catch {
      setStatus("Failed to pause simulation");
    }
  }

  async function handleResume() {
    if (!simSessionId) return;
    setStatus("Resuming simulation...");
    try {
      await resumeSimSession(baseUrl, simSessionId);
      setStatus("Simulation resumed");
      setIsRunning(true);
    } catch {
      setStatus("Failed to resume simulation");
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Simulation</h1>
          <p style={{ color: "#a1a1aa" }}>Create sessions; start, pause, resume, stop; view metrics</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Create Session</h2>
        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>mapVersionId</label>
            <input value={mapVersionId} onChange={(e) => setMapVersionId(e.target.value)} style={inputStyle} />
            <label style={{ color: "white" }}>robotCount</label>
            <input type="number" min={1} value={robotCount} onChange={(e) => setRobotCount(Number(e.target.value))} style={inputStyle} />
            <label style={{ color: "white" }}>missions (CSV)</label>
            <input value={missionsCsv} onChange={(e) => setMissionsCsv(e.target.value)} placeholder="mission-1, mission-2" style={inputStyle} />
            <label style={{ color: "white" }}>speedMultiplier</label>
            <input type="number" step="0.1" value={speedMultiplier} onChange={(e) => setSpeedMultiplier(Number(e.target.value))} style={inputStyle} />
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Capabilities</label>
            <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
              <input type="checkbox" checked={supportsTelescope} onChange={(e) => setSupportsTelescope(e.target.checked)} />
              supportsTelescope
            </label>
            <label style={{ color: "white" }}>Feature Flags</label>
            <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
              <input type="checkbox" checked={telescopeEnabled} onChange={(e) => setTelescopeEnabled(e.target.checked)} />
              telescopeEnabled
            </label>
            <label style={{ color: "white" }}>simSessionId</label>
            <input value={simSessionId} onChange={(e) => setSimSessionId(e.target.value)} style={inputStyle} />
            <div style={{ display: "flex", gap: 8 }}>
              <button onClick={handleCreate} disabled={!mapVersionId || robotCount < 1} style={buttonStyle}>Create</button>
            </div>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Controls</h2>
          <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
            <button onClick={handleStart} disabled={!simSessionId} style={iconButtonStyle}><Play size={18} /></button>
            <button onClick={handlePause} disabled={!simSessionId || !isRunning} style={iconButtonStyle}><Pause size={18} /></button>
            <button onClick={handleResume} disabled={!simSessionId || isRunning} style={iconButtonStyle}><Play size={18} /></button>
            <button onClick={handleStop} disabled={!simSessionId} style={iconButtonStyle}><RotateCcw size={18} /></button>
          </div>
          <div style={{ marginTop: 12, fontFamily: "monospace", fontSize: 12, color: "#a1a1aa" }}>
            {status || "Idle"}
          </div>
        </div>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Metrics</h2>
          <div style={{ display: "grid", gap: 6, marginTop: 8, fontFamily: "monospace", fontSize: 12 }}>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "#a1a1aa" }}>throughput</span>
              <span style={{ color: "white" }}>{metrics?.throughput ?? "-"}</span>
            </div>
            <div style={{ display: "flex", justifyContent: "space_between" }}>
              <span style={{ color: "#a1a1aa" }}>avg_wait_time</span>
              <span style={{ color: "white" }}>{metrics?.avg_wait_time ?? "-"}</span>
            </div>
            <div style={{ display: "flex", justifyContent: "space_between" }}>
              <span style={{ color: "#a1a1aa" }}>congestion_penalty</span>
              <span style={{ color: "white" }}>{metrics?.congestion_penalty ?? "-"}</span>
            </div>
            <div style={{ display: "flex", justifyContent: "space_between" }}>
              <span style={{ color: "#a1a1aa" }}>safety_stop_count</span>
              <span style={{ color: "white" }}>{metrics?.safety_stop_count ?? "-"}</span>
            </div>
          </div>
        </div>
      </div>

      <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Events</h2>
        <div style={{ display: "grid", gap: 8 }}>
          {events.slice(0, 100).map((ev, i) => (
            <div key={i} style={{ display: "grid", gap: 4, padding: 10, borderRadius: 8, background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.08)" }}>
              <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa" }}>{ev?.timestamp || ""}</div>
              <div style={{ color: "white", fontFamily: "monospace", fontSize: 12 }}>{ev?.type || "event"}</div>
              <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", overflowX: "auto" }}>
                {typeof ev?.payload === "string" ? ev.payload : JSON.stringify(ev?.payload || ev, null, 2)}
              </div>
            </div>
          ))}
        </div>
      </div>
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

const iconButtonStyle: React.CSSProperties = {
  padding: 10,
  borderRadius: 8,
  border: "1px solid #ffffff1a",
  background: "#0a0a0a",
  color: "white",
  width: 44,
  height: 44,
  display: "flex",
  alignItems: "center",
  justifyContent: "center",
};
