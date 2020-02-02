#!/bin/bash
set -e

RELEASE_DIR=./src/Soulseek/bin/Release/
NUGET_API=nuget.org
TEMP_FILE=${RELEASE_DIR}not_symbols

LATEST=$(find $RELEASE_DIR | sort -dr | grep "\.nupkg$" | head -1)
LATEST_SYMBOLS=$(find $RELEASE_DIR | sort -dr | grep "\.snupkg$" | head -1)

printf "\npreparing to deploy:\n"
echo "  nupkg: $LATEST"
printf "  symbols: $LATEST_SYMBOLS\n\n"

printf "pushing nupkg $LATEST\n\n"
nuget push $LATEST -nosymbols -source ${NUGET_API} -apikey ${TOKEN_NUGET} -skipduplicate
printf "nupkg done.\n\n"

printf "pushing symbols $LATEST_SYMBOLS\n\n"
nuget push $LATEST_SYMBOLS -source ${NUGET_API} -apikey ${TOKEN_NUGET} -skipduplicate
printf "symbols done.\n\n"

printf "deployment complete."