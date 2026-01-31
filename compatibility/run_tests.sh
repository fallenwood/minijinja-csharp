#!/bin/bash

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASELINE_DIR="$SCRIPT_DIR/baseline"
REWRITE_DIR="$SCRIPT_DIR/rewrite"

# Build projects
echo "Building baseline (Rust)..."
cd "$BASELINE_DIR"
cargo build --release --quiet || { echo "Rust build failed"; exit 1; }

echo "Building rewrite (C#)..."
cd "$REWRITE_DIR"
dotnet build -c Release --nologo -v q || { echo "C# build failed"; exit 1; }

echo ""
echo "Running compatibility tests..."
echo "=============================="

PASSED=0
FAILED=0
CASES=("case0" "case1" "case2" "case3" "case4" "case5" "case6" "case7" "case8" "case9" "case10" "case11")

for case in "${CASES[@]}"; do
  # Run baseline
  cd "$BASELINE_DIR"
  BASELINE_OUTPUT=$(./target/release/baseline "$case" 2>&1) || BASELINE_OUTPUT="ERROR: $?"
  
  # Run rewrite
  cd "$REWRITE_DIR"
  REWRITE_OUTPUT=$(dotnet run -c Release --no-build -- "$case" 2>&1) || REWRITE_OUTPUT="ERROR: $?"
  
  # Compare outputs
  if [ "$BASELINE_OUTPUT" = "$REWRITE_OUTPUT" ]; then
    echo "✓ $case: PASSED"
    PASSED=$((PASSED + 1))
  else
    echo "✗ $case: FAILED"
    echo "  Baseline output:"
    echo "$BASELINE_OUTPUT" | sed 's/^/    /'
    echo "  Rewrite output:"
    echo "$REWRITE_OUTPUT" | sed 's/^/    /'
    FAILED=$((FAILED + 1))
  fi
done

echo ""
echo "=============================="
echo "Results: $PASSED passed, $FAILED failed"

if [ $FAILED -gt 0 ]; then
  exit 1
fi
