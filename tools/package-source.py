#!/usr/bin/env python3
"""Package source and documentation, without caches or fabricated build outputs."""
from pathlib import Path
from zipfile import ZIP_DEFLATED, ZipFile

root = Path(__file__).resolve().parents[1]
destination = root / "Delivery" / "RLC_Lab_Source.zip"
destination.parent.mkdir(exist_ok=True)
excluded = {".git", ".vs", ".idea", "Library", "Temp", "Logs", "UserSettings",
            "Builds", "Delivery", "TestResults", "node_modules", "bin", "obj", "__pycache__"}
count = 0
with ZipFile(destination, "w", compression=ZIP_DEFLATED, compresslevel=9) as archive:
    for item in sorted(root.rglob("*")):
        relative = item.relative_to(root)
        if not item.is_file() or any(part in excluded for part in relative.parts):
            continue
        if item.suffix in {".pyc", ".user"}:
            continue
        archive.write(item, Path("RLC_Lab") / relative)
        count += 1
print(f"Source archive: {destination} ({count} files, {destination.stat().st_size:,} bytes)")
print("Windows executable is packaged separately in Delivery/RLC_Lab_Windows_x64.zip.")
