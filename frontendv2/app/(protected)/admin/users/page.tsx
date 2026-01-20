import Link from "next/link";

import { ApiRoutes } from "@/lib/api/routes";
import type { UserDto } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";
import AdminUserCreateForm from "@/components/admin-user-create-form";

export const dynamic = "force-dynamic";

export default async function AdminUsersPage() {
  const res = await backendFetch(ApiRoutes.adminUsers.base, { method: "GET" });
  if (!res.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Users</h1>
        <div className="text-sm text-red-600">Failed to load users</div>
      </div>
    );
  }

  const users = (await res.json()) as UserDto[];

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold">Users</h1>
      <AdminUserCreateForm />
      <div className="divide-y divide-zinc-200 rounded-md border border-zinc-200 dark:divide-zinc-800 dark:border-zinc-800">
        {users.map((u) => (
          <div className="flex items-center justify-between px-4 py-3 text-sm" key={u.userId}>
            <div className="flex flex-col">
              <span className="font-medium">{u.username}</span>
              <span className="text-zinc-600 dark:text-zinc-400">{u.roles.join(", ")}</span>
            </div>
            <Link className="underline" href={`/admin/users/${u.userId}`}>
              Open
            </Link>
          </div>
        ))}
      </div>
    </div>
  );
}
