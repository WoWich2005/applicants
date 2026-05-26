#!/usr/bin/env bash
# Сборка всех PlantUML-диаграмм из diagrams/ в PNG в images/
# Требуется: plantuml (brew install plantuml)
#
# Использование:
#   ./build-diagrams.sh           — собрать все диаграммы
#   ./build-diagrams.sh use-cases — собрать только diagrams/use-cases.puml

set -euo pipefail

cd "$(dirname "$0")"

if ! command -v plantuml >/dev/null 2>&1; then
  echo "error: plantuml не найден. Установить: brew install plantuml" >&2
  exit 1
fi

mkdir -p images

if [ $# -eq 0 ]; then
  echo "Сборка всех диаграмм из diagrams/*.puml в images/..."
  plantuml -tpng -o "$(pwd)/images" diagrams/*.puml
else
  for name in "$@"; do
    src="diagrams/${name}.puml"
    if [ ! -f "$src" ]; then
      echo "error: $src не существует" >&2
      exit 1
    fi
    echo "Сборка $src..."
    plantuml -tpng -o "$(pwd)/images" "$src"
  done
fi

echo "Готово. Файлы в images/:"
ls -la images/*.png 2>/dev/null || echo "(пусто)"
