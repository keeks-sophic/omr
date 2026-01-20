"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

export default function LogoutButton() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function logout() {
    setIsSubmitting(true);
    await fetch("/api/auth/logout", { method: "POST" });
    setIsSubmitting(false);
    router.replace("/login");
  }

  return (
    <button
      className="rounded-md border border-zinc-200 px-3 py-2 text-sm dark:border-zinc-800"
      onClick={logout}
      disabled={isSubmitting}
      type="button"
    >
      {isSubmitting ? "Signing out..." : "Logout"}
    </button>
  );
}

