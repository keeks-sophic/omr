"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { listUsers } from "../../lib/adminUsersApi";

type User = { userId: string; username: string; displayName: string; isDisabled: boolean; roles: string[] };

export default function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [error, setError] = useState<string | null>(null);

  async function load() {
    try {
      const data = await listUsers();
      setUsers(data);
    } catch (e) {
      setError("Failed to load users or not authorized");
    }
  }

  useEffect(() => {
    load();
  }, []);

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">Users</h1>
      {error && <p className="text-sm text-red-400">{error}</p>}
      <div className="glass rounded-2xl overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="text-zinc-400">
              <th className="text-left px-4 py-2">Username</th>
              <th className="text-left px-4 py-2">Display Name</th>
              <th className="text-left px-4 py-2">Roles</th>
              <th className="text-left px-4 py-2">Status</th>
              <th className="text-left px-4 py-2">Actions</th>
            </tr>
          </thead>
          <tbody>
            {users.map((u) => (
              <tr key={u.userId} className="border-t border-white/5">
                <td className="px-4 py-2">{u.username}</td>
                <td className="px-4 py-2">{u.displayName}</td>
                <td className="px-4 py-2">{u.roles.join(", ")}</td>
                <td className="px-4 py-2">{u.isDisabled ? "Disabled" : "Active"}</td>
                <td className="px-4 py-2">
                  <Link href={`/users/${encodeURIComponent(u.userId)}`} className="text-primary hover:underline">
                    Details
                  </Link>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
