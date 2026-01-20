import Link from "next/link";

import LogoutButton from "@/components/logout-button";
import { getSession } from "@/lib/auth/session";

export default async function ProtectedLayout({ children }: { children: React.ReactNode }) {
  const session = await getSession();
  const isAdmin = session?.roles.includes("Admin") ?? false;

  return (
    <div className="min-h-screen">
      <header className="border-b border-zinc-200 dark:border-zinc-800">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-6 py-4">
          <nav className="flex items-center gap-4 text-sm">
            <Link href="/" className="font-medium">
              Dashboard
            </Link>
            <Link href="/account">Account</Link>
            {isAdmin ? <Link href="/admin/users">Admin</Link> : null}
          </nav>
          <LogoutButton />
        </div>
      </header>
      <main className="mx-auto max-w-5xl px-6 py-8">{children}</main>
    </div>
  );
}
