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
