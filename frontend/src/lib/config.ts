export function getApiBaseUrl() {
  const url = process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5146";
  return url.replace(/\/+$/, "");
}

export function getRealtimeHubUrl() {
  return `${getApiBaseUrl()}/hubs/realtime`;
}
