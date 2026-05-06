import struct
import json
import threading
import time
import random
import math
import socket
from typing import Optional, Callable, Dict, Any
from collections import deque

try:
    from PyQt5.QtCore import QByteArray, QDataStream, QIODevice
    from PyQt5.QtNetwork import QLocalSocket
    HAS_QT = True
except ImportError:
    HAS_QT = False


class BCIData:
    def __init__(self, timestamp: float = 0.0, attention: float = 0.0,
                 yaw: float = 0.0, pitch: float = 0.0, roll: float = 0.0,
                 screen_x: float = 0.0, screen_y: float = 0.0,
                 blink: int = 0, signal_quality: float = 1.0,
                 battery: float = 100.0):
        self.timestamp = timestamp
        self.attention = attention
        self.yaw = yaw
        self.pitch = pitch
        self.roll = roll
        self.screen_x = screen_x
        self.screen_y = screen_y
        self.blink = blink
        self.signal_quality = signal_quality
        self.battery = battery

    def to_dict(self) -> dict:
        return {
            "t": self.timestamp,
            "a": self.attention,
            "y": self.yaw,
            "p": self.pitch,
            "r": self.roll,
            "sx": self.screen_x,
            "sy": self.screen_y,
            "b": self.blink,
            "q": self.signal_quality,
            "bat": self.battery
        }

    @classmethod
    def from_dict(cls, d: dict):
        return cls(
            timestamp=d.get("t", 0.0),
            attention=d.get("a", 0.0),
            yaw=d.get("y", 0.0),
            pitch=d.get("p", 0.0),
            roll=d.get("r", 0.0),
            screen_x=d.get("sx", 0.0),
            screen_y=d.get("sy", 0.0),
            blink=d.get("b", 0),
            signal_quality=d.get("q", 1.0),
            battery=d.get("bat", 100.0)
        )


