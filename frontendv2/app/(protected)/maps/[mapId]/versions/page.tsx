import Link from "next/link";

import { backendFetch } from "@/lib/api/backendClient";
import { ApiRoutes } from "@/lib/api/routes";
import type { MapDto, MapVersionDto } from "@/lib/api/types";
import { canWrite } from "@/lib/auth/guards";
import { getSession } from "@/lib/auth/session";
import ActivateVersionButton from "@/components/maps/activate-version-button";

export const dynamic = "force-dynamic";

export default async function MapVersionsPage(props: { params: Promise<{ mapId: string }> }) {
  const { mapId } = await props.params;
  const session = await getSession();
  const write = canWrite(session?.roles ?? []);

  const mapRes = await backendFetch(ApiRoutes.maps.byId(mapId), { method: "GET" });
  const map = mapRes.ok ? ((await mapRes.json()) as MapDto) : null;

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
  const active = map?.activePublishedMapVersionId ?? null;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold">Versions</h1>
          <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
            mapId: {mapId} {active ? `• active: ${active.slice(0, 8)}` : "• active: (none)"}
          </div>
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
                {active && active === v.mapVersionId ? <span className="ml-2 rounded-full bg-emerald-600/15 px-2 py-0.5 text-xs text-emerald-600">ACTIVE</span> : null}
              </div>
              <div className="text-xs text-zinc-600 dark:text-zinc-400 font-mono">{v.mapVersionId}</div>
              <div className="text-xs text-zinc-600 dark:text-zinc-400">
                createdAt: <span className="font-mono">{v.createdAt}</span>
                {v.publishedAt ? (
                  <>
                    {" "}
                    • publishedAt: <span className="font-mono">{v.publishedAt}</span>
                  </>
                ) : null}
              </div>
              {v.label ? <div className="text-xs text-zinc-600 dark:text-zinc-400">{v.label}</div> : null}
              {v.changeSummary ? <div className="text-xs text-zinc-600 dark:text-zinc-400">{v.changeSummary}</div> : null}
            </div>
            <div className="flex items-center gap-3 text-sm">
              <Link className="underline" href={`/maps/${mapId}/versions/${v.mapVersionId}`}>
                View
              </Link>
              {write && v.status === "PUBLISHED" && active !== v.mapVersionId ? (
                <ActivateVersionButton mapId={mapId} mapVersionId={v.mapVersionId} />
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

