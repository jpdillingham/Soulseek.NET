#!/bin/sh
set -e
__dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

dotnet sonarscanner begin /key:"jpdillingham_Soulseek.NET" /o:jpdillingham-github /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="${SONARCLOUD_TOKEN}" /d:sonar.cs.opencover.reportsPaths="tests/opencover.xml" || true

source ${__dir}/build.sh
source ${__dir}/test.sh

dotnet sonarscanner end /d:sonar.login="${SONARCLOUD_TOKEN}" || true