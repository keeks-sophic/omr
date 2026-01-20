"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { getRobots, putFeatureFlags, putMotionLimits, putRuntimeMode } from "../../lib/configApi";
import { useSignalR } from "../../hooks/useSignalR";

type RobotOption = { id: string; label: string };

export default function ConfigPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const [robots, setRobots] = useState<RobotOption[]>([]);
  const [robotId, setRobotId] = useState("");
  const [loadingRobots, setLoadingRobots] = useState(true);
  const [status, setStatus] = useState<string | null>(null);
  const [motionLimits, setMotionLimits] = useState({ maxDriveSpeed: 1.0, maxAcceleration: 0.5, maxDeceleration: 0.5 });
  const [runtimeMode, setRuntimeMode] = useState<"LIVE" | "SIM">("LIVE");
  const [telescopeEnabled, setTelescopeEnabled] = useState(false);
  const { connection, isConnected } = useSignalR();

  useEffect(() => {
    setLoadingRobots(true);
    getRobots(baseUrl)
      .then((data) => {
        const options: RobotOption[] = Array.isArray(data)
          ? data.map((r: any) => {
              const id = r.robotId || r.id || r.name || "";
              const label = r.name || r.robotId || r.id || "robot";
              return { id: String(id), label: String(label) };
            })
          : [];
        setRobots(options);
        if (options.length > 0) setRobotId(options[0].id);
      })
      .catch(() => {})
      .finally(() => setLoadingRobots(false));
  }, [baseUrl]);

  useEffect(() => {
    if (!connection) return;
    connection.on("robot.config.updated", (payload: any) => {
      setStatus(`Config updated for ${payload?.robotId || "robot"}`);
    });
    return () => {
      connection.off("robot.config.updated");
    };
  }, [connection]);

  async function submitMotionLimits() {
    if (!robotId) return;
    setStatus("Updating motion limits...");
    try {
      await putMotionLimits(baseUrl, robotId, motionLimits);
      setStatus("Motion limits updated");
    } catch {
      setStatus("Failed to update motion limits");
    }
  }

  async function submitRuntimeMode() {
    if (!robotId) return;
    setStatus("Updating runtime mode...");
    try {
      await putRuntimeMode(baseUrl, robotId, runtimeMode);
      setStatus("Runtime mode updated");
    } catch {
      setStatus("Failed to update runtime mode");
    }
  }

  async function submitFeatureFlags() {
    if (!robotId) return;
    setStatus("Updating feature flags...");
    try {
      await putFeatureFlags(baseUrl, robotId, { telescopeEnabled });
      setStatus("Feature flags updated");
    } catch {
      setStatus("Failed to update feature flags");
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Config</h1>
          <p style={{ color: "#a1a1aa" }}>Per-robot limits, runtime mode, and features</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gap: 16 }}>
        <div style={{ display: "grid", gap: 8, maxWidth: 420 }}>
          <label style={{ color: "white" }}>Robot</label>
          {loadingRobots ? (
            <div style={{ color: "#a1a1aa" }}>Loading robots...</div>
          ) : robots.length > 0 ? (
            <select
              value={robotId}
              onChange={(e) => setRobotId(e.target.value)}
              style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }}
            >
              {robots.map((r) => (
                <option key={r.id} value={r.id}>
                  {r.label} ({r.id})
                </option>
              ))}
            </select>
          ) : (
            <input
              value={robotId}
              onChange={(e) => setRobotId(e.target.value)}
              placeholder="Enter robotId"
              style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }}
            />
          )}
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Motion Limits</h2>
        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(3, 1fr)", maxWidth: 720 }}>
          <div style={{ display: "grid", gap: 6 }}>
            <label style={{ color: "#a1a1aa", fontSize: 12 }}>Max Drive Speed</label>
            <input
              type="number"
              step="0.1"
              value={motionLimits.maxDriveSpeed}
              onChange={(e) => setMotionLimits((m) => ({ ...m, maxDriveSpeed: Number(e.target.value) }))}
              style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }}
            />
          </div>
          <div style={{ display: "grid", gap: 6 }}>
            <label style={{ color: "#a1a1aa", fontSize: 12 }}>Max Acceleration</label>
            <input
              type="number"
              step="0.1"
              value={motionLimits.maxAcceleration}
              onChange={(e) => setMotionLimits((m) => ({ ...m, maxAcceleration: Number(e.target.value) }))}
              style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }}
            />
          </div>
          <div style={{ display: "grid", gap: 6 }}>
            <label style={{ color: "#a1a1aa", fontSize: 12 }}>Max Deceleration</label>
            <input
              type="number"
              step="0.1"
              value={motionLimits.maxDeceleration}
              onChange={(e) => setMotionLimits((m) => ({ ...m, maxDeceleration: Number(e.target.value) }))}
              style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }}
            />
          </div>
        </div>
        <button
          onClick={submitMotionLimits}
          disabled={!robotId}
          style={{ width: 180, padding: 10, borderRadius: 8, border: "1px solid #22c55e", background: "#22c55e", color: "white" }}
        >
          Update Limits
        </button>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Runtime Mode</h2>
        <div style={{ display: "flex", gap: 12 }}>
          <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
            <input type="radio" checked={runtimeMode === "LIVE"} onChange={() => setRuntimeMode("LIVE")} />
            LIVE
          </label>
          <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
            <input type="radio" checked={runtimeMode === "SIM"} onChange={() => setRuntimeMode("SIM")} />
            SIM
          </label>
        </div>
        <button
          onClick={submitRuntimeMode}
          disabled={!robotId}
          style={{ width: 180, padding: 10, borderRadius: 8, border: "1px solid #22c55e", background: "#22c55e", color: "white" }}
        >
          Update Mode
        </button>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Feature Flags</h2>
        <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
          <input type="checkbox" checked={telescopeEnabled} onChange={(e) => setTelescopeEnabled(e.target.checked)} />
          telescopeEnabled
        </label>
        <button
          onClick={submitFeatureFlags}
          disabled={!robotId}
          style={{ width: 180, padding: 10, borderRadius: 8, border: "1px solid #22c55e", background: "#22c55e", color: "white" }}
        >
          Update Flags
        </button>
      </div>

      {status && (
        <div style={{ padding: 10, borderRadius: 8, border: "1px solid rgba(255,255,255,0.08)", color: "#a1a1aa" }}>
          {status}
        </div>
      )}
    </div>
  );
}
