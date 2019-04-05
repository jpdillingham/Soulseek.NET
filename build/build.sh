#!/bin/sh
dotnet restore
dotnet build --no-restore --no-incremental --configuration Release