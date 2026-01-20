"use client";

import { HubConnection, HubConnectionBuilder } from "@microsoft/signalr";
import { useEffect, useState } from "react";

function getBackendBaseUrl(): string {
  const base = process.env.NEXT_PUBLIC_BACKEND_BASE_URL ?? "http://localhost:5000";
  return base.replace(/\/$/, "");
}

export function useSignalR() {
  const [connection] = useState<HubConnection>(() => {
    const hubUrl = `${getBackendBaseUrl()}/hubs/realtime`;
    return new HubConnectionBuilder().withUrl(hubUrl, { withCredentials: true }).withAutomaticReconnect().build();
  });
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    connection
      .start()
      .then(() => setIsConnected(true))
      .catch(() => setIsConnected(false));

    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));

    return () => {
      void connection.stop();
    };
  }, [connection]);

  return { connection, isConnected };
}
