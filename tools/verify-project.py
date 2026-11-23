#!/usr/bin/env python3
"""Static integrity checks. These are NOT a substitute for a Unity build."""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
assets = root / "Assets"
guids = {}
count = 0
for meta in assets.rglob("*.meta"):
    m = re.search(r"^guid: ([0-9a-f]{32})$", meta.read_text(), re.M)
    assert m, f"Invalid GUID: {meta}"
    guid = m[1]
    assert guid not in guids, f"Duplicate GUID: {meta} / {guids.get(guid)}"
    guids[guid] = meta
    assert Path(str(meta)[:-5]).exists(), f"Orphan metadata: {meta}"
for item in assets.rglob("*"):
    if item.suffix != ".meta":
        assert Path(str(item) + ".meta").exists(), f"Missing metadata: {item}"
for item in assets.rglob("*"):
    if item.suffix not in {".asset", ".mat", ".unity", ".meta"}:
        continue
    for guid in re.findall(r"guid: ([0-9a-f]{32})", item.read_text()):
        # Built-in IDs and script GUIDs supplied by com.unity.ugui / TMP.
        known_package = {
            "2705215ac5b84b70bacc50632be6e391",  # TMP_Settings
            "71c1514a6bd24e1e882cebbe1904ce04",  # TMP_FontAsset
            "84a92b25f83d49b9bc132d206b370281",  # TMP_SpriteAsset
            "ab2114bdc8544297b417dfefe9f1e410",  # TMP_StyleSheet
            "2e498d1c8094910479dc3e1b768306a4",  # TMP_ColorGradient
        }
        if guid not in guids and not guid.startswith("0000000000000000"):
            assert guid in known_package, f"Unresolved asset GUID {guid} in {item}"
        count += 1
for asmdef in assets.rglob("*.asmdef"):
    json.loads(asmdef.read_text())
json.loads((root / "Packages/manifest.json").read_text())
assert (assets / "Scenes/Laboratory.unity").exists()
print(f"PASS: {len(guids)} unique asset GUIDs, {count} references; manifests and scene present.")
