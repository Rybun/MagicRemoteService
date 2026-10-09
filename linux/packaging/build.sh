#!/usr/bin/env bash
# Publishes the daemon as a self-contained linux-x64 binary (no .NET runtime required on the
# target machine — the whole point of a system service that must work before any user has a
# chance to install anything). Output goes to packaging/out/daemon/, which install.sh expects.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUT_DIR="$SCRIPT_DIR/out/daemon"

rm -rf "$OUT_DIR"
dotnet publish "$REPO_ROOT/src/MagicRemote.Daemon" \
	--configuration Release \
	--runtime linux-x64 \
	--self-contained true \
	-p:PublishSingleFile=true \
	-p:EnableCompressionInSingleFile=true \
	--output "$OUT_DIR"

echo "Published daemon to $OUT_DIR"
