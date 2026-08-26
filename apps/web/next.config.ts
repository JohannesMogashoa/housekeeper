import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  transpilePackages: ["@housekeeper/api", "@housekeeper/db"],
};

export default nextConfig;
