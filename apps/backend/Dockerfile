# ╔══════════════════════════════════════════════════════════════════════════════╗
# ║  Dockerfile — GorraShop Backend (.NET 8)                                    ║
# ║                                                                            ║
# ║  ESTE DOCKERFILE ES IDÉNTICO PARA LOS 5 ESCENARIOS.                        ║
# ║  No contiene ninguna referencia a Datadog ni OpenTelemetry.                 ║
# ║                                                                            ║
# ║  - S1/S3/S5 (OTel SDK): la instrumentación viene de paquetes NuGet         ║
# ║    en GorraShop.API.csproj + código en Program.cs                           ║
# ║  - S2/S4 (DD SDK): el Admission Controller inyecta dd-trace-dotnet          ║
# ║    como init container — el Dockerfile no cambia                            ║
# ╚══════════════════════════════════════════════════════════════════════════════╝

# ─── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/GorraShop.API/GorraShop.API.csproj GorraShop.API/
RUN dotnet restore GorraShop.API/GorraShop.API.csproj

COPY src/GorraShop.API/ GorraShop.API/
WORKDIR /src/GorraShop.API
RUN dotnet publish -c Release -o /app/publish --no-restore

# ─── Runtime Stage ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Non-root user para seguridad
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 8080

ENTRYPOINT ["dotnet", "GorraShop.API.dll"]
