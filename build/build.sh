#!/bin/sh
set -e

dotnet restore
dotnet build --no-restore --no-incremental --configuration Release