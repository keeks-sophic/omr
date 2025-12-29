"use client";

import { useEffect, useState } from "react";
import { HubConnectionBuilder, HubConnection } from "@microsoft/signalr";
import { Bot, Battery, Wifi, WifiOff, MapPin } from "lucide-react";

interface Robot {
  name: string;
  ip: string;
  x: number;
  y: number;
  state: string;
  battery: number;
  connected: boolean;
  lastActive?: string;
}

export default function RobotPage() {
  const [robots, setRobots] = useState<Robot[]>([]);
  const [loading, setLoading] = useState(true);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // 1. Initial Fetch to populate list
    const fetchInitialData = async () => {
      try {
        const res = await fetch("http://localhost:5146/robots");
        if (!res.ok) throw new Error("Failed to fetch initial robot data");
        const data = await res.json();
        setRobots(data);
        setLoading(false);
      } catch (err) {
        console.error("Initial fetch failed:", err);
        // Don't set error here, let SignalR try to connect
        setLoading(false); 
      }
    };

    fetchInitialData();

    // 2. Setup SignalR
    const newConnection = new HubConnectionBuilder()
      .withUrl("http://localhost:5146/hub/robots")
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log("Connected to SignalR");
          setError(null);

          // Handle 'identity' updates (New robot or IP change)
          connection.on("identity", (data: { name: string, ip: string }) => {
            setRobots(prev => {
              const idx = prev.findIndex(r => r.name === data.name);
              if (idx === -1) {
                // New robot
                return [...prev, { 
                  name: data.name, 
                  ip: data.ip, 
                  x: 0, 
                  y: 0, 
                  state: 'unknown', 
                  battery: 100,
                  connected: true,
                  lastActive: new Date().toISOString()
                }];
              }
              // Update IP
              const updated = [...prev];
              updated[idx] = { ...updated[idx], ip: data.ip };
              return updated;
            });
          });

          // Handle 'telemetry' updates (Real-time movement/stats)
          connection.on("telemetry", (data: Robot) => {
             setRobots(prev => {
                const idx = prev.findIndex(r => r.name === data.name);
                if (idx === -1) {
                   // If we get telemetry for a robot we don't know yet, add it
                   return [...prev, data];
                }
                const updated = [...prev];
                updated[idx] = { ...updated[idx], ...data };
                return updated;
             });
          });

        })
        .catch(err => {
          console.error("SignalR Connection Failed: ", err);
          setError("Real-time stream unavailable. Checking connection...");
        });
        
        return () => {
           connection.stop();
        };
    }
  }, [connection]);

  if (loading && robots.length === 0) {
    return (
      <div className="flex items-center justify-center h-[500px]">
        <div className="flex flex-col items-center gap-4">
          <div className="w-8 h-8 rounded-full border-2 border-primary border-t-transparent animate-spin" />
          <p className="text-zinc-500 font-mono text-sm">CONNECTING TO SKYNET...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
         <div>
            <h1 className="text-3xl font-semibold tracking-tight text-white">Robot Fleet</h1>
            <p className="text-zinc-400 mt-1">Real-time telemetry for {robots.length} active units.</p>
         </div>
         <div className="flex gap-2">
            <span className={`px-3 py-1 rounded-full text-xs font-mono border flex items-center gap-2 transition-colors ${
               error ? 'bg-rose-500/10 text-rose-500 border-rose-500/20' : 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20'
            }`}>
               <div className={`w-1.5 h-1.5 rounded-full ${error ? 'bg-rose-500' : 'bg-emerald-400 animate-pulse'}`} />
               {error ? 'STREAM OFFLINE' : 'LIVE FEED'}
            </span>
         </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        {robots.map((robot) => (
          <div key={robot.name} className="glass-card rounded-2xl p-6 group hover:border-zinc-600 transition-all duration-300">
            {/* Header */}
            <div className="flex justify-between items-start mb-6">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-xl bg-zinc-800 flex items-center justify-center group-hover:bg-primary/20 group-hover:text-primary transition-colors">
                  <Bot size={20} />
                </div>
                <div>
                  <h3 className="text-white font-medium text-lg">{robot.name}</h3>
                  <p className="text-zinc-500 text-xs font-mono">{robot.ip}</p>
                </div>
              </div>
              <div className={`px-2 py-1 rounded text-[10px] font-mono uppercase border ${
                !robot.connected ? 'bg-zinc-800 text-zinc-500 border-zinc-700' :
                robot.state === 'idle' ? 'bg-amber-500/10 text-amber-500 border-amber-500/20' : 
                robot.state === 'active' ? 'bg-emerald-500/10 text-emerald-500 border-emerald-500/20' :
                'bg-zinc-800 text-zinc-400 border-zinc-700'
              }`}>
                {!robot.connected ? 'OFFLINE' : (robot.state || 'unknown')}
              </div>
            </div>

            {/* Stats Grid */}
            <div className="grid grid-cols-2 gap-4 mb-6">
               <div className="p-3 bg-white/5 rounded-xl">
                  <div className="flex items-center gap-2 mb-2 text-zinc-400">
                     <Battery size={16} />
                     <span className="text-xs">Battery</span>
                  </div>
                  <div className="text-xl font-bold text-white">{robot.battery}%</div>
                  <div className="h-1 bg-zinc-700 rounded-full mt-2 overflow-hidden">
                     <div 
                       className={`h-full ${robot.battery > 20 ? 'bg-emerald-500' : 'bg-rose-500'}`} 
                       style={{ width: `${robot.battery}%` }} 
                     />
                  </div>
               </div>

               <div className="p-3 bg-white/5 rounded-xl">
                  <div className="flex items-center gap-2 mb-2 text-zinc-400">
                     <MapPin size={16} />
                     <span className="text-xs">Position</span>
                  </div>
                  <div className="text-xs font-mono text-white space-y-1">
                     <div className="flex justify-between">
                       <span className="text-zinc-500">X:</span> {robot.x.toFixed(2)}
                     </div>
                     <div className="flex justify-between">
                       <span className="text-zinc-500">Y:</span> {robot.y.toFixed(2)}
                     </div>
                  </div>
               </div>
            </div>

            {/* Footer */}
            <div className="flex items-center justify-between pt-4 border-t border-white/5">
               <div className={`flex items-center gap-2 text-xs transition-colors ${robot.connected ? 'text-emerald-400' : 'text-zinc-500'}`}>
                  {robot.connected ? (
                    <>
                      <Wifi size={14} />
                      <span>Signal Strong</span>
                    </>
                  ) : (
                    <>
                      <WifiOff size={14} className="text-zinc-600" />
                      <span className="text-zinc-500">
                        {robot.lastActive 
                          ? `Last active: ${new Date(robot.lastActive).toLocaleTimeString()}`
                          : 'Offline'}
                      </span>
                    </>
                  )}
               </div>
               <button className="text-xs text-primary hover:text-white transition-colors">
                  View Details →
               </button>
            </div>
          </div>
        ))}

        {/* Empty State / Add New Placeholder */}
        <button className="glass-card rounded-2xl p-6 border-dashed border-zinc-800 hover:border-zinc-600 hover:bg-white/5 transition-all flex flex-col items-center justify-center gap-4 text-zinc-500 hover:text-zinc-300 min-h-[280px]">
           <div className="w-12 h-12 rounded-full bg-zinc-900 border border-zinc-800 flex items-center justify-center">
              <span className="text-2xl font-light">+</span>
           </div>
           <span className="text-sm font-medium">Connect New Unit</span>
        </button>
      </div>
    </div>
  );
}
