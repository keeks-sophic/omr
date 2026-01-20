import Link from "next/link";

import MapViewer from "@/components/maps/map-viewer";
import { backendFetch } from "@/lib/api/backendClient";
import { ApiRoutes } from "@/lib/api/routes";
import type { MapSnapshotDto } from "@/lib/api/types";
import { canWrite } from "@/lib/auth/guards";
import { getSession } from "@/lib/auth/session";

export const dynamic = "force-dynamic";

export default async function MapVersionViewPage(props: { params: Promise<{ mapId: string; mapVersionId: string }> }) {
  const { mapId, mapVersionId } = await props.params;
  const session = await getSession();
  const write = canWrite(session?.roles ?? []);

  const res = await backendFetch(ApiRoutes.maps.snapshot(mapId, mapVersionId), { method: "GET" });
  if (!res.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Map version</h1>
        <div className="text-sm text-red-600">Failed to load snapshot</div>
      </div>
    );
  }
  const snapshot = (await res.json()) as MapSnapshotDto;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Map version</h1>
          <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            v{snapshot.version.version} • {snapshot.version.status} • {snapshot.version.mapVersionId.slice(0, 8)}
          </div>
        </div>
        <div className="flex items-center gap-3 text-sm">
          <Link className="underline" href={`/maps/${mapId}`}>
            Map
          </Link>
          <Link className="underline" href={`/maps/${mapId}/versions`}>
            Versions
          </Link>
          {write ? (
            <Link className="underline" href={`/maps/${mapId}/edit`}>
              Edit (draft)
            </Link>
          ) : null}
        </div>
      </div>

      <MapViewer snapshot={snapshot} />
    </div>
  );
}

