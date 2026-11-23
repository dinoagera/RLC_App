#!/usr/bin/env python3
"""Run the available non-Unity checks. Does not claim to validate a Unity build."""
import json
import subprocess
import sys
from pathlib import Path
from datetime import datetime, timezone

root = Path(__file__).resolve().parents[1]
commands = [
    ["dotnet", "run", "--project", "tools/MathTests/MathTests.csproj", "-c", "Release"],
    ["node", "--test", "Web/tests/math.test.mjs"],
    [sys.executable, "tools/verify-project.py"]
]
results = []
for command in commands:
    print("$ " + " ".join(command), flush=True)
    process = subprocess.run(command, cwd=root, text=True, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    print(process.stdout, flush=True)
    results.append({"command": command, "exitCode": process.returncode, "output": process.stdout})
report = {
    "timestamp": datetime.now(timezone.utc).isoformat(),
    "scope": "non-unity-regression-checks",
    "passed": all(result["exitCode"] == 0 for result in results),
    "unityEditorTestsExecuted": False,
    "windowsBuildExecuted": False,
    "checks": results
}
output = root / "TestResults"
output.mkdir(exist_ok=True)
(output / "checks-report.json").write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding="utf-8")
sys.exit(0 if report["passed"] else 1)
