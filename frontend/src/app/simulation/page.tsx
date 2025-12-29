"use client";

import { useState, useRef, useEffect, useMemo, useCallback } from "react";
import { Play, Pause, RotateCcw, Map as MapIcon, Bot, Navigation, Target } from "lucide-react";
import { useSimulation } from "@/hooks/useSimulation";
import { useRobotFleet } from "@/hooks/useRobotFleet";
import { Point, Route } from "@/types";

export default function SimulationPage() {
  const { mockRobots, maps, createMockRobot, calculateRoute } = useSimulation();
  const { robots: onlineRobots } = useRobotFleet();

  // State
  const [selectedRobotId, setSelectedRobotId] = useState<string>("");
  const [selectedMapId, setSelectedMapId] = useState<string>("");
  const [destination, setDestination] = useState<Point | null>(null);
  const [route, setRoute] = useState<Route | null>(null);
  const [isSimulating, setIsSimulating] = useState(false);
  const [simProgress, setSimProgress] = useState(0);
  const [newRobotName, setNewRobotName] = useState("");
  const [isCreatingRobot, setIsCreatingRobot] = useState(false);

  // Refs for animation
  const animationRef = useRef<number>(0);
  const lastFrameTime = useRef<number>(0);

  // Derived State
  const allRobots = useMemo(() => [...mockRobots, ...onlineRobots], [mockRobots, onlineRobots]);
  const selectedRobot = useMemo(() => allRobots.find(r => r.ip === selectedRobotId || r.name === selectedRobotId), [allRobots, selectedRobotId]);
  const selectedMap = useMemo(() => maps.find(m => m.id === selectedMapId), [maps, selectedMapId]);

  // Helpers
  const handleMapClick = (e: React.MouseEvent<HTMLDivElement>) => {
    if (!selectedMap || !selectedRobot || isSimulating) return;

    const rect = e.currentTarget.getBoundingClientRect();
    const scaleX = selectedMap.width / rect.width;
    const scaleY = selectedMap.height / rect.height;

    const x = (e.clientX - rect.left) * scaleX;
    const y = (e.clientY - rect.top) * scaleY;

    setDestination({ x, y });
    setRoute(null); // Reset route when destination changes
  };

  const handleGenerateRoute = async () => {
    if (!selectedRobot || !destination) return;
    
    const start = { x: selectedRobot.x, y: selectedRobot.y };
    const generatedRoute = await calculateRoute(start, destination);
    setRoute(generatedRoute);
    setSimProgress(0);
  };

  const stopSimulation = useCallback(() => {
    setIsSimulating(false);
    if (animationRef.current) {
        cancelAnimationFrame(animationRef.current);
    }
  }, []);

  const animate = useCallback((time: number) => {
    const delta = time - lastFrameTime.current;
    lastFrameTime.current = time;

    setSimProgress(prev => {
      const newProgress = prev + (delta / 2000); // 2 seconds to complete for now
      if (newProgress >= 1) {
        // We cannot call stopSimulation here directly if it causes a state update during render? 
        // No, this is inside requestAnimationFrame callback, so it's fine.
        // But better to return 1 and handle stop in effect or here.
        // We'll just return 1 and let the effect clean up or use a separate effect.
        return 1;
      }
      return newProgress;
    });

    if (isSimulating) {
      animationRef.current = requestAnimationFrame(animate);
    }
  }, [isSimulating]); // Removed stopSimulation dependency to avoid cycles, handled via logic below

  // Watch for completion
  useEffect(() => {
      if (simProgress >= 1 && isSimulating) {
          stopSimulation();
      }
  }, [simProgress, isSimulating, stopSimulation]);

  const startSimulation = () => {
    if (!route) return;
    setIsSimulating(true);
    lastFrameTime.current = performance.now();
    animationRef.current = requestAnimationFrame(animate);
  };

  const resetSimulation = () => {
    stopSimulation();
    setSimProgress(0);
  };
  
  // Re-trigger animation loop if isSimulating changes
  useEffect(() => {
      if (isSimulating) {
          lastFrameTime.current = performance.now();
          animationRef.current = requestAnimationFrame(animate);
      } else {
          cancelAnimationFrame(animationRef.current);
      }
      return () => cancelAnimationFrame(animationRef.current);
  }, [isSimulating, animate]);

  // Calculate robot position based on progress
  const getRobotPosition = (): Point => {
      if (!selectedRobot) return { x: 0, y: 0 };
      if (!route || simProgress === 0) return { x: selectedRobot.x, y: selectedRobot.y };
      
      // Simple interpolation between start and end of the ENTIRE route for now
      // A better implementation would interpolate between specific segments
      
      // Total path length logic
      // For simplicity, let's just lerp between the first and last point of the route
      // or implement segment-based interpolation if time permits.
      
      // Let's do segment-based interpolation
      if (route.points.length < 2) return route.points[0];
      
      // This is a simplified "percentage of total points" approach
      // Real distance-based interpolation requires more math
      const totalPoints = route.points.length - 1;
      const scaledProgress = simProgress * totalPoints;
      const currentIndex = Math.floor(scaledProgress);
      const nextIndex = Math.min(currentIndex + 1, totalPoints);
      const segmentProgress = scaledProgress - currentIndex;
      
      const p1 = route.points[currentIndex];
      const p2 = route.points[nextIndex];
      
      return {
          x: p1.x + (p2.x - p1.x) * segmentProgress,
          y: p1.y + (p2.y - p1.y) * segmentProgress
      };
  };

  const currentPos = getRobotPosition();

  const handleCreateRobot = () => {
      if (newRobotName) {
          createMockRobot(newRobotName);
          setNewRobotName("");
          setIsCreatingRobot(false);
      }
  };

  return (
    <div className="space-y-6 h-full flex flex-col">
       <div className="flex items-center justify-between flex-shrink-0">
         <div>
            <h1 className="text-3xl font-semibold tracking-tight text-white">Simulation</h1>
            <p className="text-zinc-400 mt-1">Run scenarios in a virtual environment.</p>
         </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-4 gap-6 flex-1 min-h-0">
         {/* Main Viewport */}
         <div className="lg:col-span-3 bg-zinc-950 rounded-3xl border border-zinc-800 relative overflow-hidden flex flex-col">
             {/* Map Container */}
             <div 
                className="flex-1 relative cursor-crosshair overflow-hidden"
                onClick={handleMapClick}
             >
                {selectedMap ? (
                    <div 
                        className="absolute inset-0 m-auto bg-zinc-900/50 border border-zinc-800"
                        style={{ 
                            width: '100%', 
                            height: '100%',
                            maxWidth: '100%',
                            maxHeight: '100%',
                            aspectRatio: `${selectedMap.width} / ${selectedMap.height}`
                        }}
                    >
                         {/* Grid Background */}
                         <div className="absolute inset-0 bg-[linear-gradient(to_right,#27272a_1px,transparent_1px),linear-gradient(to_bottom,#27272a_1px,transparent_1px)] bg-[size:40px_40px] opacity-20 pointer-events-none" />
                         
                         {/* Robot */}
                         {selectedRobot && (
                             <div 
                                className="absolute w-8 h-8 -ml-4 -mt-4 bg-blue-500 rounded-full flex items-center justify-center shadow-[0_0_20px_rgba(59,130,246,0.5)] z-20 transition-all duration-75"
                                style={{ 
                                    left: `${(currentPos.x / selectedMap.width) * 100}%`, 
                                    top: `${(currentPos.y / selectedMap.height) * 100}%` 
                                }}
                             >
                                 <Bot size={16} className="text-white" />
                             </div>
                         )}

                         {/* Destination */}
                         {destination && (
                             <div 
                                className="absolute w-6 h-6 -ml-3 -mt-3 text-emerald-500 z-10"
                                style={{ 
                                    left: `${(destination.x / selectedMap.width) * 100}%`, 
                                    top: `${(destination.y / selectedMap.height) * 100}%` 
                                }}
                             >
                                 <Target size={24} />
                             </div>
                         )}

                         {/* Route Path */}
                         {route && (
                             <svg className="absolute inset-0 w-full h-full pointer-events-none z-0">
                                 <polyline 
                                    points={route.points.map(p => `${(p.x / selectedMap.width) * 100}%,${(p.y / selectedMap.height) * 100}%`).join(" ")}
                                    fill="none"
                                    stroke="#10b981"
                                    strokeWidth="2"
                                    strokeDasharray="4 4"
                                    className="opacity-50"
                                 />
                             </svg>
                         )}
                    </div>
                ) : (
                    <div className="absolute inset-0 flex items-center justify-center text-zinc-600 font-mono text-sm">
                        SELECT_MAP_TO_BEGIN
                    </div>
                )}
             </div>
             
             {/* Timeline Controls */}
             <div className="p-4 border-t border-zinc-800 bg-zinc-900/50 backdrop-blur-sm flex items-center gap-4">
                <button 
                    onClick={resetSimulation}
                    className="p-2 hover:bg-white/10 rounded-lg text-zinc-400 hover:text-white transition-colors"
                >
                   <RotateCcw size={20} />
                </button>
                
                <button 
                    onClick={isSimulating ? stopSimulation : startSimulation}
                    disabled={!route}
                    className={`w-12 h-12 rounded-full flex items-center justify-center text-white shadow-lg transition-all hover:scale-105 ${!route ? 'bg-zinc-700 opacity-50 cursor-not-allowed' : 'bg-primary hover:bg-primary-hover shadow-primary/30'}`}
                >
                   {isSimulating ? <Pause size={24} fill="currentColor" /> : <Play size={24} fill="currentColor" className="ml-1" />}
                </button>

                <div className="flex-1 px-4">
                   <div className="h-1.5 bg-zinc-800 rounded-full overflow-hidden relative">
                      <div 
                        className="absolute inset-y-0 left-0 bg-primary transition-all duration-100 ease-linear"
                        style={{ width: `${simProgress * 100}%` }}
                      />
                   </div>
                   <div className="flex justify-between mt-2 text-[10px] font-mono text-zinc-500">
                      <span>{(simProgress * 100).toFixed(0)}%</span>
                      <span>{route ? `${route.distance.toFixed(1)}m` : '--'}</span>
                   </div>
                </div>
             </div>
         </div>

         {/* Sidebar Controls */}
         <div className="space-y-4 overflow-y-auto">
            {/* Robot Selection */}
            <div className="glass-card p-5 rounded-2xl">
               <div className="flex items-center justify-between mb-4">
                   <h3 className="text-zinc-200 font-medium flex items-center gap-2">
                       <Bot size={16} className="text-primary" />
                       Robot
                   </h3>
                   <button 
                    onClick={() => setIsCreatingRobot(!isCreatingRobot)}
                    className="text-xs text-primary hover:text-primary-hover transition-colors"
                   >
                       {isCreatingRobot ? 'Cancel' : '+ New'}
                   </button>
               </div>
               
               {isCreatingRobot && (
                   <div className="mb-4 p-3 bg-zinc-900/50 rounded-xl border border-zinc-800">
                       <input 
                           type="text" 
                           placeholder="Robot Name"
                           value={newRobotName}
                           onChange={(e) => setNewRobotName(e.target.value)}
                           className="w-full bg-zinc-950 border border-zinc-800 rounded-lg px-3 py-2 text-sm text-zinc-300 focus:outline-none focus:border-primary mb-2"
                       />
                       <button 
                           onClick={handleCreateRobot}
                           className="w-full py-1.5 bg-primary hover:bg-primary-hover text-white text-xs font-medium rounded-lg transition-colors"
                       >
                           Create Mock Robot
                       </button>
                   </div>
               )}

               <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                  {allRobots.map((robot) => (
                     <div 
                        key={robot.ip || robot.name} 
                        onClick={() => setSelectedRobotId(robot.ip || robot.name)}
                        className={`flex items-center justify-between p-3 rounded-xl cursor-pointer transition-all border ${
                            (selectedRobotId === robot.ip || selectedRobotId === robot.name)
                            ? 'bg-primary/10 border-primary/50' 
                            : 'hover:bg-white/5 border-transparent'
                        }`}
                     >
                        <div>
                            <div className="text-sm text-zinc-200 font-medium">{robot.name}</div>
                            <div className="text-[10px] text-zinc-500 font-mono">{robot.isMock ? 'MOCK' : 'ONLINE'} • {robot.ip}</div>
                        </div>
                        <div className={`w-2 h-2 rounded-full ${robot.connected ? 'bg-emerald-500' : 'bg-zinc-700'}`} />
                     </div>
                  ))}
               </div>
            </div>

            {/* Map Selection */}
            <div className="glass-card p-5 rounded-2xl">
               <h3 className="text-zinc-200 font-medium mb-4 flex items-center gap-2">
                   <MapIcon size={16} className="text-accent" />
                   Map
               </h3>
               <div className="space-y-2">
                  {maps.map((map) => (
                     <div 
                        key={map.id}
                        onClick={() => setSelectedMapId(map.id)}
                        className={`p-3 rounded-xl cursor-pointer transition-all border ${
                            selectedMapId === map.id
                            ? 'bg-accent/10 border-accent/50' 
                            : 'hover:bg-white/5 border-transparent'
                        }`}
                     >
                        <div className="text-sm text-zinc-200 font-medium">{map.name}</div>
                        <div className="text-[10px] text-zinc-500 font-mono">{map.width}x{map.height}</div>
                     </div>
                  ))}
               </div>
            </div>
            
            {/* Action Panel */}
            <div className="glass-card p-5 rounded-2xl">
                <h3 className="text-zinc-200 font-medium mb-4 flex items-center gap-2">
                   <Navigation size={16} className="text-emerald-400" />
                   Actions
               </h3>
               
               <div className="space-y-3">
                   <div className="flex justify-between text-xs text-zinc-400">
                       <span>Destination</span>
                       <span className="font-mono text-zinc-200">
                           {destination ? `(${destination.x.toFixed(0)}, ${destination.y.toFixed(0)})` : 'Not Set'}
                       </span>
                   </div>
                   
                   <button 
                       onClick={handleGenerateRoute}
                       disabled={!selectedRobot || !selectedMap || !destination}
                       className="w-full py-2.5 bg-zinc-800 hover:bg-zinc-700 disabled:opacity-50 disabled:cursor-not-allowed text-white text-sm font-medium rounded-xl transition-colors flex items-center justify-center gap-2"
                   >
                       <Navigation size={14} />
                       Generate Route
                   </button>
               </div>
            </div>
         </div>
      </div>
    </div>
  );
}
