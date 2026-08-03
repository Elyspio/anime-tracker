# Build de l'image mono-conteneur + push. Le déploiement vit avec le chart, dans
# infrastructure-elylan/kubernetes/apps/anime-tracker/deploy.ps1, qui prend ce tag en -Tag.
# Usage : ./deploy/build/build.ps1 [-Image ...]
param(
    [string]$Image = "registry.elylan/elyspio/anime-tracker"
)

$ErrorActionPreference = "Stop"
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$tag = Get-Date -Format "yyyy.MM.dd.HHmm"

Write-Host "Build $Image`:$tag" -ForegroundColor Cyan
docker build -f "$root/deploy/build/dockerfile" --build-arg APP_VERSION=$tag -t "$Image`:$tag" $root

docker push "$Image`:$tag"

Write-Host "Image pushed : $Image`:$tag" -ForegroundColor Green
pwsh "P:\own\common\keycloak\kubernetes\apps\anime-tracker\deploy.ps1" -Tag $tag
