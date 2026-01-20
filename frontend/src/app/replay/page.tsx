"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { getRobots } from "../../lib/configApi";
import { createReplaySession, startReplay, stopReplay, seekReplay } from "../../lib/replayApi";
import { useSignalR } from "../../hooks/useSignalR";

type RobotOption = { id: string; label: string };

export default function ReplayPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const [robots, setRobots] = useState<RobotOption[]>([]);
  const [robotId, setRobotId] = useState("");
  const [fromTime, setFromTime] = useState("");
  const [toTime, setToTime] = useState("");
  const [playbackSpeed, setPlaybackSpeed] = useState(1);
  const [replaySessionId, setReplaySessionId] = useState("");
  const [status, setStatus] = useState<string | null>(null);
  const [events, setEvents] = useState<any[]>([]);
  const { connection, isConnected } = useSignalR();

  useEffect(() => {
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
      .catch(() => {});
  }, [baseUrl]);

  useEffect(() => {
    if (!connection) return;
    connection.on("replay.session.status", (payload: any) => {
      setStatus(`Replay status: ${payload?.status || ""}`);
    });
    connection.on("replay.event", (payload: any) => {
      setEvents((prev) => [payload, ...prev].slice(0, 200));
    });
    return () => {
      connection.off("replay.session.status");
      connection.off("replay.event");
    };
  }, [connection]);

  async function handleCreateSession() {
    if (!robotId || !fromTime || !toTime) return;
    setStatus("Creating replay session...");
    try {
      const res = await createReplaySession(baseUrl, { robotId, fromTime, toTime, playbackSpeed });
      const id = res?.replaySessionId || res?.id || "";
      setReplaySessionId(String(id));
      setStatus(`Replay session created: ${id}`);
      setEvents([]);
    } catch {
      setStatus("Failed to create replay session");
    }
  }

  async function handleStart() {
    if (!replaySessionId) return;
    setStatus("Starting replay...");
    try {
      await startReplay(baseUrl, replaySessionId);
      setStatus("Replay started");
    } catch {
      setStatus("Failed to start replay");
    }
  }

  async function handleStop() {
    if (!replaySessionId) return;
    setStatus("Stopping replay...");
    try {
      await stopReplay(baseUrl, replaySessionId);
      setStatus("Replay stopped");
    } catch {
      setStatus("Failed to stop replay");
    }
  }

  async function handleSeek(ts: string) {
    if (!replaySessionId || !ts) return;
    setStatus("Seeking...");
    try {
      await seekReplay(baseUrl, replaySessionId, { timestamp: ts });
      setStatus("Seek ok");
    } catch {
      setStatus("Failed to seek");
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Replay</h1>
          <p style={{ color: "#a1a1aa" }}>Incident review and playback</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Create Session</h2>
        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Robot</label>
            <select value={robotId} onChange={(e) => setRobotId(e.target.value)} style={inputStyle}>
              {robots.map((r) => (
                <option key={r.id} value={r.id}>{r.label} ({r.id})</option>
              ))}
            </select>
            <label style={{ color: "white" }}>From</label>
            <input type="datetime-local" value={fromTime} onChange={(e) => setFromTime(e.target.value)} style={inputStyle} />
            <label style={{ color: "white" }}>To</label>
            <input type="datetime-local" value={toTime} onChange={(e) => setToTime(e.target.value)} style={inputStyle} />
            <label style={{ color: "white" }}>Playback Speed</label>
            <input type="number" step="0.1" value={playbackSpeed} onChange={(e) => setPlaybackSpeed(Number(e.target.value))} style={inputStyle} />
            <button onClick={handleCreateSession} disabled={!robotId || !fromTime || !toTime} style={buttonStyle}>Create</button>
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Session</label>
            <input placeholder="replaySessionId" value={replaySessionId} onChange={(e) => setReplaySessionId(e.target.value)} style={inputStyle} />
            <div style={{ display: "flex", gap: 8 }}>
              <button onClick={handleStart} disabled={!replaySessionId} style={buttonStyle}>Start</button>
              <button onClick={handleStop} disabled={!replaySessionId} style={buttonStyle}>Stop</button>
            </div>
            <label style={{ color: "white" }}>Seek</label>
            <input type="datetime-local" onChange={(e) => handleSeek(e.target.value)} style={inputStyle} />
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Events</h2>
        <div style={{ display: "grid", gap: 8 }}>
          {events.slice(0, 100).map((ev, i) => (
            <div key={i} style={{ display: "grid", gap: 4, padding: 10, borderRadius: 8, background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.08)" }}>
              <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa" }}>{ev?.timestamp || ""}</div>
              <div style={{ color: "white", fontFamily: "monospace", fontSize: 12 }}>{ev?.subject || ev?.type || "event"}</div>
              <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", overflowX: "auto" }}>
                {typeof ev?.payload === "string" ? ev.payload : JSON.stringify(ev?.payload || ev, null, 2)}
              </div>
            </div>
          ))}
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
