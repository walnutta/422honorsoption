
import asyncio
import json
import logging
import time
from io import BytesIO

import websockets
from PIL import Image
from ultralytics import YOLO


HOST = "0.0.0.0"   # Listen on all interfaces so Quest 3 can reach it over wifi
PORT = 8765
MODEL_PATH = "yolov8s.pt"
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

            img_w, img_h = image.size

            # Run YOLOv8 inference on GPU
            results = model(image, conf=CONFIDENCE_THRESHOLD, verbose=False)

            # Parse detections into a plain list of dicts
            detections = []
            for result in results:
                for box in result.boxes:
                    x1, y1, x2, y2 = box.xyxy[0].tolist()

                    # Pixel-space center and size
                    cx_px = (x1 + x2) / 2.0
                    cy_px = (y1 + y2) / 2.0
                    w_px  = x2 - x1
                    h_px  = y2 - y1

                    # Normalized [0..1] — cx/cy are proportional image coords.
                    # Unity will use these to build the viewport ray and estimate
                    # world-space box size at the hit depth.
                    cx_n = cx_px / img_w
                    cy_n = cy_px / img_h
                    w_n  = w_px  / img_w
                    h_n  = h_px  / img_h

                    detections.append({
                        "label":      result.names[int(box.cls)],
                        "confidence": round(float(box.conf), 3),
                        "bbox": [round(v, 1) for v in [x1, y1, x2, y2]],
                        # Normalized center + size for 3-D world projection
                        # bbox_norm = [cx, cy, width, height] all in [0..1]
                        "bbox_norm": [
                            round(cx_n, 4),
                            round(cy_n, 4),
                            round(w_n,  4),
                            round(h_n,  4),
                        ],
                    })

            t_elapsed_ms = round((time.perf_counter() - t_start) * 1000, 1)

            # Send results back as JSON
            response = {
                "detections":   detections,
                "inference_ms": t_elapsed_ms,
                "image_size":   [img_w, img_h],
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