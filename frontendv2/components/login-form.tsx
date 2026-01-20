"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

export default function LoginForm({ returnTo }: { returnTo: string }) {
  const router = useRouter();

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setIsSubmitting(true);
    setError(null);

    const res = await fetch("/api/auth/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ username, password }),
    });

    setIsSubmitting(false);

    if (!res.ok) {
      setError("Login failed");
      return;
    }

    router.replace(returnTo);
  }

  return (
    <form className="mt-6 flex flex-col gap-4" onSubmit={onSubmit}>
      <label className="flex flex-col gap-1">
        <span className="text-sm">Username</span>
        <input
          className="rounded-md border border-zinc-200 bg-transparent px-3 py-2 dark:border-zinc-800"
          value={username}
          onChange={(e) => setUsername(e.target.value)}
          autoComplete="username"
          required
        />
      </label>

      <label className="flex flex-col gap-1">
        <span className="text-sm">Password</span>
        <input
          className="rounded-md border border-zinc-200 bg-transparent px-3 py-2 dark:border-zinc-800"
          type="password"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          autoComplete="current-password"
          required
        />
      </label>

      {error ? <div className="text-sm text-red-600">{error}</div> : null}

      <button
        className="rounded-md bg-black px-3 py-2 text-white disabled:opacity-60 dark:bg-white dark:text-black"
        type="submit"
        disabled={isSubmitting}
      >
        {isSubmitting ? "Signing in..." : "Sign in"}
      </button>
    </form>
  );
}

