# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║  Dockerfile — GorraShop Frontend (Next.js 14)                              ║
# ║                                                                            ║
# ║  ESTE DOCKERFILE ES IDÉNTICO PARA LOS 5 ESCENARIOS.                        ║
# ║  No contiene ninguna referencia a Datadog ni OpenTelemetry.                 ║
# ║                                                                            ║
# ║  - S1/S3/S5 (OTel SDK): la instrumentación viene de paquetes npm           ║
# ║    en package.json + código en instrumentation.ts                           ║
# ║  - S2/S4 (DD SDK): el Admission Controller inyecta dd-trace-js             ║
# ║    como init container — el Dockerfile no cambia                            ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

# ─── Deps Stage ───────────────────────────────────────────────────────────────
FROM node:20-alpine AS deps
WORKDIR /app
COPY package.json ./
RUN npm install

# ─── Build Stage ──────────────────────────────────────────────────────────────
FROM node:20-alpine AS builder
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY . .

RUN npm run build

# ─── Runtime Stage ────────────────────────────────────────────────────────────
FROM node:20-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

RUN addgroup --system nodejs && adduser --system --ingroup nodejs nextjs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

CMD ["node", "server.js"]
