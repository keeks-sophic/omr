"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getApiBaseUrl } from "../../lib/config";
import { fetchMe, clearToken } from "../../lib/auth";

type Me = {
  userId: string;
  roles: string[];
  allowedRobotIds?: string[];
};

export default function ProfilePage() {
  const [me, setMe] = useState<Me | null>(null);
  const [error, setError] = useState<string | null>(null);
  const router = useRouter();

  useEffect(() => {
    async function load() {
      try {
        const data = await fetchMe(getApiBaseUrl());
        setMe(data);
      } catch {
        setError("Failed to load profile");
      }
    }
    load();
  }, []);

  async function handleLogout() {
    try {
      const baseUrl = getApiBaseUrl();
      const token = typeof window !== "undefined" ? localStorage.getItem("auth_token") : null;
      if (token) {
        await fetch(`${baseUrl}/api/v1/auth/logout`, {
          method: "POST",
          headers: { Authorization: `Bearer ${token}` },
        });
      }
    } catch {}
    clearToken();
    router.replace("/login");
  }

  return (
    <div className="max-w-xl mx-auto space-y-6">
      <h1 className="text-2xl font-semibold">Profile</h1>
      {error && <p className="text-sm text-red-400">{error}</p>}
      {me && (
        <div className="glass rounded-2xl p-4 space-y-2">
          <p className="text-sm text-zinc-400">
            User ID: <span className="font-mono text-zinc-200">{me.userId}</span>
          </p>
          <p className="text-sm text-zinc-400">
            Roles: <span className="font-mono text-zinc-200">{me.roles.join(", ") || "None"}</span>
          </p>
          {me.allowedRobotIds && me.allowedRobotIds.length > 0 && (
            <p className="text-sm text-zinc-400">
              Allowed Robots:{" "}
              <span className="font-mono text-zinc-200">{me.allowedRobotIds.join(", ")}</span>
            </p>
          )}
        </div>
      )}
      <button
        onClick={handleLogout}
        className="inline-flex items-center justify-center px-4 py-2 rounded-xl border border-red-500/60 text-red-200 text-sm hover:bg-red-500/10 transition-colors"
      >
        Logout
      </button>
    </div>
  );
}

