#!/usr/bin/env bash
set -euo pipefail

# Build artifacts only. Does not start, restart or write to any application or DB.
root="$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)"
cd "$root"
stage="$(mktemp -d "${TMPDIR:-/tmp}/hrms-release.XXXXXX")"
image="scientificresearch:local-build-$(date +%Y%m%d%H%M%S)-$$"
container="hrms-release-export-$$-$(date +%s)"
container_created=false
image_created=false
cleanup() {
  if [ "$container_created" = true ]; then docker rm "$container" >/dev/null 2>&1 || true; fi
  if [ "$image_created" = true ]; then docker image rm "$image" >/dev/null 2>&1 || true; fi
  rm -rf "$stage"
}
trap cleanup EXIT
python3 deploy/release-source-manifest.py write "$stage/release-source-manifest.json"
(cd frontend && pnpm install --frozen-lockfile --prefer-offline && pnpm build)
docker build --build-arg "SDK_IMAGE=${SDK_IMAGE:-mcr.microsoft.com/dotnet/sdk:10.0}" --build-arg "NUGET_REMOTE_SOURCE=${NUGET_REMOTE_SOURCE-https://api.nuget.org/v3/index.json}" -f deploy/Dockerfile.regression --target build -t "$image" .
image_created=true
docker create --name "$container" "$image" >/dev/null
container_created=true
mkdir "$stage/release"
docker cp "$container:/out/." "$stage/release/"
cp "$stage/release/wwwroot/index.html" "$stage/release/wwwroot/index.legacy.html"
cp -R frontend/dist/. "$stage/release/wwwroot/"
python3 deploy/release-source-manifest.py check "$stage/release-source-manifest.json"
cp "$stage/release-source-manifest.json" "$stage/release/release-source-manifest.json"
python3 deploy/release-source-manifest.py seal "$stage/release/release-source-manifest.json" --artifact-root "$stage/release"
PUBLISH_ROOT="$stage/release" ./deploy/verify-publish-parity.sh
mkdir -p publish
if [ -e publish/linux-x64 ]; then
  backup="publish/linux-x64.before-$(date +%Y%m%d%H%M%S)-$$"
  mv publish/linux-x64 "$backup"
  echo "Previous offline artifacts retained: $backup"
fi
mv "$stage/release" publish/linux-x64
echo 'Local release artifacts built and verified; no running service was changed.'
