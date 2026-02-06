import Link from "next/link";

import { backendFetch } from "@/lib/api/backendClient";
import { ApiRoutes } from "@/lib/api/routes";
import type { MapDto } from "@/lib/api/types";
import { canWrite } from "@/lib/auth/guards";
import { getSession } from "@/lib/auth/session";

export const dynamic = "force-dynamic";

export default async function MapsListPage() {
  const session = await getSession();
  const write = canWrite(session?.roles ?? []);

  const res = await backendFetch(ApiRoutes.maps.base, { method: "GET" });
  if (!res.ok) {
    return (
      <div className="flex flex-col gap-2">
        <h1 className="text-2xl font-semibold">Maps</h1>
        <div className="text-sm text-red-600">Failed to load maps</div>
      </div>
    );
  }

  const maps = (await res.json()) as MapDto[];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Maps</h1>
          <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Multi-map list</div>
        </div>
        {write ? (
          <Link
            href="/maps/new"
            className="rounded-xl bg-black px-4 py-2 text-sm font-medium text-white dark:bg-white dark:text-black"
          >
            Create map
          </Link>
        ) : null}
      </div>

      <div className="divide-y divide-zinc-200 rounded-2xl border border-zinc-200 dark:divide-zinc-800 dark:border-zinc-800">
        {maps.map((m) => (
          <div key={m.mapId} className="flex flex-wrap items-center justify-between gap-3 px-4 py-3">
            <div className="flex flex-col">
              <div className="flex items-center gap-2 text-sm font-medium">
                <span>{m.name}</span>
                {m.activePublishedMapVersionId ? (
                  <span className="rounded-full bg-emerald-600/15 px-2 py-0.5 text-xs text-emerald-600">PUBLISHED</span>
                ) : (
                  <span className="rounded-full bg-zinc-600/15 px-2 py-0.5 text-xs text-zinc-500">NO PUBLISH</span>
                )}
              </div>
              <div className="text-xs text-zinc-600 dark:text-zinc-400">
                mapId: <span className="font-mono">{m.mapId}</span>
              </div>
              <div className="text-xs text-zinc-600 dark:text-zinc-400">
                activePublishedMapVersionId: <span className="font-mono">{m.activePublishedMapVersionId ?? "(none)"}</span>
              </div>
              <div className="text-xs text-zinc-600 dark:text-zinc-400">
                updatedAt: <span className="font-mono">{m.updatedAt}</span>
              </div>
            </div>
            <div className="flex flex-wrap items-center gap-3 text-sm">
              <Link className="underline" href={`/maps/${m.mapId}`}>
                Open
              </Link>
              <Link className="underline" href={`/maps/${m.mapId}/versions`}>
                Versions
              </Link>
              {write ? (
                <Link className="underline" href={`/maps/${m.mapId}/edit`}>
                  Edit (draft)
                </Link>
              ) : null}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
