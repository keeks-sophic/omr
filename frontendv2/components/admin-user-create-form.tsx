"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";

import AdminRoleSelector from "@/components/admin-role-selector";

const DEFAULT_ROLES = ["Pending"];

export default function AdminUserCreateForm() {
  const router = useRouter();

  const [username, setUsername] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [roles, setRoles] = useState<string[]>(DEFAULT_ROLES);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const canSubmit = useMemo(() => {
    return username.trim().length > 0 && displayName.trim().length > 0 && password.length > 0;
  }, [username, displayName, password]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!canSubmit) return;

    setIsSubmitting(true);
    setError(null);

    const res = await fetch("/api/admin/users", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        username,
        displayName,
        password,
        roles,
      }),
    });

    setIsSubmitting(false);

    if (!res.ok) {
      const text = await res.text();
      setError(text || "Create user failed");
      return;
    }

    setUsername("");
    setDisplayName("");
    setPassword("");
    setRoles(DEFAULT_ROLES);
    router.refresh();
  }

  return (
    <form className="flex flex-col gap-3 rounded-md border border-zinc-200 p-4 dark:border-zinc-800" onSubmit={submit}>
      <div className="text-sm font-medium">Create user</div>
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        <label className="flex flex-col gap-1">
          <span className="text-xs text-zinc-600 dark:text-zinc-400">Username</span>
          <input
            className="rounded-md border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            disabled={isSubmitting}
            required
          />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs text-zinc-600 dark:text-zinc-400">Display name</span>
          <input
            className="rounded-md border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
            value={displayName}
            onChange={(e) => setDisplayName(e.target.value)}
            disabled={isSubmitting}
            required
          />
        </label>
        <label className="flex flex-col gap-1">
          <span className="text-xs text-zinc-600 dark:text-zinc-400">Password</span>
          <input
            className="rounded-md border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={isSubmitting}
            required
          />
        </label>
      </div>

      <div className="flex flex-col gap-2">
        <div className="text-xs text-zinc-600 dark:text-zinc-400">Roles</div>
        <AdminRoleSelector value={roles} onChange={setRoles} disabled={isSubmitting} />
      </div>

      {error ? <div className="text-sm text-red-600">{error}</div> : null}

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={!canSubmit || isSubmitting}
          className="rounded-md bg-black px-3 py-2 text-sm text-white disabled:opacity-60 dark:bg-white dark:text-black"
        >
          {isSubmitting ? "Creating..." : "Create"}
        </button>
      </div>
    </form>
  );
}

