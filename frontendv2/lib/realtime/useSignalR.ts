"use client";

import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useEffect, useState } from "react";

function getBackendBaseUrl(): string {
  const base = process.env.NEXT_PUBLIC_BACKEND_BASE_URL ?? "http://localhost:5000";
  return base.replace(/\/$/, "");
}

let shared: HubConnection | null = null;
let subscribers = 0;
let startPromise: Promise<void> | null = null;

function isBenignStartStopError(err: unknown) {
  if (!(err instanceof Error)) return false;
  return err.message.includes("stopped during negotiation");
}

function getOrCreateConnection() {
  if (shared) return shared;
  const hubUrl = `${getBackendBaseUrl()}/hubs/realtime`;
  shared = new HubConnectionBuilder()
    .withUrl(hubUrl, { withCredentials: true })
    .withAutomaticReconnect()
    .configureLogging({
      log: (level, message) => {
        if (message.includes("stopped during negotiation")) return;
        if (level === LogLevel.Error) console.error(message);
        else if (level === LogLevel.Warning) console.warn(message);
        else if (level === LogLevel.Information) console.info(message);
        else console.debug(message);
      },
    })
    .build();
  return shared;
}

export function useSignalR() {
  const [connection] = useState<HubConnection | null>(() => {
    if (typeof window === "undefined") return null;
    return getOrCreateConnection();
  });
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    if (!connection) return;
    subscribers += 1;

    if (!startPromise) {
      startPromise = connection
        .start()
        .then(() => setIsConnected(true))
        .catch((err) => {
          if (!isBenignStartStopError(err)) setIsConnected(false);
        })
        .finally(() => {
          startPromise = null;
        });
    }

    connection.onclose(() => setIsConnected(false));
    connection.onreconnected(() => setIsConnected(true));

    return () => {
      subscribers -= 1;
      if (subscribers <= 0) {
        subscribers = 0;
        void connection.stop();
      }
    };
  }, [connection]);

  return { connection, isConnected };
}
