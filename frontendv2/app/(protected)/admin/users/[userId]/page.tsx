import { ApiRoutes } from "@/lib/api/routes";
import type { UserDto } from "@/lib/api/types";
import { backendFetch } from "@/lib/api/backendClient";
import AdminUserEditor from "@/components/admin-user-editor";

export const dynamic = "force-dynamic";

export default async function AdminUserDetailPage(props: { params: Promise<{ userId: string }> }) {
  const { userId } = await props.params;
  const res = await backendFetch(ApiRoutes.adminUsers.byId(userId), { method: "GET" });
  if (!res.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">User</h1>
        <div className="text-sm text-red-600">Failed to load user</div>
      </div>
    );
  }

  const user = (await res.json()) as UserDto;

  return (
    <div className="flex flex-col gap-4">
      <h1 className="text-2xl font-semibold">User</h1>
      <div className="rounded-md border border-zinc-200 p-4 text-sm dark:border-zinc-800">
        <div>
          <span className="font-medium">UserId:</span> {user.userId}
        </div>
        <div>
          <span className="font-medium">Username:</span> {user.username}
        </div>
        <div>
          <span className="font-medium">Display name:</span> {user.displayName}
        </div>
        <div>
          <span className="font-medium">Roles:</span> {user.roles.join(", ")}
        </div>
        <div>
          <span className="font-medium">Disabled:</span> {user.isDisabled ? "true" : "false"}
        </div>
      </div>

      <AdminUserEditor user={user} />
    </div>
  );
}
