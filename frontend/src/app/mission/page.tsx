"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { getRobots } from "../../lib/configApi";
import {
  createMission,
  updateMission,
  validateMission,
  createTeachSession,
  startTeachSession,
  stopTeachSession,
  captureTeachStep,
  saveMissionFromTeach,
} from "../../lib/missionApi";
import { createTask } from "../../lib/tasksApi";
import { useSignalR } from "../../hooks/useSignalR";

export default function MissionPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const [robots, setRobots] = useState<{ id: string; label: string }[]>([]);
  const [robotId, setRobotId] = useState("");
  const [mapVersionId, setMapVersionId] = useState("");
  const [teachSessionId, setTeachSessionId] = useState("");
  const [missionId, setMissionId] = useState("");
  const [missionName, setMissionName] = useState("");
  const [missionVersion, setMissionVersion] = useState("v1");
  const [status, setStatus] = useState<string | null>(null);
  const [captureCommandJson, setCaptureCommandJson] = useState('{"action":"ACTUATE","type":"grip","params":{"open":true}}');
  const { connection, isConnected } = useSignalR();

  useEffect(() => {
    getRobots(baseUrl)
      .then((data) => {
        const options: { id: string; label: string }[] = Array.isArray(data)
          ? data.map((r: any) => {
              const id = r.robotId || r.id || r.name || "";
              const label = r.name || r.robotId || r.id || "robot";
              return { id: String(id), label: String(label) };
            })
          : [];
        setRobots(options);
        if (options.length > 0) setRobotId(options[0].id);
      })
      .catch(() => {});
  }, [baseUrl]);

  useEffect(() => {
    if (!connection) return;
    connection.on("teach.session.started", (payload: any) => {
      setStatus(`Teach session started: ${payload?.teachSessionId || teachSessionId}`);
    });
    connection.on("teach.session.stopped", (payload: any) => {
      setStatus(`Teach session stopped: ${payload?.teachSessionId || teachSessionId}`);
    });
    connection.on("teach.step.captured", (payload: any) => {
      setStatus(`Step captured: ${payload?.correlationId || ""}`);
    });
    connection.on("teach.step.updated", () => {
      setStatus("Teach step updated");
    });
    connection.on("mission.created", (payload: any) => {
      setStatus(`Mission created: ${payload?.missionId || missionId}`);
    });
    connection.on("mission.updated", (payload: any) => {
      setStatus(`Mission updated: ${payload?.missionId || missionId}`);
    });
    return () => {
      connection.off("teach.session.started");
      connection.off("teach.session.stopped");
      connection.off("teach.step.captured");
      connection.off("teach.step.updated");
      connection.off("mission.created");
      connection.off("mission.updated");
    };
  }, [connection, teachSessionId, missionId]);

  async function handleCreateTeachSession() {
    if (!robotId || !mapVersionId) return;
    setStatus("Creating teach session...");
    try {
      const res = await createTeachSession(baseUrl, { robotId, mapVersionId });
      const id = res?.teachSessionId || res?.id || "";
      setTeachSessionId(String(id));
      setStatus(`Teach session created: ${id}`);
    } catch {
      setStatus("Failed to create teach session");
    }
  }

  async function handleStartTeach() {
    if (!teachSessionId) return;
    setStatus("Starting teach session...");
    try {
      await startTeachSession(baseUrl, teachSessionId);
      setStatus("Teach session started");
    } catch {
      setStatus("Failed to start teach session");
    }
  }

  async function handleStopTeach() {
    if (!teachSessionId) return;
    setStatus("Stopping teach session...");
    try {
      await stopTeachSession(baseUrl, teachSessionId);
      setStatus("Teach session stopped");
    } catch {
      setStatus("Failed to stop teach session");
    }
  }

  async function handleCaptureStep() {
    if (!teachSessionId) return;
    setStatus("Capturing step...");
    try {
      const command = JSON.parse(captureCommandJson);
      await captureTeachStep(baseUrl, teachSessionId, { command });
      setStatus("Step captured");
    } catch {
      setStatus("Failed to capture step (check JSON)");
    }
  }

  async function handleSaveMission() {
    if (!teachSessionId || !missionId || !missionName || !missionVersion) return;
    setStatus("Saving mission...");
    try {
      await saveMissionFromTeach(baseUrl, teachSessionId, { missionId, name: missionName, version: missionVersion });
      setStatus(`Mission saved: ${missionId}`);
    } catch {
      setStatus("Failed to save mission");
    }
  }

  async function handleCreateMission() {
    if (!missionId || !missionName || !missionVersion) return;
    setStatus("Creating mission (manual)...");
    try {
      const res = await createMission(baseUrl, { missionId, name: missionName, version: missionVersion, steps: [] });
      const id = res?.missionId || missionId;
      setMissionId(String(id));
      setStatus(`Mission created: ${id}`);
    } catch {
      setStatus("Failed to create mission");
    }
  }

  async function handleUpdateMission() {
    if (!missionId) return;
    setStatus("Updating mission (manual)...");
    try {
      await updateMission(baseUrl, missionId, { name: missionName, version: missionVersion });
      setStatus("Mission updated");
    } catch {
      setStatus("Failed to update mission");
    }
  }

  async function handleValidateMission() {
    if (!missionId) return;
    setStatus("Validating mission...");
    try {
      const result = await validateMission(baseUrl, missionId, {});
      setStatus(`Validation: ${result?.status || "OK"}`);
    } catch {
      setStatus("Failed to validate mission");
    }
  }

  async function handleRunMissionTask() {
    if (!missionId || !robotId) return;
    setStatus("Creating RUN_MISSION task...");
    try {
      await createTask(baseUrl, { type: "RUN_MISSION", missionId, robotId });
      setStatus("Mission task created");
    } catch {
      setStatus("Failed to create mission task");
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Missions & Teaching</h1>
          <p style={{ color: "#a1a1aa" }}>Teach sessions, mission library, validate and run</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Teach Mode Console</h2>
        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Session Setup</label>
            <select value={robotId} onChange={(e) => setRobotId(e.target.value)} style={inputStyle}>
              {robots.map((r) => (
                <option key={r.id} value={r.id}>{r.label} ({r.id})</option>
              ))}
            </select>
            <input
              placeholder="mapVersionId"
              value={mapVersionId}
              onChange={(e) => setMapVersionId(e.target.value)}
              style={inputStyle}
            />
            <button onClick={handleCreateTeachSession} disabled={!robotId || !mapVersionId} style={buttonStyle}>Create Session</button>
            <input
              placeholder="teachSessionId"
              value={teachSessionId}
              onChange={(e) => setTeachSessionId(e.target.value)}
              style={inputStyle}
            />
            <div style={{ display: "flex", gap: 8 }}>
              <button onClick={handleStartTeach} disabled={!teachSessionId} style={buttonStyle}>Start Teach</button>
              <button onClick={handleStopTeach} disabled={!teachSessionId} style={buttonStyle}>Stop Teach</button>
            </div>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Capture Step</label>
            <textarea
              rows={6}
              placeholder='{"action":"ACTUATE","type":"grip","params":{"open":true}}'
              value={captureCommandJson}
              onChange={(e) => setCaptureCommandJson(e.target.value)}
              style={textareaStyle}
            />
            <button onClick={handleCaptureStep} disabled={!teachSessionId} style={buttonStyle}>Capture Step</button>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Mission Library</h2>
        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Create / Update</label>
            <input placeholder="missionId" value={missionId} onChange={(e) => setMissionId(e.target.value)} style={inputStyle} />
            <input placeholder="name" value={missionName} onChange={(e) => setMissionName(e.target.value)} style={inputStyle} />
            <input placeholder="version" value={missionVersion} onChange={(e) => setMissionVersion(e.target.value)} style={inputStyle} />
            <div style={{ display: "flex", gap: 8 }}>
              <button onClick={handleCreateMission} disabled={!missionId || !missionName || !missionVersion} style={buttonStyle}>Create</button>
              <button onClick={handleUpdateMission} disabled={!missionId} style={buttonStyle}>Update</button>
            </div>
            <button onClick={handleValidateMission} disabled={!missionId} style={buttonStyle}>Validate</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Save From Teach</label>
            <input placeholder="teachSessionId" value={teachSessionId} onChange={(e) => setTeachSessionId(e.target.value)} style={inputStyle} />
            <button onClick={handleSaveMission} disabled={!teachSessionId || !missionId || !missionName || !missionVersion} style={buttonStyle}>Save Mission</button>
            <label style={{ color: "white", marginTop: 12 }}>Run Mission</label>
            <div style={{ display: "flex", gap: 8 }}>
              <select value={robotId} onChange={(e) => setRobotId(e.target.value)} style={inputStyle}>
                {robots.map((r) => (
                  <option key={r.id} value={r.id}>{r.label} ({r.id})</option>
                ))}
              </select>
              <button onClick={handleRunMissionTask} disabled={!missionId || !robotId} style={buttonStyle}>Run</button>
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

const textareaStyle: React.CSSProperties = {
  padding: 10,
  borderRadius: 8,
  border: "1px solid #27272a",
  background: "#0a0a0a",
  color: "white",
  fontFamily: "monospace",
};

const buttonStyle: React.CSSProperties = {
  padding: 10,
  borderRadius: 8,
  border: "1px solid #22c55e",
  background: "#22c55e",
  color: "white",
  width: 160,
};
