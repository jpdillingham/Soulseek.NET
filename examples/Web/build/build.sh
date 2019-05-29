#!/bin/bash
set -e

# build web
cd web
pwd
npm run build

# remove old build, but keep .gitkeep
rm -rf ../api/wwwroot
touch ../api/wwwroot/.gitkeep

# copy new files
mkdir ../api/wwwroot
cp -r build/* ../api/wwwroot/

# publish api + web
cd ../api
pwd
dotnet publish --configuration Release

# build docker
cd ..
pwd
docker build -t slsk-web .