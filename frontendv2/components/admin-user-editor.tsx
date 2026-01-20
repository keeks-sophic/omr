"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";

import AdminRoleSelector from "@/components/admin-role-selector";
import type { UserDto } from "@/lib/api/types";

export default function AdminUserEditor({ user }: { user: UserDto }) {
  const router = useRouter();

  const [displayName, setDisplayName] = useState(user.displayName);
  const [password, setPassword] = useState("");
  const [isDisabled, setIsDisabled] = useState<boolean>(user.isDisabled);
  const [roles, setRoles] = useState<string[]>(user.roles);
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const canSaveUser = useMemo(() => displayName.trim().length > 0, [displayName]);

  async function saveUser(e: React.FormEvent) {
    e.preventDefault();
    if (!canSaveUser) return;
    setIsSubmitting(true);
    setError(null);

    const res = await fetch(`/api/admin/users/${user.userId}`, {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        displayName,
        password: password.trim().length > 0 ? password : null,
        isDisabled,
      }),
    });

    setIsSubmitting(false);

    if (!res.ok) {
      const text = await res.text();
      setError(text || "Update failed");
      return;
    }

    setPassword("");
    router.refresh();
  }

  async function disableUser() {
    setIsSubmitting(true);
    setError(null);
    const res = await fetch(`/api/admin/users/${user.userId}/disable`, { method: "POST" });
    setIsSubmitting(false);
    if (!res.ok) {
      const text = await res.text();
      setError(text || "Disable failed");
      return;
    }
    setIsDisabled(true);
    router.refresh();
  }

  async function replaceRoles() {
    setIsSubmitting(true);
    setError(null);
    const res = await fetch(`/api/admin/users/${user.userId}/roles`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ roles }),
    });
    setIsSubmitting(false);
    if (!res.ok) {
      const text = await res.text();
      setError(text || "Update roles failed");
      return;
    }
    router.refresh();
  }

  return (
    <div className="flex flex-col gap-6">
      <form className="flex flex-col gap-4 rounded-md border border-zinc-200 p-4 dark:border-zinc-800" onSubmit={saveUser}>
        <div className="text-sm font-medium">User settings</div>

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
          <span className="text-xs text-zinc-600 dark:text-zinc-400">New password (optional)</span>
          <input
            className="rounded-md border border-zinc-200 bg-transparent px-3 py-2 text-sm dark:border-zinc-800"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            disabled={isSubmitting}
          />
        </label>

        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={isDisabled}
            disabled={isSubmitting}
            onChange={(e) => setIsDisabled(e.target.checked)}
          />
          <span>Disabled</span>
        </label>

        <div className="flex items-center gap-3">
          <button
            type="submit"
            disabled={!canSaveUser || isSubmitting}
            className="rounded-md bg-black px-3 py-2 text-sm text-white disabled:opacity-60 dark:bg-white dark:text-black"
          >
            {isSubmitting ? "Saving..." : "Save"}
          </button>
          <button
            type="button"
            disabled={isSubmitting || isDisabled}
            onClick={disableUser}
            className="rounded-md border border-zinc-200 px-3 py-2 text-sm disabled:opacity-60 dark:border-zinc-800"
          >
            Disable user
          </button>
        </div>
      </form>

      <div className="flex flex-col gap-3 rounded-md border border-zinc-200 p-4 dark:border-zinc-800">
        <div className="text-sm font-medium">Roles</div>
        <AdminRoleSelector value={roles} onChange={setRoles} disabled={isSubmitting} />
        <div>
          <button
            type="button"
            disabled={isSubmitting}
            onClick={replaceRoles}
            className="rounded-md border border-zinc-200 px-3 py-2 text-sm disabled:opacity-60 dark:border-zinc-800"
          >
            {isSubmitting ? "Updating..." : "Replace roles"}
          </button>
        </div>

        {error ? <div className="text-sm text-red-600">{error}</div> : null}
      </div>
    </div>
  );
}

