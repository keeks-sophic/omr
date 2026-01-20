import Link from "next/link";

export default function AccessDeniedPage() {
  return (
    <div className="mx-auto flex min-h-screen max-w-lg flex-col justify-center px-6">
      <h1 className="text-2xl font-semibold">Access denied</h1>
      <p className="mt-3 text-sm text-zinc-600 dark:text-zinc-400">
        You are signed in, but you do not have permission to access this page.
      </p>
      <div className="mt-6 flex gap-4">
        <Link className="underline" href="/account">
          Account
        </Link>
        <Link className="underline" href="/">
          Dashboard
        </Link>
      </div>
    </div>
  );
}

