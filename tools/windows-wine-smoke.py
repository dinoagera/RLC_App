#!/usr/bin/env python3
"""Linux-only launch/visual smoke test of the real Windows executable.

Requires Wine, X11/xwininfo, and Pillow. Uses an isolated Wine prefix and captures
only the RLC Lab window, not the desktop. This does not replace native Windows QA.
"""
import argparse
import ctypes
import ctypes.util
import json
import os
from pathlib import Path
import re
import subprocess
from datetime import datetime, timezone
from PIL import Image

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser()
parser.add_argument("--duration", type=float, default=15, help="Seconds to allow the Player to run")
args = parser.parse_args()
output = root / "TestResults"
output.mkdir(exist_ok=True)
env = os.environ.copy()
env.update(WINEPREFIX="/tmp/opencode/rlc-wine", WINEDLLOVERRIDES="mscoree,mshtml=", WINEDEBUG="-all")
player_log = output / "windows-wine-player.log"
log_path = "Z:" + str(player_log).replace("/", "\\")
command = ["wine", str(root / "Builds/Windows/RLC Lab.exe"), "-screen-fullscreen", "0",
           "-screen-width", "1280", "-screen-height", "720", "-force-d3d11", "-logFile", log_path]


def capture_window():
    windows = subprocess.check_output(["xwininfo", "-root", "-tree"], text=True)
    match = re.search(r'^\s*(0x[\da-f]+) "RLC Lab"', windows, re.M)
    if not match:
        raise RuntimeError("No visible RLC Lab window")
    window_id = int(match[1], 16)
    details = subprocess.check_output(["xwininfo", "-id", str(window_id)], text=True)
    width = int(re.search(r"Width: (\d+)", details)[1])
    height = int(re.search(r"Height: (\d+)", details)[1])

    class XImage(ctypes.Structure):
        _fields_ = [(name, kind) for name, kind in [
            ("width", ctypes.c_int), ("height", ctypes.c_int), ("xoffset", ctypes.c_int),
            ("format", ctypes.c_int), ("data", ctypes.c_void_p), ("byte_order", ctypes.c_int),
            ("bitmap_unit", ctypes.c_int), ("bitmap_bit_order", ctypes.c_int),
            ("bitmap_pad", ctypes.c_int), ("depth", ctypes.c_int), ("bytes_per_line", ctypes.c_int),
            ("bits_per_pixel", ctypes.c_int), ("red_mask", ctypes.c_ulong),
            ("green_mask", ctypes.c_ulong), ("blue_mask", ctypes.c_ulong)]]

    x11 = ctypes.CDLL(ctypes.util.find_library("X11"))
    x11.XOpenDisplay.argtypes = [ctypes.c_char_p]
    x11.XOpenDisplay.restype = ctypes.c_void_p
    x11.XCloseDisplay.argtypes = [ctypes.c_void_p]
    x11.XGetImage.argtypes = [ctypes.c_void_p, ctypes.c_ulong, ctypes.c_int, ctypes.c_int,
                             ctypes.c_uint, ctypes.c_uint, ctypes.c_ulong, ctypes.c_int]
    x11.XGetImage.restype = ctypes.POINTER(XImage)
    x11.XDestroyImage.argtypes = [ctypes.POINTER(XImage)]
    display = x11.XOpenDisplay(None)
    if not display:
        raise RuntimeError("X11 display unavailable")
    image = None
    try:
        image = x11.XGetImage(display, window_id, 0, 0, width, height, ctypes.c_ulong(-1).value, 2)
        if not image:
            raise RuntimeError("XGetImage failed")
        data = image.contents
        if (data.bits_per_pixel, data.byte_order, data.red_mask, data.blue_mask) != (32, 0, 0xff0000, 0xff):
            raise RuntimeError("Unexpected X11 pixel format")
        raw = ctypes.string_at(data.data, data.bytes_per_line * height)
        picture = Image.frombytes("RGB", (width, height), raw, "raw", "BGRX", data.bytes_per_line, 1)
        picture.save(output / "windows-wine.png")
        return width, height
    finally:
        if image:
            x11.XDestroyImage(image)
        x11.XCloseDisplay(display)


with (output / "windows-wine-launch.log").open("w") as launch_log:
    process = subprocess.Popen(command, env=env, stdout=launch_log, stderr=subprocess.STDOUT)
    try:
        try:
            process.wait(timeout=max(3, args.duration))
        except subprocess.TimeoutExpired:
            pass
        if process.poll() is not None:
            raise RuntimeError(f"Player exited early: {process.returncode}")
        width, height = capture_window()
        log_text = player_log.read_text(errors="replace")
        for marker in ["NullReferenceException", "Unhandled Exception", "IndexOutOfRangeException",
                       "MissingReferenceException", "ArgumentException:"]:
            if marker in log_text:
                raise RuntimeError("Player log contains " + marker)
        report = {
            "timestampUtc": datetime.now(timezone.utc).isoformat(),
            "passed": True,
            "environment": "Linux, isolated Wine prefix, Direct3D 11",
            "nativeWindowsTested": False,
            "runningSeconds": max(3, args.duration),
            "windowSize": [width, height],
            "screenshot": "windows-wine.png",
            "csharpExceptionsDetected": False,
            "note": "Launch/visual smoke only; not a full functional or performance test."
        }
        (output / "windows-wine-report.json").write_text(json.dumps(report, indent=2), encoding="utf-8")
        print(json.dumps(report, indent=2))
    finally:
        if process.poll() is None:
            process.terminate()
        try:
            process.wait(timeout=10)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
