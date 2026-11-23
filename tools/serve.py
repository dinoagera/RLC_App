#!/usr/bin/env python3
"""Serve the standalone preview and font locally. No dependencies."""
import argparse
import functools
import webbrowser
from http.server import SimpleHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path

parser = argparse.ArgumentParser(description="RLC Lab — локальная браузерная версия")
parser.add_argument("--port", type=int, default=8080)
parser.add_argument("--open", action="store_true", help="Открыть лабораторию в браузере")
args = parser.parse_args()
root = Path(__file__).resolve().parents[1]

class Handler(SimpleHTTPRequestHandler):
    extensions_map = {**SimpleHTTPRequestHandler.extensions_map, ".mjs": "text/javascript"}
    def do_GET(self):
        if self.path == "/":
            self.send_response(302)
            self.send_header("Location", "/Web/")
            self.end_headers()
        else:
            super().do_GET()

handler = functools.partial(Handler, directory=str(root))
with ThreadingHTTPServer(("127.0.0.1", args.port), handler) as server:
    print(f"RLC Lab: http://127.0.0.1:{args.port}/Web/", flush=True)
    print("Остановка: Ctrl+C", flush=True)
    if args.open:
        webbrowser.open(f"http://127.0.0.1:{args.port}/Web/")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
