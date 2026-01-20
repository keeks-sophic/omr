"use client";

import Link from "next/link";
import { LayoutDashboard, Bot, Map as MapIcon, Play, Gamepad2, ListTodo, Layers, Sliders, Activity, Film, ClipboardList, TrafficCone, Users, User } from "lucide-react";
import { useEffect, useState } from "react";
import { getApiBaseUrl } from "../../lib/config";
import { fetchMe } from "../../lib/auth";
import { isRouteAllowed } from "../../lib/roleAccess";

export default function NavSidebar() {
  const [roles, setRoles] = useState<string[]>(["Viewer"]);

  useEffect(() => {
    async function load() {
      try {
        const me = await fetchMe(getApiBaseUrl());
        setRoles(me.roles || ["Viewer"]);
      } catch {
        setRoles(["Viewer"]);
      }
    }
    load();
  }, []);

  const baseItems = [
    { name: "Dashboard", href: "/fleet", icon: LayoutDashboard },
    { name: "Profile", href: "/profile", icon: User },
    { name: "Robot", href: "/robot", icon: Bot },
    { name: "Map", href: "/map", icon: MapIcon },
    { name: "Traffic", href: "/traffic", icon: TrafficCone },
    { name: "Visualise", href: "/visualise", icon: Layers },
    { name: "Simulation", href: "/sim", icon: Play },
    { name: "Control", href: "/control", icon: Gamepad2 },
    { name: "Mission", href: "/mission", icon: ListTodo },
    { name: "Tasks", href: "/tasks", icon: ClipboardList },
    { name: "Config", href: "/config", icon: Sliders },
    { name: "Ops", href: "/ops", icon: Activity },
    { name: "Replay", href: "/replay", icon: Film },
  ];
  const allItems = [...baseItems, { name: "Users", href: "/users", icon: Users }];
  const navItems = allItems.filter((item) => isRouteAllowed(item.href, roles));

  return (
    <aside className="w-20 lg:w-64 flex-shrink-0 flex flex-col items-center lg:items-stretch py-6 px-3 z-50">
      <div className="glass rounded-3xl h-full flex flex-col p-4 shadow-xl shadow-black/20">
        <div className="flex items-center justify-center lg:justify-start gap-3 mb-8 px-2">
          <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-primary to-accent shadow-lg shadow-primary/30 flex-shrink-0" />
          <span className="hidden lg:block font-bold text-lg tracking-tight">Skylink</span>
        </div>
        <nav className="flex-1 flex flex-col gap-2">
          {navItems.map((item) => (
            <Link
              key={item.name}
              href={item.href}
              className="flex items-center gap-3 px-3 py-3 rounded-xl text-zinc-400 hover:text-white hover:bg-white/5 transition-all group relative"
            >
              <item.icon size={22} strokeWidth={1.5} className="group-hover:scale-110 transition-transform duration-200" />
              <span className="hidden lg:block font-medium text-sm tracking-wide">{item.name}</span>
              <div className="absolute left-0 w-1 h-0 bg-primary rounded-r-full transition-all group-hover:h-5 opacity-0 group-hover:opacity-100" />
            </Link>
          ))}
        </nav>
        <div className="mt-auto pt-4 border-t border-white/5 flex items-center justify-center lg:justify-start gap-3">
          <div className="w-8 h-8 rounded-full bg-zinc-800 border border-white/10" />
          <div className="hidden lg:block">
            <p className="text-xs font-medium text-white">Operator</p>
            <p className="text-[10px] text-zinc-500 font-mono">ID: 8821</p>
          </div>
        </div>
      </div>
    </aside>
  );
}
