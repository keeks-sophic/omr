import { useEffect, useState, useCallback } from "react";
import { HubConnectionBuilder, HubConnection, HubConnectionState } from "@microsoft/signalr";
import { Robot, RobotCommand } from "../types";

const HUB_URL = "http://localhost:5146/hub/robots";

export function useRobotFleet() {
  const [robots, setRobots] = useState<Robot[]>([]);
  const [loading, setLoading] = useState(true);
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);
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

  return { robots, loading, error, isConnected, sendCommand };
}
