#!/bin/bash
set -e

cd web
pwd
#npm run build

rm -rf ../api/wwwroot
mkdir ../api/wwwroot
cp -r build/* ../api/wwwroot/

cd ../api
pwd
dotnet publish --configuration Release
