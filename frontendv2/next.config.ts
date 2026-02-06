import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  async rewrites() {
    const base = (process.env.BACKEND_BASE_URL ?? process.env.NEXT_PUBLIC_BACKEND_BASE_URL ?? "http://localhost:5000").replace(/\/$/, "");
    return [
      {
        source: "/hubs/realtime",
        destination: `${base}/hubs/realtime`,
      },
      {
        source: "/hubs/realtime/:path*",
        destination: `${base}/hubs/realtime/:path*`,
      },
    ];
  },
};

export default nextConfig;
