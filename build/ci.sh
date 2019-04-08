#!/bin/bash
set -e
__dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
__branch=$(git branch --no-color | grep -E '^\*' | awk '{print $2}')

options="/d:sonar.branch.name="${__branch}""

if [ "${CIRCLECI}" = "true" ]; then
    __branch="${CIRCLE_BRANCH}"

    if [ ! -z "${CIRCLE_PULL_REQUEST}" ]; then
        options="/d:sonar.pullrequest.branch="${__branch}" /d:sonar.pullrequest.key="${CIRCLE_PULL_REQUEST##*/}""
    fi
fi

echo "Launching dotnet-sonarscanner with options: "${options}""

dotnet-sonarscanner begin /key:"jpdillingham_Soulseek.NET" /o:jpdillingham-github /d:sonar.host.url="https://sonarcloud.io" /d:sonar.exclusions="**/*examples*/**" "${options}" /d:sonar.login="${TOKEN_SONARCLOUD}" /d:sonar.cs.opencover.reportsPaths="tests/opencover.xml"

. "${__dir}/build.sh"
. "${__dir}/test.sh"

dotnet-sonarscanner end /d:sonar.login="${TOKEN_SONARCLOUD}"

bash <(curl -s https://codecov.io/bash) -f tests/opencover.xml