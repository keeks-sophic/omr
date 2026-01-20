"use client";

import { usePathname } from "next/navigation";

export default function UiGate(props: { hideOnPaths: string[]; children: React.ReactNode }) {
  const pathname = usePathname();
  const { hideOnPaths, children } = props;
  const hide = hideOnPaths.some((p) => pathname === p || pathname.startsWith(p));
  if (hide) return null;
  return <>{children}</>;
}
