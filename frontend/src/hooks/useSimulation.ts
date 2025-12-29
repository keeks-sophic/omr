import { useState, useCallback } from "react";
import { Robot, MapData, Point, Route } from "../types";

const MOCK_MAPS: MapData[] = [
  { 
      id: "map-1", 
      name: "Warehouse Alpha", 
      width: 800, 
      height: 600,
      nodes: [],
      paths: [],
      points: [],
      qrcodes: []
  },
  { 
      id: "map-2", 
      name: "Production Floor", 
      width: 1000, 
      height: 800,
      nodes: [],
      paths: [],
      points: [],
      qrcodes: []
  },
];

export function useSimulation() {
  const [mockRobots, setMockRobots] = useState<Robot[]>([
    { name: "SimBot-01", ip: "192.168.1.100", x: 100, y: 100, state: "IDLE", battery: 85, connected: true, isMock: true },
    { name: "SimBot-02", ip: "192.168.1.101", x: 200, y: 300, state: "IDLE", battery: 92, connected: true, isMock: true },
  ]);

  const [maps] = useState<MapData[]>(MOCK_MAPS);

  const createMockRobot = useCallback((name: string) => {
    const newRobot: Robot = {
      name,
      ip: `192.168.1.${100 + mockRobots.length}`,
      x: 50,
      y: 50,
      state: "IDLE",
      battery: 100,
      connected: true,
      isMock: true
    };
    setMockRobots(prev => [...prev, newRobot]);
  }, [mockRobots]);

  const calculateRoute = useCallback(async (start: Point, end: Point): Promise<Route> => {
    // Mock backend delay
    await new Promise(resolve => setTimeout(resolve, 800));

    // Simple logic: Create a path with 1-2 waypoints to simulate obstacle avoidance
    const distance = Math.sqrt(Math.pow(end.x - start.x, 2) + Math.pow(end.y - start.y, 2));
    
    const points = [start];
    
    // Add a simple "dogleg" waypoint if distance is significant
    if (distance > 100) {
        const midX = (start.x + end.x) / 2;
        const midY = (start.y + end.y) / 2;
        // Offset perpendicular to the path
        const offsetX = (end.y - start.y) * 0.2;
        const offsetY = -(end.x - start.x) * 0.2;
        
        points.push({ x: midX + offsetX, y: midY + offsetY });
    }
    
    points.push(end);

    return {
      points,
      distance,
      estimatedTime: distance / 100 // Mock speed units
    };
  }, []);

  return {
    mockRobots,
    maps,
    createMockRobot,
    calculateRoute
  };
}
