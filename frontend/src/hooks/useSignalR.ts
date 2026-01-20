import { useEffect, useState, useCallback } from "react";
import { HubConnectionBuilder, HubConnection, HubConnectionState } from "@microsoft/signalr";
import { RobotCommand } from "../types";
import { getRealtimeHubUrl } from "../lib/config";
import { getToken } from "../lib/auth";

export function useSignalR() {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const hubUrl = getRealtimeHubUrl();
    const newConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => getToken() ?? "",
      })
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (!connection) return;

    connection
      .start()
      .then(() => {
        setIsConnected(true);
      })
      .catch((err) => {
        console.error("SignalR Connection Failed: ", err);
        setIsConnected(false);
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

  return { connection, isConnected, sendCommand };
}
