#!/usr/bin/env bash
# Runs every formal check in specs/ and compares each result with the result it is expected to give.
#
#   specs/tla/<Module>[.<Name>].cfg   TLC model-checks <Module>.tla with that config. The first line of the
#                                     config states the expected result: "\* EXPECT: pass" or "\* EXPECT: violation".
#   specs/lean                        lake build; every proof must check.
#
# Environment:
#   JAVA        java executable (default: java on PATH)
#   TLA2TOOLS   path to tla2tools.jar (default: ~/.tlaplus/tla2tools.jar)
#   LAKE        lake executable (default: lake on PATH, then ~/.elan/bin/lake)
#
# Exits non-zero if any check gives a result other than the one expected.
set -uo pipefail

specs="$(dirname "$0")"
java="${JAVA:-java}"
jar="${TLA2TOOLS:-$HOME/.tlaplus/tla2tools.jar}"
lake="${LAKE:-$(command -v lake || echo "$HOME/.elan/bin/lake")}"
metadir="$(mktemp -d)"
failures=0

# Java on Windows (e.g. under Git Bash) needs native paths; TLC derives the module name from the spec path
native() { if command -v cygpath > /dev/null; then cygpath -w "$1"; else echo "$1"; fi; }
jar="$(native "$jar")"

trap 'rm -rf "$metadir"' EXIT

for spec in "$specs"/tla/*.tla; do
  "$java" -cp "$jar" pcal.trans -nocfg "$(native "$spec")" > /dev/null || { echo "FAIL  $spec: PlusCal translation failed"; failures=$((failures + 1)); }
  rm -f "${spec%.tla}.old"
done

for cfg in "$specs"/tla/*.cfg; do
  name="$(basename "$cfg" .cfg)"
  module="${name%%.*}"
  expected="$(sed -n '1s/^\\\* EXPECT: *//p' "$cfg")"

  output="$("$java" -XX:+UseParallelGC -cp "$jar" tlc2.TLC -workers auto -cleanup -noGenerateSpecTE \
    -metadir "$(native "$metadir/$name")" -config "$(native "$cfg")" "$(native "$specs/tla/$module.tla")" 2>&1)"

  if grep -q "No error has been found" <<< "$output"; then
    actual="pass"
  elif grep -qE "is violated|Deadlock reached" <<< "$output"; then
    actual="violation"
  else
    actual="error"
  fi

  states="$(grep -oE '[0-9]+ distinct states found' <<< "$output" | tail -1)"

  if [[ "$actual" == "$expected" ]]; then
    echo "ok    $name: $actual as expected ($states)"
  else
    echo "FAIL  $name: expected $expected, got $actual"
    grep -E "Error|violated|^State [0-9]+:" <<< "$output" | head -40
    failures=$((failures + 1))
  fi
done

if "$lake" --dir "$specs/lean" build > "$metadir/lake.log" 2>&1; then
  echo "ok    lean: all proofs check"
else
  echo "FAIL  lean: build failed"
  cat "$metadir/lake.log"
  failures=$((failures + 1))
fi

exit $((failures > 0))
