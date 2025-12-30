import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import Link from "next/link";
import { LayoutDashboard, Bot, Map as MapIcon, Play, Gamepad2, ListTodo, Layers } from "lucide-react";
import "./globals.css";

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
  const navItems = [
    { name: "Dashboard", href: "/", icon: LayoutDashboard },
    { name: "Robot", href: "/robot", icon: Bot },
    { name: "Map", href: "/map", icon: MapIcon },
    { name: "Visualise", href: "/visualise", icon: Layers },
    { name: "Simulation", href: "/simulation", icon: Play },
    { name: "Control", href: "/control", icon: Gamepad2 },
    { name: "Mission", href: "/mission", icon: ListTodo },
  ];

  return (
    <html lang="en" className="dark">
      <body
        className={`${geistSans.variable} ${geistMono.variable} antialiased bg-background text-foreground min-h-screen flex flex-row overflow-hidden selection:bg-primary selection:text-white`}
      >
        {/* Floating Sidebar */}
        <aside className="w-20 lg:w-64 flex-shrink-0 flex flex-col items-center lg:items-stretch py-6 px-3 z-50">
          <div className="glass rounded-3xl h-full flex flex-col p-4 shadow-xl shadow-black/20">
            {/* Logo Area */}
            <div className="flex items-center justify-center lg:justify-start gap-3 mb-8 px-2">
              <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-primary to-accent shadow-lg shadow-primary/30 flex-shrink-0" />
              <span className="hidden lg:block font-bold text-lg tracking-tight">Skylink</span>
            </div>

            {/* Navigation */}
            <nav className="flex-1 flex flex-col gap-2">
              {navItems.map((item) => (
                <Link
                  key={item.name}
                  href={item.href}
                  className="flex items-center gap-3 px-3 py-3 rounded-xl text-zinc-400 hover:text-white hover:bg-white/5 transition-all group relative"
                >
                  <item.icon size={22} strokeWidth={1.5} className="group-hover:scale-110 transition-transform duration-200" />
                  <span className="hidden lg:block font-medium text-sm tracking-wide">{item.name}</span>
                  
                  {/* Active Indicator (Simple Dot for now, can be dynamic later) */}
                  <div className="absolute left-0 w-1 h-0 bg-primary rounded-r-full transition-all group-hover:h-5 opacity-0 group-hover:opacity-100" />
                </Link>
              ))}
            </nav>

            {/* User Profile / Footer */}
            <div className="mt-auto pt-4 border-t border-white/5 flex items-center justify-center lg:justify-start gap-3">
              <div className="w-8 h-8 rounded-full bg-zinc-800 border border-white/10" />
              <div className="hidden lg:block">
                <p className="text-xs font-medium text-white">Operator</p>
                <p className="text-[10px] text-zinc-500 font-mono">ID: 8821</p>
              </div>
            </div>
          </div>
        </aside>

        {/* Main Content Area */}
        <main className="flex-1 relative overflow-y-auto overflow-x-hidden p-6">
           {/* Top Bar (Minimal) */}
           <header className="fixed top-0 left-20 lg:left-64 right-0 h-16 z-40 flex items-center justify-between px-8 pointer-events-none">
              <div className="glass px-4 py-1.5 rounded-full pointer-events-auto flex items-center gap-2">
                 <div className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
                 <span className="text-xs font-mono text-zinc-400">SYSTEM NOMINAL</span>
              </div>
           </header>

           <div className="mt-12 w-full max-w-[1600px] mx-auto pb-10 animate-in fade-in duration-500 slide-in-from-bottom-4">
            {children}
           </div>
        </main>
      </body>
    </html>
  );
}
