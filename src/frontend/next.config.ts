import type { NextConfig } from "next";

/**
 * پیکربندی Next برای پوستهٔ پلتفرم. مرز ماژول بک‌اند از ساختار app/ استنباط نمی‌شود.
 */
const hostOrigin = process.env.TOOBA_HOST_ORIGIN ?? "http://127.0.0.1:5088";

const nextConfig: NextConfig = {
  reactStrictMode: true,
  async rewrites() {
    return [{ source: "/v1/:path*", destination: `${hostOrigin}/v1/:path*` }];
  },
};

export default nextConfig;
