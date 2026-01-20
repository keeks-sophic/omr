import Link from "next/link";

export default function PendingPage() {
  return (
    <div className="mx-auto flex min-h-screen max-w-lg flex-col justify-center px-6">
      <h1 className="text-2xl font-semibold">Pending approval</h1>
      <p className="mt-3 text-sm text-zinc-600 dark:text-zinc-400">
        Your account is created, but access is pending approval. You can still view your current
        session details.
      </p>
      <div className="mt-6 flex gap-4">
        <Link className="underline" href="/account">
          Go to account
        </Link>
        <Link className="underline" href="/login">
          Back to login
        </Link>
      </div>
    </div>
  );
}

