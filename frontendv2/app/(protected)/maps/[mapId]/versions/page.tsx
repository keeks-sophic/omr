import Link from "next/link";

import { backendFetch } from "@/lib/api/backendClient";
import { ApiRoutes } from "@/lib/api/routes";
import type { MapVersionDto } from "@/lib/api/types";
import { canWrite } from "@/lib/auth/guards";
import { getSession } from "@/lib/auth/session";

export const dynamic = "force-dynamic";

export default async function MapVersionsPage(props: { params: Promise<{ mapId: string }> }) {
  const { mapId } = await props.params;
  const session = await getSession();
  const write = canWrite(session?.roles ?? []);

  const res = await backendFetch(ApiRoutes.maps.versions(mapId), { method: "GET" });
  if (!res.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Versions</h1>
        <div className="text-sm text-red-600">Failed to load versions</div>
      </div>
    );
  }

  const versions = (await res.json()) as MapVersionDto[];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Versions</h1>
          <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">mapId: {mapId}</div>
        </div>
        <div className="flex items-center gap-3 text-sm">
          <Link className="underline" href={`/maps/${mapId}`}>
            Back to map
          </Link>
          {write ? (
            <Link className="underline" href={`/maps/${mapId}/edit`}>
              Edit (draft)
            </Link>
          ) : null}
        </div>
      </div>

      <div className="divide-y divide-zinc-200 rounded-2xl border border-zinc-200 dark:divide-zinc-800 dark:border-zinc-800">
        {versions.map((v) => (
          <div key={v.mapVersionId} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
            <div className="flex flex-col">
              <div className="text-sm font-medium">
                v{v.version} • {v.status}
              </div>
              <div className="text-xs text-zinc-600 dark:text-zinc-400 font-mono">{v.mapVersionId}</div>
              {v.changeSummary ? <div className="text-xs text-zinc-600 dark:text-zinc-400">{v.changeSummary}</div> : null}
            </div>
            <div className="flex items-center gap-3 text-sm">
              <Link className="underline" href={`/maps/${mapId}/versions/${v.mapVersionId}`}>
                View
              </Link>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

