"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { getApiBaseUrl } from "../../lib/config";
import { registerUser } from "../../lib/auth";

export default function RegisterPage() {
  const [username, setUsername] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  async function handleSubmit() {
    setLoading(true);
    setError(null);
    try {
      const baseUrl = getApiBaseUrl();
      await registerUser(baseUrl, username, displayName, password);
      router.replace("/login");
    } catch (e) {
      setError("Failed to register user or not authorized");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ display: "grid", placeItems: "center", minHeight: "100vh" }}>
      <div style={{ width: 420, padding: 24, borderRadius: 12, background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)" }}>
        <h1 style={{ color: "white", fontSize: 24, marginBottom: 8 }}>Register User</h1>
        <p style={{ color: "#a1a1aa", marginBottom: 16 }}>Create a new user account</p>
        <div style={{ display: "grid", gap: 12 }}>
          <input value={username} onChange={(e) => setUsername(e.target.value)} placeholder="Username" style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }} />
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} placeholder="Display Name" style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }} />
          <input value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" type="password" style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }} />
          <button onClick={handleSubmit} disabled={loading || !username || !displayName || !password} style={{ padding: 10, borderRadius: 8, border: "1px solid #3b82f6", background: loading ? "#1e3a8a" : "#3b82f6", color: "white", cursor: loading ? "not-allowed" : "pointer" }}>
            {loading ? "Registering..." : "Register"}
          </button>
          {error && <div style={{ color: "#ef4444", fontSize: 12 }}>{error}</div>}
        </div>
      </div>
    </div>
  );
}
