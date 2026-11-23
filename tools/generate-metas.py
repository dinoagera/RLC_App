#!/usr/bin/env python3
"""Create deterministic Unity metadata only where it is absent. Existing GUIDs are preserved."""
from pathlib import Path
import hashlib

root = Path(__file__).resolve().parents[1]
for item in sorted((root / "Assets").rglob("*")):
    if item.suffix == ".meta":
        continue
    meta = Path(str(item) + ".meta")
    if meta.exists():
        continue
    guid = hashlib.md5(item.relative_to(root).as_posix().encode()).hexdigest()
    header = f"fileFormatVersion: 2\nguid: {guid}\n"
    if item.is_dir():
        content = "folderAsset: yes\nDefaultImporter:\n  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
    elif item.suffix == ".cs":
        content = "MonoImporter:\n  externalObjects: {}\n  serializedVersion: 2\n  defaultReferences: []\n  executionOrder: 0\n  icon: {instanceID: 0}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
    elif item.suffix == ".ttf":
        content = ("TrueTypeFontImporter:\n  serializedVersion: 2\n  fontSize: 16\n  forceTextureCase: -2\n"
                   "  characterSpacing: 1\n  characterPadding: 0\n  includeFontData: 1\n  use2xBehaviour: 0\n"
                   "  fontNames: []\n  fallbackFontReferences: []\n  customCharacters:\n  fontRenderingMode: 0\n"
                   "  userData:\n  assetBundleName:\n  assetBundleVariant:\n")
    else:
        importer = "AssemblyDefinitionImporter" if item.suffix == ".asmdef" else "TextScriptImporter" if item.suffix == ".txt" else "DefaultImporter"
        content = f"{importer}:\n  externalObjects: {{}}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n"
    meta.write_text(header + content, encoding="utf-8")
print("Unity metadata: OK")
