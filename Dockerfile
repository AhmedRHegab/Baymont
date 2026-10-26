# Baymont — single image: React build served by the ASP.NET Core API (SQLite on a volume).

# 1) Frontend. vite.config.ts writes to ../backend/Baymont.Api/wwwroot, so mirror that layout.
FROM node:22-alpine AS web
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# 2) Backend publish, with the built SPA dropped into wwwroot.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/ backend/
COPY --from=web /src/backend/Baymont.Api/wwwroot backend/Baymont.Api/wwwroot
RUN dotnet publish backend/Baymont.Api/Baymont.Api.csproj -c Release -o /out /p:UseAppHost=false

# 3) Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /out ./

# Mount a Railway volume at /app/App_Data: it holds baymont.db, uploads/ and keys/.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ConnectionStrings__Default="Data Source=/app/App_Data/baymont.db" \
    DOTNET_gcServer=0

# Railway injects PORT; fall back to 8080 for local `docker run`.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Baymont.Api.dll"]
