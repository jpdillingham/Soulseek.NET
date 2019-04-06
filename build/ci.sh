#!/bin/bash
set -e
__dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

dotnet-sonarscanner begin /key:"jpdillingham_Soulseek.NET" /o:jpdillingham-github /d:sonar.host.url="https://sonarcloud.io" /d:sonar.login="${SONARCLOUD_TOKEN}" /d:sonar.cs.opencover.reportsPaths="tests/opencover.xml"

. "${__dir}/build.sh"
. "${__dir}/test.sh"

dotnet-sonarscanner end /d:sonar.login="${SONARCLOUD_TOKEN}"

bash <(curl -s https://codecov.io/bash) -f tests/opencover.xml