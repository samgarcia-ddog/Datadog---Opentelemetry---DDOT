/** @type {import('next').NextConfig} */
const nextConfig = {
  output: "standalone",
  images: {
    unoptimized: true,
    remotePatterns: [
      { hostname: "placehold.co" },
    ],
  },
  // Habilitar el instrumentation hook de Next.js para OTel
  experimental: {
    instrumentationHook: true,
  },
};

export default nextConfig;
