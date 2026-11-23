#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
: "${UNITY_EDITOR:?Укажите UNITY_EDITOR — путь к Unity 6 Editor}"
mkdir -p "$ROOT/Builds"
"$UNITY_EDITOR" -batchmode -nographics -quit \
  -projectPath "$ROOT" -buildTarget Win64 \
  -executeMethod RLCLab.Editor.ProjectSetup.BuildWindows \
  -logFile "$ROOT/Builds/windows-build.log"
