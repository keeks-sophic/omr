import Link from "next/link";

import { backendFetch } from "@/lib/api/backendClient";
import { ApiRoutes } from "@/lib/api/routes";
import type { MapDto, MapSnapshotDto } from "@/lib/api/types";
import MapViewer from "@/components/maps/map-viewer";
import { canWrite } from "@/lib/auth/guards";
import { getSession } from "@/lib/auth/session";

export const dynamic = "force-dynamic";

export default async function MapViewPage(props: { params: Promise<{ mapId: string }> }) {
  const { mapId } = await props.params;
  const session = await getSession();
  const write = canWrite(session?.roles ?? []);

  const mapRes = await backendFetch(ApiRoutes.maps.byId(mapId), { method: "GET" });
  if (!mapRes.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Map</h1>
        <div className="text-sm text-red-600">Map not found</div>
      </div>
    );
  }
  const map = (await mapRes.json()) as MapDto;

  const active = map.activePublishedMapVersionId;
  if (!active) {
    return (
      <div className="flex flex-col gap-4">
        <div className="flex items-center justify-between gap-3">
          <div>
            <h1 className="text-2xl font-semibold">{map.name}</h1>
            <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">No published version yet.</div>
          </div>
          <div className="flex items-center gap-3 text-sm">
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

        <div className="rounded-2xl border border-zinc-200 p-6 text-sm text-zinc-600 dark:border-zinc-800 dark:text-zinc-400">
          Publish a draft to make a viewable version.
        </div>
      </div>
    );
  }

  const snapRes = await backendFetch(ApiRoutes.maps.snapshot(mapId, active), { method: "GET" });
  if (!snapRes.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">{map.name}</h1>
        <div className="text-sm text-red-600">Failed to load snapshot</div>
      </div>
    );
  }

  const snapshot = (await snapRes.json()) as MapSnapshotDto;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">{map.name}</h1>
          <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            Viewing published version {snapshot.version.version} • {snapshot.version.mapVersionId.slice(0, 8)}
          </div>
        </div>
        <div className="flex items-center gap-3 text-sm">
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
