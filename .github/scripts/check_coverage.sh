#!/usr/bin/env bash
set -euo pipefail

if [ -z "$(ls -A coverage 2>/dev/null || true)" ]; then
  echo "No coverage files found in coverage/" >&2
  exit 1
fi

FAILED=0
for f in coverage/*.cobertura.xml; do
  if [ ! -f "$f" ]; then
    echo "No cobertura files found" >&2
    exit 1
  fi
  # Extract line-rate attribute from cobertura xml
  rate=$(grep -o 'line-rate="[^"]*"' "$f" | head -1 | sed -E 's/line-rate="([^"]+)"/\1/')
  if [ -z "$rate" ]; then
    echo "Could not parse line-rate from $f" >&2
    FAILED=1
    continue
  fi
  # Convert to percentage
  pct=$(awk "BEGIN{printf \"%.2f\", $rate * 100}")
  echo "$f -> $pct%"
  # Fail if less than 100.00
  cmp=$(awk "BEGIN{print ($rate < 1.0)}")
  if [ "$cmp" -eq 1 ]; then
    echo "Coverage threshold not met for $f: $pct% < 100%" >&2
    FAILED=1
  fi
done

if [ "$FAILED" -ne 0 ]; then
  echo "One or more coverage checks failed" >&2
  exit 2
fi

echo "All coverage checks passed (100%)."
