import { useEffect, useState, useCallback } from "react";
import { HubConnectionBuilder, HubConnection, HubConnectionState } from "@microsoft/signalr";
import { Robot, RobotCommand } from "../types";

const HUB_URL = "http://localhost:5067/hub/robots";

export function useRobotFleet() {
  const [robots, setRobots] = useState<Robot[]>([]);
  const [loading, setLoading] = useState(true);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [routes, setRoutes] = useState<Record<string, { mapId: number; nodes: { id: number; x: number; y: number }[] }>>({});

  useEffect(() => {
    // 1. Initial Fetch to populate list
    const fetchInitialData = async () => {
      try {
        const res = await fetch("http://localhost:5067/robots");
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
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (connection) {
      connection.start()
        .then(() => {
          console.log("Connected to SignalR");
          setIsConnected(true);
          setError(null);

          // Handle 'identity' updates (New robot or IP change)
          connection.on("identity", (data: { name: string, ip: string }) => {
            setRobots(prev => {
              const idx = prev.findIndex(r => r.ip === data.ip);
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
              updated[idx] = { ...updated[idx], name: data.name, ip: data.ip };
              return updated;
            });
          });

          // Handle 'telemetry' updates (Real-time movement/stats)
          connection.on("telemetry", (data: Robot) => {
             setRobots(prev => {
                const idx = prev.findIndex(r => r.ip === data.ip);
                if (idx === -1) {
                   // If we get telemetry for a robot we don't know yet, add it
                   return [...prev, data];
                }
                const updated = [...prev];
                updated[idx] = { ...updated[idx], ...data };
                return updated;
             });
             setRoutes(prev => {
               if (!data.ip || !prev[data.ip]) return prev;
               const route = prev[data.ip];
               const last = route.nodes[route.nodes.length - 1];
               if (!last) return prev;
               const rx = typeof data.x === "number" ? data.x : 0;
               const ry = typeof data.y === "number" ? data.y : 0;
               const dx = rx - last.x;
               const dy = ry - last.y;
               const d = Math.sqrt(dx * dx + dy * dy);
               if (d <= 0.2) {
                 const { [data.ip]: _, ...rest } = prev;
                 return rest;
               }
               return prev;
             });
          });

          // Handle route overlay
          connection.on("route", (data: { ip: string; mapId: number; nodes: { id: number; x: number; y: number }[] }) => {
            if (data?.ip && Array.isArray(data.nodes)) {
              setRoutes(prev => ({ ...prev, [data.ip]: { mapId: data.mapId, nodes: data.nodes } }));
            }
          });

        })
        .catch(err => {
          console.error("SignalR Connection Failed: ", err);
          setIsConnected(false);
          setError("Real-time stream unavailable. Checking connection...");
        });
        
        connection.onclose(() => {
           setIsConnected(false);
        });

        connection.onreconnected(() => {
           setIsConnected(true);
        });

        return () => {
           connection.stop();
        };
    }
  }, [connection]);

  const sendCommand = useCallback(
    async (cmd: RobotCommand) => {
      if (connection && connection.state === HubConnectionState.Connected) {
        try {
          await connection.invoke("SendCommand", cmd);
          console.log("Command sent:", cmd);
        } catch (err) {
          console.error("Failed to send command:", err);
        }
      } else {
        console.warn("SignalR not connected. Cannot send command.");
      }
    },
    [connection]
  );

  const joinMap = useCallback(async (mapId: number) => {
    if (connection && connection.state === HubConnectionState.Connected) {
      try {
        await connection.invoke("JoinMap", mapId);
      } catch (err) {
        console.error("JoinMap failed:", err);
      }
    }
  }, [connection]);

  const leaveMap = useCallback(async (mapId: number) => {
    if (connection && connection.state === HubConnectionState.Connected) {
      try {
        await connection.invoke("LeaveMap", mapId);
      } catch (err) {
        console.error("LeaveMap failed:", err);
      }
    }
  }, [connection]);

  return { robots, loading, error, isConnected, sendCommand, joinMap, leaveMap, routes };
}
