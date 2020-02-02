#!/bin/bash
set -e

LATEST=$(find . | sort -dr | grep nupkg | head -1)

dotnet nuget push $LATEST --api-key ${TOKEN_NUGET}

LATEST_SYMBOLS=$(find . | sort -dr | grep snupkg | head -1)

dotnet nuget push $LATEST_SYMBOLS --apk-key ${TOKEN_NUGET}