class HybridBCIAdapter:
    MODE_MOCK = "mock"
    MODE_LOCAL = "local"
    MODE_TCP = "tcp"

    def __init__(self, mode: str = MODE_MOCK, **kwargs):
        self.mode = mode
        self.server_name = kwargs.get("server_name", "HNNKPlatform")
        self.host = kwargs.get("host", "127.0.0.1")
        self.port = kwargs.get("port", 8000)

        self.mock_config = {
            "attention_min": kwargs.get("attention_min", 30.0),
            "attention_max": kwargs.get("attention_max", 90.0),
            "attention_freq": kwargs.get("attention_freq", 0.1),
            "imu_noise": kwargs.get("imu_noise", 3.0),
            "update_rate": kwargs.get("update_rate", 50),
        }

        self.is_running = False
        self.is_connected = False
        self._thread: Optional[threading.Thread] = None
        self._recv_buffer = bytearray()
        self._socket = None
        self._local_socket = None

        self.on_data_received: Optional[Callable[[BCIData], None]] = None
        self.on_attention_updated: Optional[Callable[[float], None]] = None
        self.on_imu_updated: Optional[Callable[[float, float, float, float, float], None]] = None
        self.on_blink_detected: Optional[Callable[[int], None]] = None
        self.on_connection_changed: Optional[Callable[[bool], None]] = None
        self.on_error: Optional[Callable[[str], None]] = None

        self.stats = {
            "packet_count": 0,
            "bytes_received": 0,
            "start_time": 0.0,
            "last_packet_time": 0.0,
        }

    def start(self) -> bool:
        if self.is_running:
            return True
        self.is_running = True
        self.stats["start_time"] = time.time()

        if self.mode == self.MODE_MOCK:
            self._thread = threading.Thread(target=self._mock_loop, daemon=True)
            self._thread.start()
            self.is_connected = True
            self._notify_connection(True)
            return True
        elif self.mode == self.MODE_TCP:
            return self._connect_tcp()
        elif self.mode == self.MODE_LOCAL:
            return self._connect_local()
        return False

    def stop(self):
        self.is_running = False
        if self._local_socket:
            try:
                self._local_socket.disconnectFromServer()
            except Exception:
                pass
            self._local_socket = None
        if self._socket:
            try:
                self._socket.close()
            except Exception:
                pass
            self._socket = None
        self.is_connected = False
        self._notify_connection(False)

    def _notify_connection(self, connected: bool):
        if self.on_connection_changed:
            self.on_connection_changed(connected)

    def _connect_tcp(self) -> bool:
        try:
            self._socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self._socket.settimeout(5.0)
            self._socket.connect((self.host, self.port))
            self.is_connected = True
            self._notify_connection(True)
            self._thread = threading.Thread(target=self._tcp_receive_loop, daemon=True)
            self._thread.start()
            return True
        except Exception as e:
            self._emit_error(f"TCP连接失败: {e}")
            self.is_connected = False
            return False

    def _connect_local(self) -> bool:
        if not HAS_QT:
            self._emit_error("PyQt5未安装，无法使用LocalSocket模式")
            return False
        try:
            self._local_socket = QLocalSocket()
            self._local_socket.connected.connect(lambda: self._on_local_connected())
            self._local_socket.disconnected.connect(lambda: self._on_local_disconnected())
            self._local_socket.readyRead.connect(self._on_local_ready_read)
            self._local_socket.connectToServer(self.server_name)
            return True
        except Exception as e:
            self._emit_error(f"LocalSocket连接失败: {e}")
            return False

    def _on_local_connected(self):
        self.is_connected = True
        self._notify_connection(True)

    def _on_local_disconnected(self):
        self.is_connected = False
        self._notify_connection(False)

    def _on_local_ready_read(self):
        if not self._local_socket:
            return
        data = self._local_socket.readAll()
        self._recv_buffer.extend(data)
        self._parse_packets()

    def _tcp_receive_loop(self):
        while self.is_running and self.is_connected:
            try:
                chunk = self._socket.recv(4096)
                if not chunk:
                    break
                self._recv_buffer.extend(chunk)
                self._parse_packets()
            except socket.timeout:
                continue
            except Exception as e:
                if self.is_running:
                    self._emit_error(f"TCP接收错误: {e}")
                break
        self.is_connected = False
        self._notify_connection(False)

    def _parse_packets(self):
        while len(self._recv_buffer) >= 4:
            payload_len = struct.unpack(">I", self._recv_buffer[0:4])[0]
            total_len = 4 + payload_len
            if len(self._recv_buffer) < total_len:
                break
            payload = bytes(self._recv_buffer[4:total_len])
            self._recv_buffer = self._recv_buffer[total_len:]
            self.stats["packet_count"] += 1
            self.stats["bytes_received"] += total_len
            self.stats["last_packet_time"] = time.time()
            try:
                data = self._interpret_payload(payload)
                if data:
                    self._emit_data(data)
            except Exception as e:
                self._emit_error(f"数据解析错误: {e}")

    def _interpret_payload(self, payload: bytes) -> Optional[BCIData]:
        try:
            text = payload.decode("utf-8")
            obj = json.loads(text)
            now = time.time()
            return BCIData(
                timestamp=obj.get("timestamp", now),
                attention=float(obj.get("attention", obj.get("data", {}).get("attention", 0.0))),
                yaw=float(obj.get("yaw", obj.get("data", {}).get("yaw", 0.0))),
                pitch=float(obj.get("pitch", obj.get("data", {}).get("pitch", 0.0))),
                roll=float(obj.get("roll", obj.get("data", {}).get("roll", 0.0))),
                screen_x=float(obj.get("screen_x", obj.get("data", {}).get("screen_x", 0.0))),
                screen_y=float(obj.get("screen_y", obj.get("data", {}).get("screen_y", 0.0))),
                blink=int(obj.get("blink", obj.get("data", {}).get("blink", 0))),
                signal_quality=float(obj.get("signal_quality", obj.get("data", {}).get("quality", 1.0))),
                battery=float(obj.get("battery", obj.get("data", {}).get("battery", 100.0))),
            )
        except (UnicodeDecodeError, json.JSONDecodeError):
            pass
        try:
            fmt = ">fffffIb"
            header_size = struct.calcsize(fmt)
            if len(payload) >= header_size:
                values = struct.unpack(fmt, payload[:header_size])
                return BCIData(
                    timestamp=time.time(),
                    attention=values[0],
                    yaw=values[1],
                    pitch=values[2],
                    roll=values[3],
                    screen_x=values[4],
                    screen_y=values[5],
                    blink=values[6],
                    signal_quality=values[7] if len(values) > 7 else 1.0,
                )
        except struct.error:
            pass
        self._emit_error(f"未知payload格式: {payload[:50]}")
        return None

    def _emit_data(self, data: BCIData):
        if self.on_data_received:
            self.on_data_received(data)
        if self.on_attention_updated:
            self.on_attention_updated(data.attention)
        if self.on_imu_updated:
            self.on_imu_updated(data.yaw, data.pitch, data.roll, data.screen_x, data.screen_y)
        if self.on_blink_detected and data.blink != 0:
            self.on_blink_detected(data.blink)

    def _emit_error(self, msg: str):
        if self.on_error:
            self.on_error(msg)

    def _mock_loop(self):
        cfg = self.mock_config
        rate = cfg["update_rate"]
        interval = 1.0 / rate
        t = 0.0

        attention_base = (cfg["attention_min"] + cfg["attention_max"]) / 2
        attention_amp = (cfg["attention_max"] - cfg["attention_min"]) / 2
        attention_phase = random.uniform(0, 2 * math.pi)

        while self.is_running:
            t += interval
            raw_attention = (attention_base
                             + attention_amp * math.sin(2 * math.pi * cfg["attention_freq"] * t + attention_phase)
                             + random.gauss(0, 5))
            attention = max(0, min(100, raw_attention))
            yaw = random.gauss(0, cfg["imu_noise"])
            pitch = random.gauss(0, cfg["imu_noise"])
            roll = random.gauss(0, cfg["imu_noise"] * 0.5)
            blink = 1 if random.random() < 0.002 else 0

            data = BCIData(
                timestamp=time.time(),
                attention=round(attention, 1),
                yaw=round(yaw, 1),
                pitch=round(pitch, 1),
                roll=round(roll, 1),
                screen_x=round(yaw / 90, 4),
                screen_y=round(pitch / 90, 4),
                blink=blink,
                signal_quality=random.uniform(0.8, 1.0),
                battery=100.0 - t * 0.001,
            )
            self._emit_data(data)
            time.sleep(interval)

    def get_status(self) -> dict:
        uptime = time.time() - self.stats["start_time"] if self.stats["start_time"] else 0
        return {
            "mode": self.mode,
            "running": self.is_running,
            "connected": self.is_connected,
            "uptime_sec": round(uptime, 1),
            "packets_received": self.stats["packet_count"],
            "bytes_received": self.stats["bytes_received"],
            "last_packet_sec_ago": round(time.time() - self.stats["last_packet_time"], 2) if self.stats["last_packet_time"] else -1,
        }

    def send_command(self, msg_type: str, data: dict = None):
        if data is None:
            data = {}
        packet = json.dumps({"type": msg_type, "data": data, "timestamp": time.time()}).encode("utf-8")
        framed = struct.pack(">I", len(packet)) + packet
        if self.mode == self.MODE_TCP and self._socket:
            try:
                self._socket.sendall(framed)
            except Exception as e:
                self._emit_error(f"发送命令失败: {e}")
        elif self.mode == self.MODE_LOCAL and self._local_socket:
            try:
                self._local_socket.write(framed)
            except Exception as e:
                self._emit_error(f"发送命令失败: {e}")


KeyboardControl = None


class MockKeyboardController:
    def __init__(self, adapter: HybridBCIAdapter):
        self.adapter = adapter
        self._override_yaw = 0.0
        self._override_pitch = 0.0
        self._thread = threading.Thread(target=self._listen, daemon=True)
        self._thread.start()

    def _listen(self):
        import sys, select, termios, tty
        fd = sys.stdin.fileno()
        old = termios.tcgetattr(fd)
        tty.setraw(fd)
        try:
            while self.adapter.is_running:
                if select.select([sys.stdin], [], [], 0.05)[0]:
                    ch = sys.stdin.read(1).lower()
                    step = 10.0
                    if ch == "a":
                        self._override_yaw = max(-90, self._override_yaw - step)
                    elif ch == "d":
                        self._override_yaw = min(90, self._override_yaw + step)
                    elif ch == "w":
                        self._override_pitch = min(90, self._override_pitch + step)
                    elif ch == "s":
                        self._override_pitch = max(-90, self._override_pitch - step)
                    elif ch == " ":
                        pass
                    elif ch == "q":
                        break
        finally:
            termios.tcsetattr(fd, termios.TCSADRAIN, old)
