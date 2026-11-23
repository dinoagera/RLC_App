#!/usr/bin/env python3
"""Archive the actual Unity Windows build, including all required companion files."""
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

root = Path(__file__).resolve().parents[1]
build = root / "Builds/Windows"
for required in ["RLC Lab.exe", "UnityPlayer.dll", "RLC Lab_Data", "MonoBleedingEdge"]:
    if not (build / required).exists():
        raise SystemExit(f"Missing Windows build component: {required}. Build the project first.")
output = root / "Delivery/RLC_Lab_Windows_x64.zip"
output.parent.mkdir(exist_ok=True)
prefix = Path("RLC_Lab_Windows_x64")
with ZipFile(output, "w", ZIP_DEFLATED, compresslevel=6) as archive:
    for file in sorted(build.rglob("*")):
        if file.is_file():
            archive.write(file, prefix / file.relative_to(build))
    archive.writestr(str(prefix / "README_ru.txt"),
        "RLC / LAB — Windows x64\n\n"
        "Распакуйте архив целиком и запустите RLC Lab.exe.\n"
        "Не переносите .exe отдельно от остальных файлов и папок.\n"
        "Unity, Python и .NET для запуска не нужны.\n\n"
        "Пробел: старт/пауза. R: сброс. 1/2/3: режим графика.\n"
        "F1: встроенная справка. Esc: закрыть справку. Alt+F4: выход.\n\n"
        "Собрано Unity 6000.0.62f1, Windows x64, Mono.\n"
        "Проверены Unity EditMode/PlayMode и запуск Windows .exe под Wine.\n"
        "Отдельная проверка на Windows-ПК ещё не выполнена.\n")
    for source, name in [
        ("Assets/RLCLab/Resources/Fonts/LICENSE.txt", "DejaVuSans-LICENSE.txt"),
        ("Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt", "LiberationSans-OFL.txt"),
        ("Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt", "EmojiOne-Attribution.txt"),
    ]:
        archive.write(root / source, prefix / "Licenses" / name)
print(f"Windows archive: {output} ({output.stat().st_size:,} bytes)")
