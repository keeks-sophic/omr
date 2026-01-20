"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { getRobots } from "../../lib/configApi";
import { createTask, pauseTask, resumeTask, cancelTask } from "../../lib/tasksApi";
import { planRoute } from "../../lib/routeApi";
import { useSignalR } from "../../hooks/useSignalR";

type RobotOption = { id: string; label: string };
type TaskItem = { taskId: string; type: string; status: string; robotId?: string; eta?: number };

export default function TasksPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const { connection, isConnected } = useSignalR();
  const [robots, setRobots] = useState<RobotOption[]>([]);
  const [taskType, setTaskType] = useState("GO_TO_POINT");
  const [pointId, setPointId] = useState("");
  const [fromPointId, setFromPointId] = useState("");
  const [toPointId, setToPointId] = useState("");
  const [missionId, setMissionId] = useState("");
  const [assignmentMode, setAssignmentMode] = useState<"AUTO" | "MANUAL">("AUTO");
  const [robotId, setRobotId] = useState("");
  const [priority, setPriority] = useState<number>(0);
  const [earliestStart, setEarliestStart] = useState("");
  const [allowPreemption, setAllowPreemption] = useState(false);
  const [status, setStatus] = useState<string | null>(null);
  const [tasks, setTasks] = useState<TaskItem[]>([]);
  const [activeRoute, setActiveRoute] = useState<any | null>(null);
  const [previewRoute, setPreviewRoute] = useState<any | null>(null);
  const [holdsSummary, setHoldsSummary] = useState<any | null>(null);
  const [controlTaskId, setControlTaskId] = useState("");

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
    connection.on("task.created", (payload: any) => {
      const t: TaskItem = {
        taskId: String(payload?.taskId || ""),
        type: String(payload?.type || ""),
        status: String(payload?.status || "CREATED"),
        robotId: payload?.robotId ? String(payload.robotId) : undefined,
      };
      setTasks((prev) => {
        const idx = prev.findIndex((x) => x.taskId === t.taskId);
        if (idx === -1) return [t, ...prev].slice(0, 200);
        const next = [...prev];
        next[idx] = { ...next[idx], ...t };
        return next;
      });
    });
    connection.on("task.assigned", (payload: any) => {
      setTasks((prev) => prev.map((x) => (x.taskId === String(payload?.taskId) ? { ...x, robotId: String(payload?.robotId || ""), status: "ASSIGNED" } : x)));
    });
    connection.on("task.status.changed", (payload: any) => {
      setTasks((prev) => prev.map((x) => (x.taskId === String(payload?.taskId) ? { ...x, status: String(payload?.status || x.status) } : x)));
    });
    connection.on("task.completed", (payload: any) => {
      setTasks((prev) => prev.map((x) => (x.taskId === String(payload?.taskId) ? { ...x, status: "COMPLETED" } : x)));
    });
    connection.on("task.failed", (payload: any) => {
      setTasks((prev) => prev.map((x) => (x.taskId === String(payload?.taskId) ? { ...x, status: "FAILED" } : x)));
    });
    connection.on("route.updated", (payload: any) => {
      setActiveRoute(payload);
    });
    connection.on("traffic.schedule.summary.updated", (payload: any) => {
      setHoldsSummary(payload);
    });
    return () => {
      connection.off("task.created");
      connection.off("task.assigned");
      connection.off("task.status.changed");
      connection.off("task.completed");
      connection.off("task.failed");
      connection.off("route.updated");
      connection.off("traffic.schedule.summary.updated");
    };
  }, [connection]);

  async function handleCreateTask() {
    setStatus("Creating task...");
    try {
      const parameters =
        taskType === "GO_TO_POINT"
          ? { pointId }
          : taskType === "PICK_DROP"
          ? { fromPointId, toPointId }
          : taskType === "CHARGE"
          ? { pointId }
          : taskType === "RUN_MISSION"
          ? { missionId }
          : {};
      const payload: any = {
        type: taskType,
        parameters,
        assignment: assignmentMode === "AUTO" ? { mode: "AUTO" } : { mode: "MANUAL", robotId },
        constraints: {
          priority,
          earliestStart: earliestStart || undefined,
          allowPreemption,
        },
      };
      const res = await createTask(baseUrl, payload);
      const t: TaskItem = {
        taskId: String(res?.taskId || res?.id || ""),
        type: taskType,
        status: "CREATED",
        robotId: assignmentMode === "MANUAL" ? robotId : undefined,
      };
      setTasks((prev) => [t, ...prev].slice(0, 200));
      setStatus("Task created");
    } catch {
      setStatus("Failed to create task");
    }
  }

  async function handlePause() {
    if (!controlTaskId) return;
    setStatus("Pausing task...");
    try {
      await pauseTask(baseUrl, controlTaskId);
      setStatus("Task paused");
    } catch {
      setStatus("Failed to pause task");
    }
  }

  async function handleResume() {
    if (!controlTaskId) return;
    setStatus("Resuming task...");
    try {
      await resumeTask(baseUrl, controlTaskId);
      setStatus("Task resumed");
    } catch {
      setStatus("Failed to resume task");
    }
  }

  async function handleCancel() {
    if (!controlTaskId) return;
    setStatus("Canceling task...");
    try {
      await cancelTask(baseUrl, controlTaskId);
      setStatus("Task canceled");
    } catch {
      setStatus("Failed to cancel task");
    }
  }

  async function handlePreviewRoute() {
    const parameters =
      taskType === "GO_TO_POINT"
        ? { pointId }
        : taskType === "PICK_DROP"
        ? { fromPointId, toPointId }
        : taskType === "CHARGE"
        ? { pointId }
        : taskType === "RUN_MISSION"
        ? { missionId }
        : {};
    const payload: any = { taskType, parameters, robotId: assignmentMode === "MANUAL" ? robotId : undefined };
    try {
      const route = await planRoute(baseUrl, payload);
      setPreviewRoute(route);
    } catch {
      setPreviewRoute(null);
    }
  }

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Tasks</h1>
          <p style={{ color: "#a1a1aa" }}>Create and manage tasks; monitor lifecycle and routes</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gap: 12, padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Create Task</h2>
        <div style={{ display: "grid", gap: 8, gridTemplateColumns: "repeat(2, 1fr)" }}>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Type</label>
            <select value={taskType} onChange={(e) => setTaskType(e.target.value)} style={inputStyle}>
              <option value="GO_TO_POINT">GO_TO_POINT</option>
              <option value="PICK_DROP">PICK_DROP</option>
              <option value="CHARGE">CHARGE</option>
              <option value="RUN_MISSION">RUN_MISSION</option>
            </select>
            {taskType === "GO_TO_POINT" && (
              <input placeholder="pointId" value={pointId} onChange={(e) => setPointId(e.target.value)} style={inputStyle} />
            )}
            {taskType === "PICK_DROP" && (
              <>
                <input placeholder="fromPointId" value={fromPointId} onChange={(e) => setFromPointId(e.target.value)} style={inputStyle} />
                <input placeholder="toPointId" value={toPointId} onChange={(e) => setToPointId(e.target.value)} style={inputStyle} />
              </>
            )}
            {taskType === "CHARGE" && (
              <input placeholder="pointId" value={pointId} onChange={(e) => setPointId(e.target.value)} style={inputStyle} />
            )}
            {taskType === "RUN_MISSION" && (
              <input placeholder="missionId" value={missionId} onChange={(e) => setMissionId(e.target.value)} style={inputStyle} />
            )}
          </div>
          <div style={{ display: "grid", gap: 8 }}>
            <label style={{ color: "white" }}>Assignment</label>
            <div style={{ display: "flex", gap: 12 }}>
              <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
                <input type="radio" checked={assignmentMode === "AUTO"} onChange={() => setAssignmentMode("AUTO")} />
                AUTO
              </label>
              <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
                <input type="radio" checked={assignmentMode === "MANUAL"} onChange={() => setAssignmentMode("MANUAL")} />
                MANUAL
              </label>
            </div>
            {assignmentMode === "MANUAL" && (
              <select value={robotId} onChange={(e) => setRobotId(e.target.value)} style={inputStyle}>
                {robots.map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.label} ({r.id})
                  </option>
                ))}
              </select>
            )}
            <label style={{ color: "white" }}>Priority</label>
            <input type="number" value={priority} onChange={(e) => setPriority(Number(e.target.value))} style={inputStyle} />
            <label style={{ color: "white" }}>Earliest Start</label>
            <input type="datetime-local" value={earliestStart} onChange={(e) => setEarliestStart(e.target.value)} style={inputStyle} />
            <label style={{ color: "white", display: "flex", alignItems: "center", gap: 8 }}>
              <input type="checkbox" checked={allowPreemption} onChange={(e) => setAllowPreemption(e.target.checked)} />
              allowPreemption
            </label>
            <div style={{ display: "flex", gap: 8 }}>
              <button onClick={handleCreateTask} style={buttonStyle}>Create</button>
              <button onClick={handlePreviewRoute} style={buttonStyleSecondary}>Preview Route</button>
            </div>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Tasks</h2>
          <div style={{ display: "grid", gap: 8, marginTop: 8 }}>
            {tasks.slice(0, 50).map((t) => (
              <div key={t.taskId} style={{ display: "flex", justifyContent: "space-between", padding: 10, borderRadius: 8, background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.08)", fontFamily: "monospace", fontSize: 12 }}>
                <span style={{ color: "#a1a1aa" }}>{t.taskId}</span>
                <span style={{ color: "white" }}>{t.type}</span>
                <span style={{ color: t.status === "FAILED" ? "#ef4444" : t.status === "COMPLETED" ? "#22c55e" : "#a1a1aa" }}>{t.status}</span>
                <span style={{ color: "#a1a1aa" }}>{t.robotId || "-"}</span>
              </div>
            ))}
          </div>
        </div>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Controls</h2>
          <input placeholder="taskId" value={controlTaskId} onChange={(e) => setControlTaskId(e.target.value)} style={inputStyle} />
          <div style={{ display: "flex", gap: 8, marginTop: 8 }}>
            <button onClick={handlePause} disabled={!controlTaskId} style={buttonStyle}>Pause</button>
            <button onClick={handleResume} disabled={!controlTaskId} style={buttonStyle}>Resume</button>
            <button onClick={handleCancel} disabled={!controlTaskId} style={buttonStyle}>Cancel</button>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Route Preview</h2>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8, overflowX: "auto" }}>
            {previewRoute ? JSON.stringify(previewRoute, null, 2) : "No preview"}
          </div>
        </div>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Active Route</h2>
          <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8, overflowX: "auto" }}>
            {activeRoute ? JSON.stringify(activeRoute, null, 2) : "No route"}
          </div>
        </div>
      </div>

      <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
        <h2 style={{ color: "white", fontSize: 18 }}>Traffic Holds Summary</h2>
        <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa", marginTop: 8, overflowX: "auto" }}>
          {holdsSummary ? JSON.stringify(holdsSummary, null, 2) : "No holds"}
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

const buttonStyleSecondary: React.CSSProperties = {
  padding: 10,
  borderRadius: 8,
  border: "1px solid #ffffff1a",
  background: "#0a0a0a",
  color: "white",
  width: 160,
};
