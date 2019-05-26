#!/bin/bash
set -e

cd ../web
npm run build

rm -rf ../api/wwwroot
cp -r build/* ../api/wwwroot/

cd api
dotnet publish --configuration Release
