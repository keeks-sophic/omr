"use client";

import { useEffect, useState, useCallback } from "react";
import { ArrowUp, ArrowDown, ArrowLeft, ArrowRight, StopCircle, Power, Zap, Bot } from "lucide-react";
import { Robot } from "../../types";
import * as signalR from "@microsoft/signalr";

export default function ControlPage() {
  const [robots, setRobots] = useState<Robot[]>([]);
  const [selectedIp, setSelectedIp] = useState<string | null>(null);
  const [activeCommand, setActiveCommand] = useState<string | null>(null);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);

  // Get currently selected robot data
  const selectedRobot = robots.find(r => r.ip === selectedIp);

  const handleCommand = useCallback(async (cmd: string) => {
    setActiveCommand(cmd);
    const map: Record<string, string> = {
      UP: "moveup",
      DOWN: "movedown",
      LEFT: "moveleft",
      RIGHT: "moveright",
      CHARGE: "charge",
    };
    const backendCmd = map[cmd];
    if (backendCmd && selectedIp && connection) {
      try {
        await connection.invoke("SendCommand", { ip: selectedIp, command: backendCmd, data: {} });
      } catch {}
    }
    if (cmd !== "STOP") {
      setTimeout(() => setActiveCommand(null), 200);
    }
  }, [selectedIp, connection]);

  const handleKeyDown = useCallback((e: KeyboardEvent) => {
    if (e.repeat) return;
    
    switch (e.key) {
      case "ArrowUp": handleCommand("UP"); break;
      case "ArrowDown": handleCommand("DOWN"); break;
      case "ArrowLeft": handleCommand("LEFT"); break;
      case "ArrowRight": handleCommand("RIGHT"); break;
    }
  }, [handleCommand]);

  const handleKeyUp = useCallback((e: KeyboardEvent) => {
    switch (e.key) {
      case "ArrowUp":
      case "ArrowDown":
      case "ArrowLeft":
      case "ArrowRight":
        handleCommand("STOP");
        break;
    }
  }, [handleCommand]);

  useEffect(() => {
    window.addEventListener("keydown", handleKeyDown);
    window.addEventListener("keyup", handleKeyUp);
    return () => {
      window.removeEventListener("keydown", handleKeyDown);
      window.removeEventListener("keyup", handleKeyUp);
    };
  }, [handleKeyDown, handleKeyUp]);

  useEffect(() => {
    let mounted = true;
    (async () => {
      try {
        const res = await fetch("http://localhost:5146/robots", { credentials: "include" });
        const data: Robot[] = await res.json();
        if (!mounted) return;
        setRobots(data);
        if (data.length > 0) setSelectedIp(data[0].ip);
      } catch {}
    })();
    return () => { mounted = false; };
  }, []);

  useEffect(() => {
    const conn = new signalR.HubConnectionBuilder()
      .withUrl("http://localhost:5146/hub/robots", { withCredentials: true })
      .withAutomaticReconnect()
      .build();
    conn.on("telemetry", (dto: Robot) => {
      setRobots(prev => {
        const idx = prev.findIndex(r => r.ip === dto.ip);
        if (idx >= 0) {
          const next = [...prev];
          next[idx] = { ...next[idx], ...dto };
          return next;
        }
        return [...prev, dto];
      });
    });
    conn.on("commandAck", () => {});
    conn.start().then(() => setConnection(conn)).catch(() => {});
    return () => {
      conn.stop().catch(() => {});
      setConnection(null);
    };
  }, []);

  return (
    <div className="max-w-7xl mx-auto space-y-6">
       <div className="flex items-center justify-between">
         <div>
            <h1 className="text-3xl font-semibold tracking-tight text-white">Manual Control</h1>
            <p className="text-zinc-400 mt-1">Direct teleoperation of connected units.</p>
         </div>
         <div className="flex gap-4 items-center">
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-full border text-emerald-400 bg-emerald-500/10 border-emerald-500/20">
               <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
               <span className="text-xs font-mono font-medium">SYSTEM ONLINE</span>
            </div>
         </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
         {/* Sidebar: Robot List */}
         <div className="lg:col-span-1 space-y-4">
            <h2 className="text-sm font-medium text-zinc-400 uppercase tracking-wider px-1">Available Units</h2>
            <div className="space-y-3">
              {robots.map((robot) => (
                <button
                  key={robot.ip}
                  onClick={() => setSelectedIp(robot.ip)}
                  className={`w-full text-left p-4 rounded-xl border transition-all ${
                    selectedIp === robot.ip 
                      ? "bg-zinc-800 border-primary/50 shadow-lg shadow-primary/10" 
                      : "bg-zinc-900/40 border-zinc-800 hover:border-zinc-700 hover:bg-zinc-800/50"
                  }`}
                >
                  <div className="flex justify-between items-start mb-2">
                    <div className="flex items-center gap-3">
                      <div className={`w-8 h-8 rounded-lg flex items-center justify-center ${
                        selectedIp === robot.ip ? "bg-primary text-white" : "bg-zinc-800 text-zinc-400"
                      }`}>
                        <Bot size={16} />
                      </div>
                      <div>
                        <div className="font-medium text-sm text-white">{robot.name}</div>
                        <div className="text-[10px] font-mono text-zinc-500">{robot.ip}</div>
                      </div>
                    </div>
                    <div className={`w-2 h-2 rounded-full ${
                      robot.connected ? "bg-emerald-500 shadow-[0_0_8px_rgba(16,185,129,0.4)]" : "bg-rose-500"
                    }`} />
                  </div>
                  
                  <div className="flex items-center justify-between text-[10px] text-zinc-500 font-mono mt-2 pt-2 border-t border-white/5">
                    <span className={robot.connected ? "text-emerald-400" : "text-zinc-600"}>
                      {robot.connected ? "ONLINE" : "OFFLINE"}
                    </span>
                    <span className={robot.battery < 20 ? "text-rose-400" : "text-zinc-400"}>
                       BAT: {robot.battery.toFixed(1)}%
                    </span>
                  </div>
                </button>
              ))}
            </div>
         </div>

         {/* Main Control Area */}
         <div className="lg:col-span-3 grid grid-cols-1 md:grid-cols-2 gap-8">
            {/* Camera Feed */}
            <div className="glass-card rounded-2xl aspect-video bg-black relative overflow-hidden group">
                <div className="absolute inset-0 bg-zinc-900 flex items-center justify-center">
                  {selectedRobot?.connected ? (
                     <div className="text-center space-y-2">
                        <div className="w-12 h-12 rounded-full border-2 border-zinc-800 border-t-emerald-500 animate-spin mx-auto" />
                        <span className="text-zinc-500 font-mono text-xs block">ESTABLISHING FEED...</span>
                     </div>
                  ) : (
                     <div className="flex flex-col items-center gap-2 opacity-50">
                        <Power size={32} className="text-zinc-700" />
                        <span className="text-zinc-700 font-mono text-xs">NO SIGNAL</span>
                     </div>
                  )}
                </div>
                
                {/* HUD Overlay */}
                <div className="absolute inset-0 p-4 flex flex-col justify-between pointer-events-none">
                  <div className="flex justify-between">
                      <span className="text-[10px] font-mono text-emerald-500">
                        {selectedIp ? `UNIT: ${selectedIp}` : "SELECT UNIT"}
                      </span>
                      {selectedRobot?.connected && (
                         <span className="text-[10px] font-mono text-rose-500 animate-pulse">● REC</span>
                      )}
                  </div>
                  <div className="flex justify-center">
                      <div className="w-8 h-8 border-2 border-white/20 rounded-full flex items-center justify-center">
                        <div className="w-1 h-1 bg-white/50 rounded-full" />
                      </div>
                  </div>
                </div>
            </div>

            {/* Controls */}
            <div className="space-y-6">
                <div className="glass-card p-8 rounded-2xl flex flex-col items-center justify-center gap-6">
                  <div className="flex flex-col items-center gap-2">
                      <button 
                        onMouseDown={() => handleCommand("UP")}
                        onMouseUp={() => handleCommand("STOP")}
                        onMouseLeave={() => handleCommand("STOP")}
                        className={`w-16 h-16 rounded-2xl border transition-all active:scale-95 flex items-center justify-center shadow-lg ${
                           activeCommand === "UP" 
                              ? "bg-primary text-white border-primary shadow-primary/30" 
                              : "bg-zinc-800 border-zinc-700 text-zinc-400 hover:text-white hover:bg-zinc-700"
                        }`}
                      >
                        <ArrowUp size={32} />
                      </button>
                      <div className="flex gap-2">
                        <button 
                            onMouseDown={() => handleCommand("LEFT")}
                            onMouseUp={() => handleCommand("STOP")}
                            onMouseLeave={() => handleCommand("STOP")}
                            className={`w-16 h-16 rounded-2xl border transition-all active:scale-95 flex items-center justify-center shadow-lg ${
                              activeCommand === "LEFT" 
                                 ? "bg-primary text-white border-primary shadow-primary/30" 
                                 : "bg-zinc-800 border-zinc-700 text-zinc-400 hover:text-white hover:bg-zinc-700"
                           }`}
                        >
                            <ArrowLeft size={32} />
                        </button>
                        <button 
                            onMouseDown={() => handleCommand("DOWN")}
                            onMouseUp={() => handleCommand("STOP")}
                            onMouseLeave={() => handleCommand("STOP")}
                            className={`w-16 h-16 rounded-2xl border transition-all active:scale-95 flex items-center justify-center shadow-lg ${
                              activeCommand === "DOWN" 
                                 ? "bg-primary text-white border-primary shadow-primary/30" 
                                 : "bg-zinc-800 border-zinc-700 text-zinc-400 hover:text-white hover:bg-zinc-700"
                           }`}
                        >
                            <ArrowDown size={32} />
                        </button>
                        <button 
                            onMouseDown={() => handleCommand("RIGHT")}
                            onMouseUp={() => handleCommand("STOP")}
                            onMouseLeave={() => handleCommand("STOP")}
                            className={`w-16 h-16 rounded-2xl border transition-all active:scale-95 flex items-center justify-center shadow-lg ${
                              activeCommand === "RIGHT" 
                                 ? "bg-primary text-white border-primary shadow-primary/30" 
                                 : "bg-zinc-800 border-zinc-700 text-zinc-400 hover:text-white hover:bg-zinc-700"
                           }`}
                        >
                            <ArrowRight size={32} />
                        </button>
                      </div>
                  </div>

                  <div className="w-full h-px bg-white/5" />

                  <div className="flex flex-col gap-3 w-full">
                    <button 
                        onClick={() => handleCommand("STOP")}
                        className="w-full py-4 rounded-xl bg-rose-500/10 hover:bg-rose-500/20 text-rose-500 border border-rose-500/20 flex items-center justify-center gap-3 font-medium transition-all hover:scale-[1.02] active:scale-[0.98]"
                    >
                        <StopCircle size={24} />
                        EMERGENCY STOP
                    </button>

                    <button 
                        onClick={() => handleCommand("CHARGE")}
                        className="w-full py-4 rounded-xl bg-gradient-to-r from-violet-600 to-indigo-600 text-white shadow-lg shadow-violet-500/20 flex items-center justify-center gap-3 font-medium transition-all hover:scale-[1.02] active:scale-[0.98]"
                    >
                        <Zap size={24} />
                        INITIATE CHARGE
                    </button>
                  </div>
                </div>

                
                <div className="grid grid-cols-2 gap-4">
                  <div className="glass-card p-4 rounded-xl">
                      <span className="text-xs text-zinc-500 block mb-1">Status</span>
                      <div className="flex items-baseline gap-2">
                        <span className={`text-lg font-bold ${
                           selectedRobot?.state === "active" ? "text-emerald-400" :
                           selectedRobot?.state === "charging" ? "text-amber-400" : "text-zinc-400"
                        }`}>
                           {selectedRobot?.state?.toUpperCase() || "UNKNOWN"}
                        </span>
                      </div>
                  </div>
                  <div className="glass-card p-4 rounded-xl">
                      <span className="text-xs text-zinc-500 block mb-1">Location</span>
                      <div className="flex items-baseline gap-1">
                         <span className="text-sm font-mono text-white">
                           {selectedRobot ? `X:${selectedRobot.x.toFixed(1)} Y:${selectedRobot.y.toFixed(1)}` : "N/A"}
                         </span>
                      </div>
                  </div>
                </div>
            </div>
         </div>
      </div>
    </div>
  );
}
