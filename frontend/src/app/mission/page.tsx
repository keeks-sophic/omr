import { ListTodo, CheckCircle2, Clock, MapPin, MoreHorizontal, Plus } from "lucide-react";

export default function MissionPage() {
  const tasks = [
    { id: "M-001", title: "Sector 4 Surveillance", status: "In Progress", progress: 65, priority: "High" },
    { id: "M-002", title: "Base Perimeter Check", status: "Pending", progress: 0, priority: "Medium" },
    { id: "M-003", title: "Sensor Calibration", status: "Completed", progress: 100, priority: "Low" },
  ];

  return (
    <div className="space-y-6">
       <div className="flex items-center justify-between">
         <div>
            <h1 className="text-3xl font-semibold tracking-tight text-white">Mission Control</h1>
            <p className="text-zinc-400 mt-1">Manage and track autonomous objectives.</p>
         </div>
         <button className="px-4 py-2 bg-primary hover:bg-primary-hover text-white rounded-full text-sm font-medium shadow-lg shadow-primary/20 transition-all flex items-center gap-2">
            <Plus size={18} />
            New Mission
         </button>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
         {/* Mission List */}
         <div className="lg:col-span-2 space-y-4">
            {tasks.map((task) => (
               <div key={task.id} className="glass-card p-5 rounded-2xl flex items-center justify-between group hover:border-zinc-700 transition-all">
                  <div className="flex items-center gap-4">
                     <div className={`w-10 h-10 rounded-full flex items-center justify-center border
                        ${task.status === 'Completed' ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-500' :
                          task.status === 'In Progress' ? 'bg-sky-500/10 border-sky-500/20 text-sky-500' :
                          'bg-zinc-800 border-zinc-700 text-zinc-500'}`}
                     >
                        {task.status === 'Completed' ? <CheckCircle2 size={20} /> : <Clock size={20} />}
                     </div>
                     <div>
                        <div className="flex items-center gap-2">
                           <h3 className="text-white font-medium">{task.title}</h3>
                           <span className="text-[10px] px-2 py-0.5 rounded bg-zinc-800 text-zinc-400 border border-zinc-700">{task.id}</span>
                        </div>
                        <div className="flex items-center gap-4 mt-1">
                           <span className={`text-xs ${
                              task.status === 'In Progress' ? 'text-sky-400' : 
                              task.status === 'Completed' ? 'text-emerald-400' : 'text-zinc-500'
                           }`}>{task.status}</span>
                           <span className="text-xs text-zinc-600">•</span>
                           <span className="text-xs text-zinc-500">{task.priority} Priority</span>
                        </div>
                     </div>
                  </div>

                  <div className="flex items-center gap-6">
                     {task.status === 'In Progress' && (
                        <div className="w-24">
                           <div className="flex justify-between text-[10px] text-zinc-500 mb-1">
                              <span>Progress</span>
                              <span>{task.progress}%</span>
                           </div>
                           <div className="h-1 bg-zinc-800 rounded-full overflow-hidden">
                              <div className="h-full bg-sky-500" style={{ width: `${task.progress}%` }} />
                           </div>
                        </div>
                     )}
                     <button className="p-2 hover:bg-white/10 rounded-lg text-zinc-500 hover:text-white transition-colors">
                        <MoreHorizontal size={20} />
                     </button>
                  </div>
               </div>
            ))}
         </div>

         {/* Sidebar / Details */}
         <div className="glass-card p-6 rounded-2xl h-fit">
            <h3 className="text-zinc-200 font-medium mb-6">Mission Summary</h3>
            <div className="space-y-6 relative pl-4 border-l border-zinc-800">
               {[
                  { step: "Initialize", status: "done" },
                  { step: "Pathfinding", status: "done" },
                  { step: "Execution", status: "active" },
                  { step: "Data Sync", status: "pending" },
                  { step: "Return", status: "pending" },
               ].map((item, i) => (
                  <div key={i} className="relative">
                     <div className={`absolute -left-[21px] top-1 w-2.5 h-2.5 rounded-full border-2 
                        ${item.status === 'done' ? 'bg-emerald-500 border-emerald-500' : 
                          item.status === 'active' ? 'bg-zinc-950 border-sky-500 animate-pulse' : 'bg-zinc-950 border-zinc-700'}`} 
                     />
                     <div className={`text-sm ${item.status === 'active' ? 'text-white font-medium' : 'text-zinc-500'}`}>
                        {item.step}
                     </div>
                  </div>
               ))}
            </div>
            
            <div className="mt-8 pt-6 border-t border-white/5">
               <div className="flex items-center gap-3 mb-4">
                  <MapPin size={18} className="text-zinc-500" />
                  <span className="text-sm text-zinc-300">Target Coordinates</span>
               </div>
               <div className="bg-black/30 p-3 rounded-lg font-mono text-xs text-emerald-400 border border-emerald-500/20">
                  LAT: 34.4210 N<br/>
                  LNG: 118.392 W
               </div>
            </div>
         </div>
      </div>
    </div>
  );
}
