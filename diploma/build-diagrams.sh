#!/usr/bin/env bash
# Сборка всех PlantUML-диаграмм из diagrams/ в PDF + PNG в images/
# PDF — векторный формат, используется LaTeX-сборкой для идеального
# качества при масштабировании на ширину страницы.
# PNG — растровый формат, используется для предпросмотра в IDE и git.
#
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

build_puml() {
  local src="$1"
  local name
  name="$(basename "$src" .puml)"
  echo "Сборка $src (PlantUML)..."
  # SVG — промежуточный векторный формат с корректным embedded-текстом.
  plantuml -tsvg -o "$(pwd)/images" "$src"
  # PDF — конвертация из SVG через rsvg-convert (librsvg).
  # Прямой plantuml -tpdf не годится: для component- и usecase-нодов
  # подписи нодов не попадают в PDF (известный баг PlantUML).
  rsvg-convert -f pdf -o "images/${name}.pdf" "images/${name}.svg"
  # PNG — для предпросмотра в IDE и git.
  plantuml -tpng -o "$(pwd)/images" "$src"
}

build_tex() {
  local src="$1"
  local name
  name="$(basename "$src" .tex)"
  echo "Сборка $src (TikZ)..."
  # Standalone TikZ — компилируем XeLaTeX внутри diagrams/, потом
  # перемещаем PDF в images/ и чистим вспомогательные файлы.
  (cd diagrams && xelatex -interaction=nonstopmode "${name}.tex" >/dev/null)
  mv "diagrams/${name}.pdf" "images/${name}.pdf"
  rm -f "diagrams/${name}.aux" "diagrams/${name}.log"
  # Для предпросмотра — конвертация PDF в PNG через sips (macOS native).
  sips -s format png "images/${name}.pdf" --out "images/${name}.png" >/dev/null
}

build_one() {
  local src="$1"
  case "$src" in
    *.puml) build_puml "$src" ;;
    *.tex)  build_tex "$src" ;;
    *) echo "error: неизвестный формат $src" >&2; exit 1 ;;
  esac
}

if [ $# -eq 0 ]; then
  echo "Сборка всех диаграмм из diagrams/*.{puml,tex} в images/..."
  for src in diagrams/*.puml diagrams/*.tex; do
    [ -f "$src" ] || continue
    build_one "$src"
  done
else
  for name in "$@"; do
    if   [ -f "diagrams/${name}.puml" ]; then build_one "diagrams/${name}.puml"
    elif [ -f "diagrams/${name}.tex"  ]; then build_one "diagrams/${name}.tex"
    else echo "error: diagrams/${name}.{puml,tex} не существует" >&2; exit 1
    fi
  done
fi

echo "Готово. Файлы в images/:"
ls -la images/*.pdf images/*.png 2>/dev/null || echo "(пусто)"
