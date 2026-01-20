"use client";

import { useEffect, useState } from "react";
import { useParams } from "next/navigation";
import { listUsers, disableUser } from "../../../lib/adminUsersApi";

type User = { userId: string; username: string; displayName: string; isDisabled: boolean; roles: string[] };

export default function UserDetailPage() {
  const params = useParams<{ userId: string }>();
  const userId = params?.userId as string;
  const [user, setUser] = useState<User | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function load() {
    try {
      const users = await listUsers();
      const found = users.find((u: User) => u.userId === userId) || null;
      setUser(found);
      if (!found) setError("User not found");
    } catch {
      setError("Failed to load user or not authorized");
    }
  }

  useEffect(() => {
    load();
  }, [userId]);

  async function handleDisable() {
    if (!user) return;
    setSaving(true);
    try {
      await disableUser(user.userId);
      await load();
    } catch {
      setError("Failed to disable user");
    } finally {
      setSaving(false);
    }
  }

  if (!user) return <div className="space-y-6"><h1 className="text-2xl font-semibold">User</h1>{error && <p className="text-sm text-red-400">{error}</p>}</div>;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">User</h1>
      {error && <p className="text-sm text-red-400">{error}</p>}
      <div className="glass rounded-2xl p-4 space-y-2">
        <p className="text-sm text-zinc-400">Username: <span className="text-zinc-200">{user.username}</span></p>
        <p className="text-sm text-zinc-400">Display Name: <span className="text-zinc-200">{user.displayName}</span></p>
        <p className="text-sm text-zinc-400">Roles: <span className="font-mono text-zinc-200">{user.roles.join(", ")}</span></p>
        <p className="text-sm text-zinc-400">Status: <span className="text-zinc-200">{user.isDisabled ? "Disabled" : "Active"}</span></p>
      </div>
      <button
        onClick={handleDisable}
        disabled={saving || user.isDisabled}
        className="inline-flex items-center justify-center px-4 py-2 rounded-xl border border-amber-500/60 text-amber-200 text-sm hover:bg-amber-500/10 transition-colors disabled:opacity-50"
      >
        Disable User
      </button>
    </div>
  );
}
