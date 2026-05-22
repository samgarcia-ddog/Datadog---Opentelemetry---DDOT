import type { Metadata } from "next";
import "./globals.css";
import { Header } from "@/components/Header";
import { Footer } from "@/components/Footer";
import dynamic from "next/dynamic";

const DatadogRum = dynamic(() => import("@/components/DatadogRum").then(m => m.DatadogRum), { ssr: false });

export const metadata: Metadata = {
  title: "GorraShop — Tu tienda de gorras",
  description: "Snapbacks, fitteds, truckers, bucket hats y beanies. AKS + OTel Lab.",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  const rumEnabled = process.env.DD_RUM_ENABLED === "true";
  const rumAppId = process.env.DD_RUM_APPLICATION_ID || "";
  const rumClientToken = process.env.DD_RUM_CLIENT_TOKEN || "";
  const rumSite = process.env.DD_RUM_SITE || "datadoghq.com";
  const ddEnv = process.env.DD_ENV || "lab";
  const ddService = process.env.DD_SERVICE || "gorrashop-frontend";
  const ddVersion = process.env.DD_VERSION || "1.0.0";

  return (
    <html lang="es" className="dark">
      <body className="min-h-screen flex flex-col">
        <DatadogRum
          enabled={rumEnabled}
          applicationId={rumAppId}
          clientToken={rumClientToken}
          site={rumSite}
          env={ddEnv}
          service={ddService}
          version={ddVersion}
        />
        <Header />
        <main className="flex-1 container mx-auto px-4 py-8 max-w-7xl">
          {children}
        </main>
        <Footer />
      </body>
    </html>
  );
}
