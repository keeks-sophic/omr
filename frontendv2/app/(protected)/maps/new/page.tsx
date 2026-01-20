"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState } from "react";

export default function NewMapPage() {
  const router = useRouter();
  const [name, setName] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const canSubmit = useMemo(() => name.trim().length > 0, [name]);

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    if (!canSubmit) return;
    setIsSubmitting(true);
    setError(null);

    const res = await fetch("/api/maps", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name }),
    });

    setIsSubmitting(false);

    if (!res.ok) {
      setError("Create map failed");
      return;
    }

    const data = (await res.json()) as { mapId?: string };
    const mapId = data.mapId;
    if (!mapId) {
      setError("Create map failed (missing mapId)");
      return;
    }
    router.replace(`/maps/${mapId}/edit`);
  }

  return (
    <div className="mx-auto flex max-w-xl flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Create map</h1>
        <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">Creates a new map with an initial draft version.</div>
      </div>

      <form className="flex flex-col gap-4" onSubmit={submit}>
        <label className="flex flex-col gap-1">
          <span className="text-sm">Name</span>
          <input
            className="rounded-xl border border-zinc-200 bg-transparent px-3 py-2 dark:border-zinc-800"
            value={name}
            onChange={(e) => setName(e.target.value)}
            disabled={isSubmitting}
            required
          />
        </label>

        {error ? <div className="text-sm text-red-600">{error}</div> : null}

        <button
          type="submit"
          disabled={!canSubmit || isSubmitting}
          className="rounded-xl bg-black px-4 py-2 text-sm font-medium text-white disabled:opacity-60 dark:bg-white dark:text-black"
        >
          {isSubmitting ? "Creating..." : "Create"}
        </button>
      </form>
    </div>
  );
}
