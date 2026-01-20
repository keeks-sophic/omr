import Link from "next/link";

import MapEditor from "@/components/maps/map-editor";
import { backendFetch } from "@/lib/api/backendClient";
import { ApiRoutes } from "@/lib/api/routes";
import type { MapSnapshotDto, MapVersionDto } from "@/lib/api/types";

export const dynamic = "force-dynamic";

export default async function MapEditPage(props: { params: Promise<{ mapId: string }> }) {
  const { mapId } = await props.params;

  const draftRes = await backendFetch(ApiRoutes.maps.draft(mapId), { method: "GET" });
  if (!draftRes.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Map editor</h1>
        <div className="text-sm text-red-600">Failed to load draft</div>
      </div>
    );
  }

  const draft = (await draftRes.json()) as MapVersionDto;
  const snapRes = await backendFetch(ApiRoutes.maps.snapshot(mapId, draft.mapVersionId), { method: "GET" });
  if (!snapRes.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Map editor</h1>
        <div className="text-sm text-red-600">Failed to load draft snapshot</div>
      </div>
    );
  }
  const snapshot = (await snapRes.json()) as MapSnapshotDto;

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between gap-3">
        <div className="text-sm text-zinc-600 dark:text-zinc-400">
          <Link className="underline" href={`/maps/${mapId}`}>
            Back to map
          </Link>
        </div>
        <div className="text-sm text-zinc-600 dark:text-zinc-400">
          Draft v{draft.version} • {draft.mapVersionId.slice(0, 8)}
        </div>
      </div>
      <MapEditor mapId={mapId} draftVersion={draft} initialSnapshot={snapshot} />
    </div>
  );
}

