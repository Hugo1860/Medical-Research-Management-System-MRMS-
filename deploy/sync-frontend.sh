#!/usr/bin/env bash
set -euo pipefail

# 自动化构建与同步现代前端产物
# 用法:
#   ./deploy/sync-frontend.sh
# 可选环境变量:
#   SYNC_TO_CONTAINER=true  (构建后热同步到正在运行的 2018-7-24-app-1 容器)
#   CONTAINER_NAME=2018-7-24-app-1

script_dir="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
project_root="$(cd "$script_dir/.." && pwd)"
frontend_dir="$project_root/frontend"
container_name="${CONTAINER_NAME:-2018-7-24-app-1}"
sync_to_container="${SYNC_TO_CONTAINER:-false}"

echo "========================================================"
echo " 正在编译现代 Vue 3 + TypeScript 前端工程..."
echo "========================================================"

cd "$frontend_dir"
if ! command -v pnpm >/dev/null 2>&1; then
    echo "错误: 未找到 pnpm，请先安装 pnpm (npm install -g pnpm)" >&2
    exit 1
fi

pnpm run build

echo ""
echo "========================================================"
echo " 前端构建成功！dist 产物已生成。"
echo " 正在校验构建产物完整性..."
echo "========================================================"

dist_dir="$frontend_dir/dist"
if [ ! -f "$dist_dir/index.html" ] || [ ! -d "$dist_dir/assets" ]; then
    echo "错误: $dist_dir 缺少 index.html 或 assets 目录" >&2
    exit 1
fi

asset_count=$(find "$dist_dir/assets" -type f | wc -l | tr -d ' ')
echo "检测到 $asset_count 个已优化打包的静态 chunk 资源。"

# Keep the checked-out offline publish tree in sync too. Dockerfile.published
# copies this directory into the image, so a container recreation retains the
# modern SPA instead of relying on a one-off hot sync.
publish_wwwroot="$project_root/publish/linux-x64/wwwroot"
if [ -d "$publish_wwwroot" ]; then
    if [ -f "$publish_wwwroot/index.html" ] && [ ! -f "$publish_wwwroot/index.legacy.html" ]; then
        cp "$publish_wwwroot/index.html" "$publish_wwwroot/index.legacy.html"
    fi
    cp -R "$dist_dir/." "$publish_wwwroot/"
    echo "已同步现代 SPA 到离线发布目录: $publish_wwwroot"
fi

if [ "$sync_to_container" = "true" ]; then
    if docker ps --format '{{.Names}}' | grep -q "^${container_name}\$"; then
        echo ""
        echo "========================================================"
        echo " 正在热同步静态产物到运行中的 Docker 容器: $container_name ..."
        echo "========================================================"
        # 备份旧版 index.html (若尚未备份)
        docker exec -u root "$container_name" sh -c "if [ ! -f /app/wwwroot/index.legacy.html ]; then cp /app/wwwroot/index.html /app/wwwroot/index.legacy.html; fi"
        # 同步静态产物
        docker cp "$dist_dir/." "${container_name}:/app/wwwroot/"
        echo "容器静态资源热同步完成！访问 http://127.0.0.1:16379/ 可即时体验现代 SPA。"
    else
        echo "提示: 容器 $container_name 未运行，跳过容器热同步。"
    fi
fi

echo ""
echo "========================================================"
echo " 现代前端构建与部署准备就绪！"
echo " 产物目录: $dist_dir"
echo "========================================================"
