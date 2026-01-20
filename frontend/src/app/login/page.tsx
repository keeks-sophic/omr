"use client";

import { useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { getApiBaseUrl } from "../../lib/config";
import { login, setToken, fetchMe } from "../../lib/auth";

export default function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();
  const params = useSearchParams();

  async function handleLogin() {
    setLoading(true);
    setError(null);
    try {
      const baseUrl = getApiBaseUrl();
      const token = await login(baseUrl, username, password);
      setToken(token);
      await fetchMe(baseUrl, token);
      const next = params.get("next");
      router.replace(next || "/fleet");
    } catch (e) {
      setError("Invalid credentials or server unavailable");
    } finally {
      setLoading(false);
    }
  }

  return (
    <div style={{ display: "grid", placeItems: "center", minHeight: "100vh" }}>
      <div style={{ width: 360, padding: 24, borderRadius: 12, background: "rgba(255,255,255,0.05)", border: "1px solid rgba(255,255,255,0.1)" }}>
        <h1 style={{ color: "white", fontSize: 24, marginBottom: 8 }}>Sign In</h1>
        <p style={{ color: "#a1a1aa", marginBottom: 16 }}>Authenticate to access control and realtime streams</p>
        <div style={{ display: "grid", gap: 12 }}>
          <input
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            placeholder="Username"
            style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }}
          />
          <input
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Password"
            type="password"
            style={{ padding: 10, borderRadius: 8, border: "1px solid #27272a", background: "#0a0a0a", color: "white" }}
          />
          <button
            onClick={handleLogin}
            disabled={loading || !username || !password}
            style={{
              padding: 10,
              borderRadius: 8,
              border: "1px solid #22c55e",
              background: loading ? "#166534" : "#22c55e",
              color: "white",
              cursor: loading ? "not-allowed" : "pointer",
            }}
          >
            {loading ? "Signing in..." : "Sign In"}
          </button>
          {error && <div style={{ color: "#ef4444", fontSize: 12 }}>{error}</div>}
          <a href="/register" style={{ color: "#60a5fa", fontSize: 12, textDecoration: "underline", justifySelf: "center" }}>
            Create an account
          </a>
        </div>
      </div>
    </div>
  );
}
