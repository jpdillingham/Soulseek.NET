#!/bin/bash
set -e
__dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
__branch=$(git branch --no-color | grep -E '^\*' | awk '{print $2}')

options="/d:sonar.branch.name="${__branch}""

if [ "${CIRCLECI}" = "true" ]; then
    __branch="${CIRCLE_BRANCH}"

    if [ ! -z "${CIRCLE_PULL_REQUEST}" ]; then
        options="/d:sonar.pullrequest.base="master" /d:sonar.pullrequest.branch="${__branch}" /d:sonar.pullrequest.key="${CIRCLE_PULL_REQUEST##*/}""
    fi
fi

echo "Launching dotnet-sonarscanner with options: ${options}"

dotnet-sonarscanner begin \
    /key:"jpdillingham_Soulseek.NET" \
    /o:jpdillingham-github \
    ${options} \
    /d:sonar.host.url="https://sonarcloud.io" \
    /d:sonar.github.repository="jpdillingham/Soulseek.NET" \
    /d:sonar.exclusions="**/*examples*/**" \
    /d:sonar.cs.opencover.reportsPaths="tests/opencover.xml" \
    /d:sonar.login="${TOKEN_SONARCLOUD}" \

. "${__dir}/build.sh"
. "${__dir}/test.sh"

dotnet-sonarscanner end /d:sonar.login="${TOKEN_SONARCLOUD}"

bash <(curl -s https://codecov.io/bash) -f tests/opencover.xml