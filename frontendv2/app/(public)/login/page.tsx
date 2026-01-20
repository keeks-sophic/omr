import Link from "next/link";
import LoginForm from "@/components/login-form";

type SearchParams = Record<string, string | string[] | undefined>;

export default function LoginPage(props: { searchParams: SearchParams }) {
  const value = props.searchParams.returnTo;
  const returnTo = typeof value === "string" ? value : "/";
  return (
    <div className="mx-auto flex min-h-screen max-w-md flex-col justify-center px-6">
      <h1 className="text-2xl font-semibold">Login</h1>
      <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-400">
        Don&apos;t have an account?{" "}
        <Link href="/register" className="underline">
          Register
        </Link>
      </p>
      <LoginForm returnTo={returnTo} />
    </div>
  );
}
