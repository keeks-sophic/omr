"use client";

import { Activity, Battery, Signal, Wifi, Clock, ArrowUpRight } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { getRobots } from "../../lib/configApi";
import { fetchOpsHealth, fetchOpsJetstream, fetchOpsAlerts, OpsAlert, JetstreamStats, OpsHealth } from "../../lib/opsApi";
import Link from "next/link";
import { useSignalR } from "../../hooks/useSignalR";

export default function FleetDashboard() {
  const baseUrl = useMemo(() => getApiBaseUrl(), []);
  const [robots, setRobots] = useState<any[]>([]);
  const [opsHealth, setOpsHealth] = useState<OpsHealth | null>(null);
  const [jetstream, setJetstream] = useState<JetstreamStats | null>(null);
  const [alerts, setAlerts] = useState<OpsAlert[]>([]);
  const { connection, isConnected } = useSignalR();

  useEffect(() => {
    getRobots(baseUrl).then(setRobots).catch(() => {});
    fetchOpsHealth(baseUrl).then(setOpsHealth).catch(() => {});
    fetchOpsJetstream(baseUrl).then(setJetstream).catch(() => {});
    fetchOpsAlerts(baseUrl).then(setAlerts).catch(() => {});
  }, [baseUrl]);

  useEffect(() => {
    if (!connection) return;
    connection.on("robot.session.updated", (payload: any) => {
      setRobots((prev) => {
        const id = payload?.robotId || payload?.id || payload?.name;
        const idx = prev.findIndex((r: any) => (r.robotId || r.id || r.name) === id);
        if (idx === -1) return [...prev, payload];
        const next = [...prev];
        next[idx] = { ...next[idx], ...payload };
        return next;
      });
    });
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
      setAlerts((prev) => [alert, ...prev].slice(0, 20));
    });
    connection.on("ops.alert.cleared", (payload: any) => {
      setAlerts((prev) => prev.filter((a) => a.id !== String(payload?.id)));
    });
    return () => {
      connection.off("robot.session.updated");
      connection.off("ops.jetstream.updated");
      connection.off("ops.alert.raised");
      connection.off("ops.alert.cleared");
    };
  }, [connection]);

  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-white">Fleet Dashboard</h1>
          <p className="text-zinc-400 mt-1">Fleet health, robots, ops status, and alerts.</p>
        </div>
        <div className="flex gap-2">
           <div className={`px-4 py-2 rounded-full text-sm font-mono border ${isConnected ? 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20' : 'bg-rose-500/10 text-rose-500 border-rose-500/20'}`}>
             {isConnected ? 'Realtime Connected' : 'Realtime Offline'}
           </div>
           <Link href="/robot" className="px-4 py-2 bg-primary hover:bg-primary-hover text-white rounded-full text-sm font-medium shadow-lg shadow-primary/20 transition-all">
              Robots
           </Link>
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 auto-rows-[180px]">
        
        <div className="glass-card rounded-2xl p-6 col-span-1 md:col-span-2 row-span-2 relative overflow-hidden group">
          <div className="absolute top-0 right-0 p-6 opacity-50 group-hover:opacity-100 transition-opacity">
            <ArrowUpRight className="text-zinc-500" />
          </div>
          <div className="h-full flex flex-col justify-between relative z-10">
            <div className="flex items-center gap-3">
               <div className="p-2 bg-emerald-500/10 rounded-lg">
                  <Activity className="text-emerald-400" size={24} />
               </div>
               <span className="text-zinc-400 font-medium uppercase tracking-wider text-xs">Ops Health</span>
            </div>
            <div>
              <div className="text-2xl font-bold text-white tracking-tighter">
                {opsHealth?.status || "Unknown"}
              </div>
              <div className="grid grid-cols-2 gap-2 mt-3">
                {opsHealth?.components &&
                  Object.entries(opsHealth.components).slice(0, 6).map(([k, v]) => (
                    <div key={k} className="text-xs text-zinc-400 font-mono flex justify-between">
                      <span>{k}</span>
                      <span className={v === "OK" ? "text-emerald-400" : "text-amber-400"}>{v}</span>
                    </div>
                  ))}
              </div>
            </div>
            <div className="w-full h-12 flex items-end gap-1 opacity-50">
               {[40, 60, 45, 70, 85, 60, 75, 50, 65, 80, 95, 85, 90].map((h, i) => (
                  <div key={i} style={{ height: `${h}%` }} className="flex-1 bg-gradient-to-t from-emerald-500/50 to-emerald-400/20 rounded-t-sm" />
               ))}
            </div>
          </div>
        </div>

        <div className="glass-card rounded-2xl p-5 flex flex-col justify-between hover:bg-white/5 transition-colors">
           <div className="flex justify-between items-start">
              <div className="p-2 bg-sky-500/10 rounded-lg">
                 <Wifi className="text-sky-400" size={20} />
              </div>
              <span className="text-xs text-zinc-500 font-mono">Realtime</span>
           </div>
           <div>
              <div className={`text-xs font-mono ${isConnected ? 'text-emerald-400' : 'text-rose-500'}`}>
                {isConnected ? "Connected" : "Offline"}
              </div>
            </div>
        </div>

        <div className="glass-card rounded-2xl p-5 flex flex-col justify-between hover:bg-white/5 transition-colors">
           <div className="flex justify-between items-start">
              <div className="p-2 bg-amber-500/10 rounded-lg">
                 <Battery className="text-amber-400" size={20} />
              </div>
              <span className="text-xs text-zinc-500 font-mono">JetStream</span>
           </div>
           <div>
              <div className="text-xs text-zinc-500 mt-1 font-mono space-y-1">
                <div className="flex justify-between">
                  <span>Lag</span>
                  <span className="text-white">{jetstream?.lag ?? "-"}</span>
                </div>
                <div className="flex justify_between">
                  <span>Dropped</span>
                  <span className="text_white">{jetstream?.droppedMessages ?? "-"}</span>
                </div>
                <div className="flex justify-between">
                  <span>ConsumersHealthy</span>
                  <span className={jetstream?.consumersHealthy ? "text-emerald-400" : "text-amber-400"}>
                    {jetstream?.consumersHealthy ? "Yes" : "No"}
                  </span>
                </div>
              </div>
           </div>
        </div>

        <div className="glass-card rounded-2xl p-6 col-span-1 md:col-span-2 flex flex-col justify_between">
           <div className="flex items-center justify-between mb-4">
              <h3 className="text-zinc-200 font-medium">Active Robots</h3>
              <span className="text-xs text-zinc-500 bg-zinc-800/50 px-2 py-1 rounded-full">
                {robots.length} Online
              </span>
           </div>
           <div className="space-y-3">
              {robots.map((r: any, i: number) => {
                const name = r.name || r.robotId || r.id || `Robot ${i + 1}`;
                const connected = r.connected ?? true;
                const battery = r.battery ?? r.telemetry?.battery ?? "-";
                const state = r.state ?? r.session?.state ?? "unknown";
                return (
                 <div key={i} className="flex items-center justify-between p-3 rounded-xl bg-white/5 hover:bg-white/10 transition-colors cursor-pointer group">
                    <div className="flex items-center gap-3">
                       <div className={`w-2 h-2 rounded-full ${connected ? 'bg-emerald-400' : 'bg-zinc-600'}`} />
                       <span className="text-sm font-medium text-zinc-200">{name}</span>
                    </div>
                    <div className="flex items-center gap-6 text-sm text-zinc-500 font-mono">
                       <span className="text-zinc-400">{String(battery)}%</span>
                       <span>{state}</span>
                       <Link href="/robot" className="text-xs text-primary hover:text-white transition-colors">Details →</Link>
                    </div>
                  </div>
              )})}
           </div>
        </div>

        <div className="glass-card rounded-2xl p-6 row-span-2 flex flex-col">
           <div className="flex items-center gap-2 mb-6">
              <Clock className="text-violet-400" size={20} />
              <h3 className="text-zinc-200 font-medium">Recent Alerts</h3>
           </div>
           <div className="space-y-6 relative pl-2">
              <div className="absolute left-[11px] top-2 bottom-2 w-[1px] bg-zinc-800" />
              {alerts.slice(0, 8).map((log, i) => (
                 <div key={i} className="relative pl-6">
                    <div className={`absolute left-0 top-1.5 w-6 h-6 rounded-full bg-zinc-950 border-2 z-10 flex items-center justify-center
                       ${log.severity === 'info' ? 'border-sky-500/50' : 
                         log.severity === 'error' ? 'border-rose-500/50' : 'border-amber-500/50'}`}
                    >
                       <div className={`w-1.5 h-1.5 rounded-full 
                          ${log.severity === 'info' ? 'bg-sky-400' : 
                            log.severity === 'error' ? 'bg-rose-400' : 'bg-amber-400'}`} 
                       />
                    </div>
                    <div className="text-xs text-zinc-500 font-mono mb-0.5">{new Date(log.timestamp).toLocaleTimeString()}</div>
                    <div className="text-sm text-zinc-300">{log.message}</div>
                 </div>
              ))}
           </div>
        </div>

        <div className="glass-card rounded-2xl p-5 flex flex-col justify-center items-center text-center hover:border-violet-500/30 transition-colors cursor-pointer group">
           <div className="w-12 h-12 rounded-full bg-zinc-800/50 flex items-center justify-center mb-3 group-hover:bg-violet-500/20 transition-colors">
              <Signal className="text-zinc-500 group-hover:text-violet-400 transition-colors" />
           </div>
           <h3 className="text-sm font-medium text-zinc-300">Signal Strength</h3>
           <p className="text-xs text-zinc-500 mt-1">Optimal Coverage</p>
        </div>

      </div>
    </div>
  );
}
