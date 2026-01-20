import { getToken } from "./auth";

export type OpsHealth = {
  status: string;
  components?: Record<string, string>;
};

export type JetstreamStats = {
  lag?: number;
  droppedMessages?: number;
  consumersHealthy?: boolean;
};

export type OpsAlert = {
  id: string;
  type: string;
  severity: "info" | "warn" | "error";
  message: string;
  timestamp: string;
};

export type AuditEvent = {
  id: string;
  actor: string;
  role: string;
  action: string;
  target?: string;
  outcome: "OK" | "ERROR" | "DENIED";
  timestamp: string;
};

export async function fetchOpsHealth(baseUrl: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/ops/health`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch ops health");
  return (await res.json()) as OpsHealth;
}

export async function fetchOpsJetstream(baseUrl: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/ops/jetstream`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch jetstream metrics");
  return (await res.json()) as JetstreamStats;
}

export async function fetchOpsAlerts(baseUrl: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/ops/alerts`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch alerts");
  return (await res.json()) as OpsAlert[];
}

export async function fetchOpsAudit(baseUrl: string) {
  const token = getToken();
  const res = await fetch(`${baseUrl}/api/v1/ops/audit`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });
  if (!res.ok) throw new Error("Failed to fetch audit events");
  return (await res.json()) as AuditEvent[];
}
