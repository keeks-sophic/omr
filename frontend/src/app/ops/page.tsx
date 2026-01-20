"use client";

import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { fetchOpsHealth, fetchOpsJetstream, fetchOpsAlerts, fetchOpsAudit, OpsAlert, JetstreamStats, OpsHealth, AuditEvent } from "../../lib/opsApi";
import { useSignalR } from "../../hooks/useSignalR";

export default function OpsPage() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const [opsHealth, setOpsHealth] = useState<OpsHealth | null>(null);
  const [jetstream, setJetstream] = useState<JetstreamStats | null>(null);
  const [alerts, setAlerts] = useState<OpsAlert[]>([]);
  const [audit, setAudit] = useState<AuditEvent[]>([]);
  const { connection, isConnected } = useSignalR();

  useEffect(() => {
    fetchOpsHealth(baseUrl).then(setOpsHealth).catch(() => {});
    fetchOpsJetstream(baseUrl).then(setJetstream).catch(() => {});
    fetchOpsAlerts(baseUrl).then(setAlerts).catch(() => {});
    fetchOpsAudit(baseUrl).then(setAudit).catch(() => {});
  }, [baseUrl]);

  useEffect(() => {
    if (!connection) return;
    connection.on("ops.jetstream.updated", (payload: any) => {
      setJetstream(payload);
    });
    connection.on("ops.alert.raised", (payload: any) => {
      const alert = {
        id: String(payload?.id || Math.random()),
        type: String(payload?.type || "ops"),
        severity: payload?.severity || "warn",
        message: String(payload?.message || "Alert"),
        timestamp: String(payload?.timestamp || new Date().toISOString()),
      };
      setAlerts((prev) => [alert, ...prev].slice(0, 100));
    });
    connection.on("ops.alert.cleared", (payload: any) => {
      setAlerts((prev) => prev.filter((a) => a.id !== String(payload?.id)));
    });
    return () => {
      connection.off("ops.jetstream.updated");
      connection.off("ops.alert.raised");
      connection.off("ops.alert.cleared");
    };
  }, [connection]);

  return (
    <div style={{ display: "grid", gap: 24 }}>
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
        <div>
          <h1 style={{ color: "white", fontSize: 24 }}>Observability & Ops</h1>
          <p style={{ color: "#a1a1aa" }}>Health, JetStream, alerts, and audit</p>
        </div>
        <div style={{ padding: "6px 10px", borderRadius: 999, border: "1px solid rgba(34,197,94,0.2)", color: isConnected ? "#22c55e" : "#ef4444" }}>
          {isConnected ? "Realtime connected" : "Realtime offline"}
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Backend Health</h2>
          <div style={{ marginTop: 8, color: "white", fontWeight: 600 }}>{opsHealth?.status || "Unknown"}</div>
          <div style={{ display: "grid", gap: 6, marginTop: 12 }}>
            {opsHealth?.components &&
              Object.entries(opsHealth.components).map(([k, v]) => (
                <div key={k} style={{ display: "flex", justifyContent: "space-between", fontFamily: "monospace", fontSize: 12 }}>
                  <span style={{ color: "#a1a1aa" }}>{k}</span>
                  <span style={{ color: v === "OK" ? "#22c55e" : "#f59e0b" }}>{v}</span>
                </div>
              ))}
          </div>
        </div>

        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>JetStream</h2>
          <div style={{ display: "grid", gap: 6, marginTop: 12, fontFamily: "monospace", fontSize: 12 }}>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "#a1a1aa" }}>Lag</span>
              <span style={{ color: "white" }}>{jetstream?.lag ?? "-"}</span>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "#a1a1aa" }}>Dropped</span>
              <span style={{ color: "white" }}>{jetstream?.droppedMessages ?? "-"}</span>
            </div>
            <div style={{ display: "flex", justifyContent: "space-between" }}>
              <span style={{ color: "#a1a1aa" }}>ConsumersHealthy</span>
              <span style={{ color: jetstream?.consumersHealthy ? "#22c55e" : "#f59e0b" }}>{jetstream?.consumersHealthy ? "Yes" : "No"}</span>
            </div>
          </div>
        </div>
      </div>

      <div style={{ display: "grid", gridTemplateColumns: "repeat(2, 1fr)", gap: 16 }}>
        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Alerts</h2>
          <div style={{ display: "grid", gap: 8, marginTop: 12 }}>
            {alerts.slice(0, 30).map((a) => (
              <div key={a.id} style={{ display: "grid", gap: 4, padding: 10, borderRadius: 8, background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.08)" }}>
                <div style={{ display: "flex", justifyContent: "space-between", fontFamily: "monospace", fontSize: 12, color: "#a1a1aa" }}>
                  <span>{new Date(a.timestamp).toLocaleString()}</span>
                  <span style={{ color: a.severity === "error" ? "#ef4444" : a.severity === "warn" ? "#f59e0b" : "#38bdf8" }}>{a.severity}</span>
                </div>
                <div style={{ color: "white" }}>{a.message}</div>
                <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa" }}>{a.type}</div>
              </div>
            ))}
          </div>
        </div>

        <div style={{ padding: 16, borderRadius: 12, border: "1px solid rgba(255,255,255,0.08)" }}>
          <h2 style={{ color: "white", fontSize: 18 }}>Audit</h2>
          <div style={{ display: "grid", gap: 8, marginTop: 12 }}>
            {audit.slice(0, 30).map((e) => (
              <div key={e.id} style={{ display: "grid", gap: 4, padding: 10, borderRadius: 8, background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.08)" }}>
                <div style={{ display: "flex", justifyContent: "space-between", fontFamily: "monospace", fontSize: 12, color: "#a1a1aa" }}>
                  <span>{new Date(e.timestamp).toLocaleString()}</span>
                  <span style={{ color: e.outcome === "OK" ? "#22c55e" : e.outcome === "DENIED" ? "#ef4444" : "#f59e0b" }}>{e.outcome}</span>
                </div>
                <div style={{ color: "white" }}>{e.action}</div>
                <div style={{ fontFamily: "monospace", fontSize: 12, color: "#a1a1aa" }}>
                  actor={e.actor} role={e.role} target={e.target || "-"}
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
