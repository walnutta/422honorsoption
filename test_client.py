"""
test_client.py
--------------
Run this on your LOCAL machine (or the server itself) to verify
the inference server is working before you touch Unity at all.

Usage:
    python test_client.py --host <server_ip> --image <path_to_jpg>

Example:
    python test_client.py --host 192.168.1.50 --image test.jpg

Install:
    pip install websockets pillow
"""

import argparse
import asyncio
import json
import time

import websockets


async def send_image(host: str, port: int, image_path: str):
    uri = f"ws://{host}:{port}"
    print(f"Connecting to {uri} ...")

    async with websockets.connect(uri) as ws:
        print(f"Connected. Sending image: {image_path}")

        with open(image_path, "rb") as f:
            image_bytes = f.read()

        t0 = time.perf_counter()
        await ws.send(image_bytes)
        raw_response = await ws.recv()
        round_trip_ms = round((time.perf_counter() - t0) * 1000, 1)

        response = json.loads(raw_response)

        print(f"\n── Results ──────────────────────────────")
        print(f"Round-trip latency : {round_trip_ms} ms")
        print(f"Server inference   : {response.get('inference_ms')} ms")
        print(f"Network overhead   : ~{round_trip_ms - response.get('inference_ms', 0)} ms")
        print(f"Image size         : {response.get('image_size')}")
        print(f"Detections ({len(response['detections'])}):")
        for d in response["detections"]:
            print(f"  {d['label']:20s}  conf={d['confidence']}  bbox={d['bbox']}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser()
    parser.add_argument("--host", default="localhost", help="Server IP address")
    parser.add_argument("--port", default=8765, type=int)
    parser.add_argument("--image", required=True, help="Path to a JPEG image file")
    args = parser.parse_args()

    asyncio.run(send_image(args.host, args.port, args.image))