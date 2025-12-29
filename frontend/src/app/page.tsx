import { Activity, Battery, Signal, Wifi, Clock, ArrowUpRight } from "lucide-react";

export default function Home() {
  return (
    <div className="space-y-6">
      <div className="flex items-end justify-between">
        <div>
          <h1 className="text-3xl font-semibold tracking-tight text-white">Dashboard</h1>
          <p className="text-zinc-400 mt-1">Overview of system telemetry and active units.</p>
        </div>
        <div className="flex gap-2">
           <button className="px-4 py-2 bg-white/5 hover:bg-white/10 text-zinc-300 rounded-full text-sm font-medium transition-colors">
              Export Data
           </button>
           <button className="px-4 py-2 bg-primary hover:bg-primary-hover text-white rounded-full text-sm font-medium shadow-lg shadow-primary/20 transition-all">
              + New Mission
           </button>
        </div>
      </div>

      {/* Bento Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 auto-rows-[180px]">
        
        {/* Large Main Stats */}
        <div className="glass-card rounded-2xl p-6 col-span-1 md:col-span-2 row-span-2 relative overflow-hidden group">
          <div className="absolute top-0 right-0 p-6 opacity-50 group-hover:opacity-100 transition-opacity">
            <ArrowUpRight className="text-zinc-500" />
          </div>
          <div className="h-full flex flex-col justify-between relative z-10">
            <div className="flex items-center gap-3">
               <div className="p-2 bg-emerald-500/10 rounded-lg">
                  <Activity className="text-emerald-400" size={24} />
               </div>
               <span className="text-zinc-400 font-medium uppercase tracking-wider text-xs">System Health</span>
            </div>
            <div>
              <div className="text-5xl font-bold text-white tracking-tighter">98.4%</div>
              <p className="text-emerald-400 mt-2 flex items-center gap-1 text-sm">
                <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
                All systems nominal
              </p>
            </div>
            {/* Fake Graph Line */}
            <div className="w-full h-12 flex items-end gap-1 opacity-50">
               {[40, 60, 45, 70, 85, 60, 75, 50, 65, 80, 95, 85, 90].map((h, i) => (
                  <div key={i} style={{ height: `${h}%` }} className="flex-1 bg-gradient-to-t from-emerald-500/50 to-emerald-400/20 rounded-t-sm" />
               ))}
            </div>
          </div>
        </div>

        {/* Small Stat Cards */}
        <div className="glass-card rounded-2xl p-5 flex flex-col justify-between hover:bg-white/5 transition-colors">
           <div className="flex justify-between items-start">
              <div className="p-2 bg-sky-500/10 rounded-lg">
                 <Wifi className="text-sky-400" size={20} />
              </div>
              <span className="text-xs text-zinc-500 font-mono">NET-01</span>
           </div>
           <div>
              <div className="text-2xl font-bold text-white">24ms</div>
              <div className="text-xs text-zinc-500 mt-1">Latency (avg)</div>
           </div>
        </div>

        <div className="glass-card rounded-2xl p-5 flex flex-col justify-between hover:bg-white/5 transition-colors">
           <div className="flex justify-between items-start">
              <div className="p-2 bg-amber-500/10 rounded-lg">
                 <Battery className="text-amber-400" size={20} />
              </div>
              <span className="text-xs text-zinc-500 font-mono">PWR-A</span>
           </div>
           <div>
              <div className="text-2xl font-bold text-white">82%</div>
              <div className="text-xs text-zinc-500 mt-1">Battery Level</div>
           </div>
        </div>

        {/* Wide Card */}
        <div className="glass-card rounded-2xl p-6 col-span-1 md:col-span-2 flex flex-col justify-between">
           <div className="flex items-center justify-between mb-4">
              <h3 className="text-zinc-200 font-medium">Active Units</h3>
              <span className="text-xs text-zinc-500 bg-zinc-800/50 px-2 py-1 rounded-full">3 Online</span>
           </div>
           <div className="space-y-3">
              {[
                 { name: "Rover-Alpha", status: "Patrol", batt: "88%", loc: "Sector 4" },
                 { name: "Drone-X1", status: "Idle", batt: "100%", loc: "Base" },
                 { name: "Arm-Z7", status: "Working", batt: "45%", loc: "Sector 2" },
              ].map((unit, i) => (
                 <div key={i} className="flex items-center justify-between p-3 rounded-xl bg-white/5 hover:bg-white/10 transition-colors cursor-pointer group">
                    <div className="flex items-center gap-3">
                       <div className={`w-2 h-2 rounded-full ${unit.status === 'Idle' ? 'bg-amber-400' : 'bg-emerald-400'}`} />
                       <span className="text-sm font-medium text-zinc-200">{unit.name}</span>
                    </div>
                    <div className="flex items-center gap-6 text-sm text-zinc-500 font-mono">
                       <span>{unit.loc}</span>
                       <span className="text-zinc-400">{unit.batt}</span>
                    </div>
                 </div>
              ))}
           </div>
        </div>

        {/* Tall Card */}
        <div className="glass-card rounded-2xl p-6 row-span-2 flex flex-col">
           <div className="flex items-center gap-2 mb-6">
              <Clock className="text-violet-400" size={20} />
              <h3 className="text-zinc-200 font-medium">Recent Logs</h3>
           </div>
           <div className="space-y-6 relative pl-2">
              {/* Timeline Line */}
              <div className="absolute left-[11px] top-2 bottom-2 w-[1px] bg-zinc-800" />
              
              {[
                 { time: "10:42:05", msg: "Mission started", type: "info" },
                 { time: "10:45:12", msg: "Waypoint A reached", type: "success" },
                 { time: "10:48:30", msg: "Obstacle detected", type: "warn" },
                 { time: "10:50:01", msg: "Rerouting...", type: "info" },
              ].map((log, i) => (
                 <div key={i} className="relative pl-6">
                    <div className={`absolute left-0 top-1.5 w-6 h-6 rounded-full bg-zinc-950 border-2 z-10 flex items-center justify-center
                       ${log.type === 'info' ? 'border-sky-500/50' : 
                         log.type === 'success' ? 'border-emerald-500/50' : 'border-amber-500/50'}`}
                    >
                       <div className={`w-1.5 h-1.5 rounded-full 
                          ${log.type === 'info' ? 'bg-sky-400' : 
                            log.type === 'success' ? 'bg-emerald-400' : 'bg-amber-400'}`} 
                       />
                    </div>
                    <div className="text-xs text-zinc-500 font-mono mb-0.5">{log.time}</div>
                    <div className="text-sm text-zinc-300">{log.msg}</div>
                 </div>
              ))}
           </div>
        </div>

        {/* Filler Card */}
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
