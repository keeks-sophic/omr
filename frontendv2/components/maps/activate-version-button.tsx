"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

export default function ActivateVersionButton(props: { mapId: string; mapVersionId: string; disabled?: boolean }) {
  const router = useRouter();
  const [busy, setBusy] = useState(false);

  return (
    <button
      type="button"
      disabled={props.disabled || busy}
      className="rounded-xl border border-zinc-200 px-3 py-1 text-xs font-medium text-zinc-700 disabled:opacity-60 dark:border-zinc-800 dark:text-zinc-200"
      onClick={async () => {
        setBusy(true);
        try {
          const res = await fetch(`/api/maps/${props.mapId}/versions/${props.mapVersionId}/activate`, { method: "POST" });
          if (!res.ok) return;
          router.refresh();
        } finally {
          setBusy(false);
        }
      }}
    >
      Set active
    </button>
  );
}

