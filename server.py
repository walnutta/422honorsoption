"""
inference_server.py
-------------------
WebSocket server that receives a JPEG image from a client,
runs YOLOv8 object detection on GPU, and returns JSON results.

Install dependencies:
    pip install ultralytics websockets pillow

Run:
    python inference_server.py
"""

import asyncio
import json
import logging
import time
from io import BytesIO

import websockets
from PIL import Image
from ultralytics import YOLO


HOST = "0.0.0.0"   # Listen on all interfaces so Quest 3 can reach it over WiFi
PORT = 8765
MODEL_PATH = "yolov8n.pt"  # nano = fastest; swap for yolov8s.pt / yolov8m.pt if you want more accuracy
CONFIDENCE_THRESHOLD = 0.4

logging.basicConfig(level=logging.INFO, format="%(asctime)s [%(levelname)s] %(message)s")
log = logging.getLogger(__name__)

# Load model once at startup (not per-request)
log.info(f"Loading model: {MODEL_PATH}")
model = YOLO(MODEL_PATH)
log.info("Model loaded. Server ready.")


async def handle_client(websocket):
    """
    Called once per connected client.
    Stays in the loop receiving frames until the client disconnects.
    """
    client_addr = websocket.remote_address
    log.info(f"Client connected: {client_addr}")

    try:
        async for message in websocket:
            t_start = time.perf_counter()

            # Decode the incoming JPEG bytes into a PIL Image 
            try:
                image = Image.open(BytesIO(message)).convert("RGB")
            except Exception as e:
                log.warning(f"Failed to decode image: {e}")
                await websocket.send(json.dumps({"error": "invalid image"}))
                continue

            # Run YOLOv8 inference on GPU
            results = model(image, conf=CONFIDENCE_THRESHOLD, verbose=False)

            # Parse detections into a plain list of dicts 
            detections = []
            for result in results:
                for box in result.boxes:
                    detections.append({
                        "label": result.names[int(box.cls)],  
                        "confidence": round(float(box.conf), 3),
                        # Bounding box as [x1, y1, x2, y2] in pixel coords
                        "bbox": [round(v, 1) for v in box.xyxy[0].tolist()],
                    })

            t_elapsed_ms = round((time.perf_counter() - t_start) * 1000, 1)

            # Send results back as JSON 
            response = {
                "detections": detections,
                "inference_ms": t_elapsed_ms,
                "image_size": list(image.size),  # [width, height]
            }
            await websocket.send(json.dumps(response))

            log.info(f"{client_addr} | {len(detections)} detections | {t_elapsed_ms}ms")

    except websockets.exceptions.ConnectionClosedOK:
        log.info(f"Client disconnected cleanly: {client_addr}")
    except websockets.exceptions.ConnectionClosedError as e:
        log.warning(f"Client disconnected with error: {client_addr} — {e}")


async def main():
    log.info(f"Starting WebSocket server on ws://{HOST}:{PORT}")
    async with websockets.serve(handle_client, HOST, PORT):
        await asyncio.Future()  # Run forever


if __name__ == "__main__":
    asyncio.run(main())