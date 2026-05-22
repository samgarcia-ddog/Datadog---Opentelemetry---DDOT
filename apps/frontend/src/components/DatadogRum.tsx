"use client";

import { useEffect } from "react";
import { datadogRum } from "@datadog/browser-rum";

interface DatadogRumProps {
  enabled: boolean;
  applicationId: string;
  clientToken: string;
  site: string;
  env: string;
  service: string;
  version: string;
}

export function DatadogRum({ enabled, applicationId, clientToken, site, env, service, version }: DatadogRumProps) {
  useEffect(() => {
    if (!enabled || !applicationId || !clientToken) return;

    datadogRum.init({
      applicationId,
      clientToken,
      site,
      service,
      env,
      version,
      sessionSampleRate: 100,
      sessionReplaySampleRate: 20,
      trackResources: true,
      trackUserInteractions: true,
      trackLongTasks: true,
      allowedTracingUrls: [{
        match: window.location.origin,
        propagatorTypes: ["datadog"],
      }],
      defaultPrivacyLevel: "allow",
    });
  }, [enabled, applicationId, clientToken, site, env, service, version]);

  return null;
}
