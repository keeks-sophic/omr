import type { MeResponse } from "@/lib/api/types";
import { ApiRoutes } from "@/lib/api/routes";
import { backendFetch } from "@/lib/api/backendClient";

export const dynamic = "force-dynamic";

export default async function AccountPage() {
  const res = await backendFetch(ApiRoutes.auth.me, { method: "GET" });
  if (!res.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Account</h1>
        <div className="text-sm text-red-600">Failed to load session</div>
      </div>
    );
  }

  const me = (await res.json()) as MeResponse;

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold">Account</h1>
      <div className="rounded-md border border-zinc-200 p-4 text-sm dark:border-zinc-800">
        <div>
          <span className="font-medium">UserId:</span> {me.userId}
        </div>
        <div>
          <span className="font-medium">Username:</span> {me.username}
        </div>
        <div>
          <span className="font-medium">Roles:</span> {me.roles.join(", ") || "(none)"}
        </div>
      </div>
    </div>
  );
}

