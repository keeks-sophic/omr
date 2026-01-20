"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams } from "next/navigation";
import { getApiBaseUrl } from "../../../lib/config";
import { getRobot, getRobotSession, commandMode, commandGrip, commandHoist, commandTelescope, commandCamToggle, commandRotate } from "../../../lib/robotApi";
import { pauseTask, resumeTask, cancelTask } from "../../../lib/tasksApi";
import { useSignalR } from "../../../hooks/useSignalR";

export default function RobotDetailPage() {
  const params = useParams();
  const robotId = String(params?.robotId || "");
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const { connection, isConnected } = useSignalR();
  const [robot, setRobot] = useState<any | null>(null);
  const [session, setSession] = useState<any | null>(null);
  const [status, setStatus] = useState<string | null>(null);
  const [taskId, setTaskId] = useState("");
  const [mode, setMode] = useState<"IDLE" | "MANUAL" | "PAUSED">("IDLE");
  const [gripOpen, setGripOpen] = useState(false);
  const [hoistHeight, setHoistHeight] = useState<number>(0);
  const [telescopeExt, setTelescopeExt] = useState<number>(0);
  const [camSide, setCamSide] = useState<"left" | "right" | "center">("center");
  const [rotateDeg, setRotateDeg] = useState<number>(0);

  useEffect(() => {
    if (!robotId) return;
    getRobot(baseUrl, robotId).then(setRobot).catch(() => {});
    getRobotSession(baseUrl, robotId).then((s) => {
      setSession(s);
      if (s?.state?.mode) setMode(s.state.mode);
    }).catch(() => {});
  }, [baseUrl, robotId]);

  useEffect(() => {
    if (!connection || !robotId) return;
    const groupStateTopic = "robot.state.snapshot";
    connection.on(groupStateTopic, (payload: any) => {
      if (String(payload?.robotId || payload?.id) === robotId) {
        setRobot((r: any) => ({ ...(r || {}), state: payload?.state || r?.state, battery: payload?.battery ?? r?.battery }));
      }
    });
    connection.on("robot.telemetry.snapshot", (payload: any) => {
      if (String(payload?.robotId || payload?.id) === robotId) {
        setRobot((r: any) => ({ ...(r || {}), telemetry: payload }));
      }
    });
    connection.on("robot.session.updated", (payload: any) => {
      if (String(payload?.robotId || payload?.id) === robotId) {
        setSession(payload);
      }
    });
    connection.on("robot.log.event", () => {});
    connection.on("robot.config.updated", () => {});
    return () => {
      connection.off(groupStateTopic);
      connection.off("robot.telemetry.snapshot");
      connection.off("robot.session.updated");
      connection.off("robot.log.event");
      connection.off("robot.config.updated");
    };
  }, [connection, robotId]);

  async function submitMode() {
    setStatus("Setting mode...");
    try {
      await commandMode(baseUrl, robotId, mode);
      setStatus("Mode updated");
    } catch {
      setStatus("Failed to set mode");
    }
  }

  async function submitGrip() {
    setStatus("Sending grip...");
    try {
      await commandGrip(baseUrl, robotId, { open: gripOpen });
      setStatus("Grip sent");
    } catch {
      setStatus("Failed to send grip");
    }
  }

  async function submitHoist() {
    setStatus("Sending hoist...");
    try {
      await commandHoist(baseUrl, robotId, { height: hoistHeight });
      setStatus("Hoist sent");
    } catch {
      setStatus("Failed to send hoist");
    }
  }

  async function submitTelescope() {
    setStatus("Sending telescope...");
    try {
      await commandTelescope(baseUrl, robotId, { extension: telescopeExt });
      setStatus("Telescope sent");
    } catch {
      setStatus("Failed to send telescope");
    }
  }

  async function submitCamToggle() {
    setStatus("Sending cam toggle...");
    try {
      await commandCamToggle(baseUrl, robotId, { side: camSide });
      setStatus("Cam toggle sent");
    } catch {
      setStatus("Failed to send cam toggle");
    }
  }

  async function submitRotate() {
    setStatus("Sending rotate...");
    try {
      await commandRotate(baseUrl, robotId, { degrees: rotateDeg });
      setStatus("Rotate sent");
    } catch {
      setStatus("Failed to send rotate");
    }
  }

  async function submitPause() {
    if (!taskId) return;
    setStatus("Pausing task...");
    try {
      await pauseTask(baseUrl, taskId);
      setStatus("Task paused");
    } catch {
      setStatus("Failed to pause task");
    }
  }

  async function submitResume() {
    if (!taskId) return;
    setStatus("Resuming task...");
    try {
      await resumeTask(baseUrl, taskId);
      setStatus("Task resumed");
    } catch {
      setStatus("Failed to resume task");
    }
  }

  async function submitCancel() {
    if (!taskId) return;
    setStatus("Canceling task...");
    try {
      await cancelTask(baseUrl, taskId);
      setStatus("Task canceled");
    } catch {
      setStatus("Failed to cancel task");
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Robot Detail</h1>
          <p style={{ color: "#a1a1aa" }}>{robotId}</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(2, 1fr)" }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>State</h2>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8 }}>
            mode={session?.state?.mode || "unknown"} battery={robot?.battery ?? "-"}
          </div>
        </div>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Session</h2>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8 }}>
            connected={String(session?.connected ?? true)} lastSeen={session?.lastSeen || "-"}
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Mode</h2>
        <div style={{ display: "flex", gap: 12 }}>
          <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
            <input type="radio" checked={mode === "IDLE"} onChange={() => setMode("IDLE")} />
            IDLE
          </label>
          <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
            <input type="radio" checked={mode === "MANUAL"} onChange={() => setMode("MANUAL")} />
            MANUAL
          </label>
          <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
            <input type="radio" checked={mode === "PAUSED"} onChange={() => setMode("PAUSED")} />
            PAUSED
          </label>
        </div>
        <button onClick={submitMode} style={buttonStyle}>Set Mode</button>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Actuators</h2>
        <div style={{ display: "grid", gap: 12, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Grip</label>
            <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
              <input type="checkbox" checked={gripOpen} onChange={(e) => setGripOpen(e.target.checked)} />
              open
            </label>
            <button onClick={submitGrip} style={buttonStyle}>Send Grip</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Hoist</label>
            <input type="number" value={hoistHeight} onChange={(e) => setHoistHeight(Number(e.target.value))} style={inputStyle} />
            <button onClick={submitHoist} style={buttonStyle}>Send Hoist</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Telescope</label>
            <input type="number" value={telescopeExt} onChange={(e) => setTelescopeExt(Number(e.target.value))} style={inputStyle} />
            <button onClick={submitTelescope} style={buttonStyle}>Send Telescope</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Cam Toggle</label>
            <select value={camSide} onChange={(e) => setCamSide(e.target.value as any)} style={inputStyle}>
              <option value="left">left</option>
              <option value="right">right</option>
              <option value="center">center</option>
            </select>
            <button onClick={submitCamToggle} style={buttonStyle}>Send Cam Toggle</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Rotate</label>
            <input type="number" value={rotateDeg} onChange={(e) => setRotateDeg(Number(e.target.value))} style={inputStyle} />
            <button onClick={submitRotate} style={buttonStyle}>Send Rotate</button>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Safety Controls</h2>
        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Task ID</label>
            <input value={taskId} onChange={(e) => setTaskId(e.target.value)} placeholder="taskId" style={inputStyle} />
            <div style={{ display: "flex", gap: 8 }}>
              <button onClick={submitPause} disabled={!taskId} style={buttonStyle}>Pause</button>
              <button onClick={submitResume} disabled={!taskId} style={buttonStyle}>Resume</button>
              <button onClick={submitCancel} disabled={!taskId} style={buttonStyle}>Cancel</button>
            </div>
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
  width: 140,
};
