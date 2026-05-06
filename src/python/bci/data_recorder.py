import json
import csv
import os
import time
import threading
from typing import Optional, List
from collections import deque


class DataRecorder:
    def __init__(self, output_dir: str = "./session_data", format: str = "json"):
        self.output_dir = output_dir
        self.format = format
        self._buffer: deque = deque(maxlen=500)
        self._session_id: Optional[str] = None
        self._file = None
        self._is_recording = False
        self._lock = threading.Lock()
        self._flush_thread: Optional[threading.Thread] = None
        self._stop_flush = False
        self._row_count = 0

    def start_session(self, session_id: Optional[str] = None):
        if session_id is None:
            session_id = time.strftime("%Y%m%d_%H%M%S")
        self._session_id = session_id
        os.makedirs(self.output_dir, exist_ok=True)
        ext = "jsonl" if self.format == "json" else "csv"
        path = os.path.join(self.output_dir, f"session_{session_id}.{ext}")
        self._file = open(path, "w", encoding="utf-8")
        if self.format == "csv":
            self._file.write("t,a,y,p,r,sx,sy,b,q,bat\n")
        self._is_recording = True
        self._row_count = 0
        self._stop_flush = False
        self._flush_thread = threading.Thread(target=self._flush_loop, daemon=True)
        self._flush_thread.start()
        return path

    def end_session(self):
        self._is_recording = False
        self._stop_flush = True
        self._flush_buffer()
        if self._file:
            self._file.close()
            self._file = None
        summary = {
            "session_id": self._session_id,
            "end_time": time.time(),
            "total_rows": self._row_count,
        }
        summary_path = os.path.join(self.output_dir, f"session_{self._session_id}_summary.json")
        with open(summary_path, "w", encoding="utf-8") as f:
            json.dump(summary, f, indent=2)
        return summary

    def record(self, timestamp: float, attention: float,
               yaw: float = 0.0, pitch: float = 0.0, roll: float = 0.0,
               screen_x: float = 0.0, screen_y: float = 0.0,
               blink: int = 0, signal_quality: float = 1.0,
               battery: float = 100.0, event: str = ""):
        if not self._is_recording:
            return
        row = {
            "t": round(timestamp, 3),
            "a": round(attention, 1),
            "y": round(yaw, 2),
            "p": round(pitch, 2),
            "r": round(roll, 2),
            "sx": round(screen_x, 4),
            "sy": round(screen_y, 4),
            "b": blink,
            "q": round(signal_quality, 2),
            "bat": round(battery, 1),
            "ev": event,
        }
        with self._lock:
            self._buffer.append(row)

    def record_dict(self, data: dict):
        if not self._is_recording:
            return
        with self._lock:
            self._buffer.append(data)

    def _flush_buffer(self):
        if not self._file:
            return
        rows = []
        with self._lock:
            while self._buffer:
                rows.append(self._buffer.popleft())
        if not rows:
            return
        for row in rows:
            if self.format == "json":
                self._file.write(json.dumps(row, ensure_ascii=False) + "\n")
            else:
                self._file.write(
                    f"{row['t']},{row['a']},{row['y']},{row['p']},{row['r']},"
                    f"{row['sx']},{row['sy']},{row['b']},{row['q']},{row['bat']}\n"
                )
        self._file.flush()
        os.fsync(self._file.fileno())
        self._row_count += len(rows)

    def _flush_loop(self):
        while not self._stop_flush:
            time.sleep(2.0)
            self._flush_buffer()

    def get_session_path(self) -> Optional[str]:
        if not self._session_id:
            return None
        ext = "jsonl" if self.format == "json" else "csv"
        return os.path.join(self.output_dir, f"session_{self._session_id}.{ext}")
