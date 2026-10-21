# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ScientificResearch/ScientificResearch/ScientificResearch.csproj ScientificResearch/ScientificResearch/
ENV NUGET_HTTP_TIMEOUT_SECONDS=120 \
    NUGET_PACKAGES=/root/.nuget/packages
RUN --mount=type=cache,id=scientificresearch-nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=scientificresearch-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
    set -eu; \
    attempt=1; \
    until dotnet restore ScientificResearch/ScientificResearch/ScientificResearch.csproj \
      --disable-parallel \
      -r linux-x64; do \
      if [ "$attempt" -ge 4 ]; then exit 1; fi; \
      attempt=$((attempt + 1)); \
      sleep 5; \
    done; \
    redis_dll="$(find "$NUGET_PACKAGES/stackexchange.redis/3.3.1" -type f -name 'StackExchange.Redis.dll' -print -quit 2>/dev/null || true)"; \
    if [ -z "$redis_dll" ]; then \
      echo 'NuGet restore 未落盘 StackExchange.Redis 3.3.1，绕过 HTTP 缓存重试' >&2; \
      dotnet restore ScientificResearch/ScientificResearch/ScientificResearch.csproj \
        --disable-parallel \
        --force \
        --no-cache \
        -r linux-x64; \
    fi; \
    test -n "$(find "$NUGET_PACKAGES/stackexchange.redis/3.3.1" -type f -name 'StackExchange.Redis.dll' -print -quit 2>/dev/null || true)"

COPY ScientificResearch/ScientificResearch/ ScientificResearch/ScientificResearch/
RUN --mount=type=cache,id=scientificresearch-nuget,target=/root/.nuget/packages \
    --mount=type=cache,id=scientificresearch-nuget-v3,target=/root/.local/share/NuGet/v3-cache \
    dotnet publish ScientificResearch/ScientificResearch/ScientificResearch.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained false \
    --no-restore \
    -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:63739 \
    ASPNETCORE_ENVIRONMENT=Production

# Npgsql probes GSSAPI at startup; keep the runtime image free of the
# libgssapi_krb5 warning and ready for Kerberos-backed PostgreSQL deployments.
USER root
RUN apt-get -o Acquire::Retries=3 update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /out ./
# Keep the modern Vue SPA in the normal runtime image as well as the
# offline/published image. Without this copy, recreating the container falls
# back to the legacy /IMIS entrypoint and discards the current login/logout UI.
COPY frontend/dist/ ./wwwroot/
RUN mkdir -p /app/wwwroot/upload /home/app/.aspnet/DataProtection-Keys \
    && chown -R $APP_UID:$APP_UID /app /home/app/.aspnet

USER $APP_UID
EXPOSE 63739
ENTRYPOINT ["dotnet", "ScientificResearch.dll"]
