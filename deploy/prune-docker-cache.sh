#!/usr/bin/env bash
set -euo pipefail

# This script deliberately never prunes volumes or containers.  It is safe to
# use on a host that also runs PostgreSQL, uploads, Redis, or Keycloak.
DRY_RUN="${DRY_RUN:-true}"
PRUNE_IMAGES="${PRUNE_IMAGES:-true}"
PRUNE_BUILDER_CACHE="${PRUNE_BUILDER_CACHE:-true}"
BUILDER_CACHE_UNTIL="${BUILDER_CACHE_UNTIL:-24h}"

if ! command -v docker >/dev/null 2>&1; then
  echo "Docker CLI 不可用" >&2
  exit 1
fi

if ! docker info >/dev/null 2>&1; then
  echo "Docker Server 不可用" >&2
  exit 1
fi

echo "清理前 Docker 占用："
docker system df

if [[ "$DRY_RUN" == "true" ]]; then
  echo "仅盘点完成。正式清理请显式设置 DRY_RUN=false；脚本不会删除卷或容器。"
  exit 0
fi

if [[ "$PRUNE_IMAGES" == "true" ]]; then
  docker image prune -f
fi

if [[ "$PRUNE_BUILDER_CACHE" == "true" ]]; then
  docker builder prune -af --filter "until=${BUILDER_CACHE_UNTIL}"
fi

echo "清理后 Docker 占用："
docker system df
