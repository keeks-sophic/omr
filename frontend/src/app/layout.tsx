import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import UiGate from "./components/UiGate";
import NavSidebar from "./components/NavSidebar";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Skylink Frontend",
  description: "Next-gen Robotics Control Interface",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {

  return (
    <html lang="en" className="dark">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased bg-background text-foreground min-h-screen flex flex-row overflow-hidden selection:bg-primary selection:text-white`}
      >
        {/* Floating Sidebar */}
        <UiGate hideOnPaths={["/login", "/logout", "/register"]}>
        <NavSidebar />
        </UiGate>

        {/* Main Content Area */}
        <main className="flex-1 relative overflow-y-auto overflow-x-hidden p-6">
           {/* Top Bar (Minimal) */}
           <UiGate hideOnPaths={["/login", "/logout", "/register"]}>
           <header className="fixed top-0 left-20 lg:left-64 right-0 h-16 z-40 flex items-center justify-between px-8 pointer-events-none">
              <div className="glass px-4 py-1.5 rounded-full pointer-events-auto flex items-center gap-2">
                 <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                 <span className="text-xs font-mono text-zinc-400">SYSTEM NOMINAL</span>
              </div>
           </header>
           </UiGate>

           <div className="mt-12 w-full max-w-[1600px] mx-auto pb-10 animate-in fade-in duration-500 slide-in-from-bottom-4">
            {children}
           </div>
        </main>
      </body>
    </html>
  );
}
