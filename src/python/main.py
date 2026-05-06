import sys
import os
import argparse
import time
import signal
import yaml

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from bci.hybridbci_adapter import HybridBCIAdapter, BCIData
from bci.signal_processor import SignalProcessor
from bci.socket_server import SocketServer, ServerConfig
from bci.data_recorder import DataRecorder


def load_config(path: str = None) -> dict:
    if path is None:
        path = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "config.yaml")
    if not os.path.exists(path):
        print(f"配置文件不存在: {path}，使用默认配置")
        return {
            "data_source": {"mode": "mock"},
            "mock": {"attention_min": 30, "attention_max": 90, "attention_freq": 0.1,
                     "imu_noise": 3.0, "update_rate": 50},
            "unity_server": {"host": "0.0.0.0", "port": 5555},
            "signal_processing": {"window_size": 7, "alpha": 0.7, "imu_deadzone": 5.0},
            "recording": {"enabled": True, "output_dir": "./session_data"},
        }
    with open(path, "r", encoding="utf-8") as f:
        return yaml.safe_load(f)


class BCIVRSystem:
    def __init__(self, config: dict):
        self.config = config
        self.is_running = False

        ds = config.get("data_source", {})
        mode = ds.get("mode", "mock")

        if mode == "local":
            self.adapter = HybridBCIAdapter(
                mode=HybridBCIAdapter.MODE_LOCAL,
                server_name=config.get("local", {}).get("server_name", "HNNKPlatform"),
            )
        elif mode == "tcp":
            tcp_cfg = config.get("tcp", {})
            self.adapter = HybridBCIAdapter(
                mode=HybridBCIAdapter.MODE_TCP,
                host=tcp_cfg.get("host", "127.0.0.1"),
                port=tcp_cfg.get("port", 8000),
            )
        else:
            mock = config.get("mock", {})
            self.adapter = HybridBCIAdapter(
                mode=HybridBCIAdapter.MODE_MOCK,
                attention_min=mock.get("attention_min", 30.0),
                attention_max=mock.get("attention_max", 90.0),
                attention_freq=mock.get("attention_freq", 0.1),
                imu_noise=mock.get("imu_noise", 3.0),
                update_rate=mock.get("update_rate", 50),
            )

        sp_cfg = config.get("signal_processing", {})
        self.processor = SignalProcessor(
            window_size=sp_cfg.get("window_size", 7),
            alpha=sp_cfg.get("alpha", 0.7),
            deadzone=sp_cfg.get("imu_deadzone", 5.0),
        )

        unity_cfg = config.get("unity_server", {})
        self.server = SocketServer(ServerConfig(
            host=unity_cfg.get("host", "0.0.0.0"),
            port=unity_cfg.get("port", 5555),
        ))

        rec_cfg = config.get("recording", {})
        self.recorder = DataRecorder(
            output_dir=rec_cfg.get("output_dir", "./session_data"),
            format="json",
        )

        self.current_attention = 0.0
        self.current_yaw = 0.0
        self.current_pitch = 0.0

        self.adapter.on_data_received = self._on_bci_data
        self.adapter.on_connection_changed = lambda c: print(f"{'✓' if c else '✗'} 平台连接: {'已建立' if c else '已断开'}")
        self.adapter.on_error = lambda e: print(f"⚠ 错误: {e}")

        self.server.on_client_connected = lambda addr: print(f"✓ Unity客户端连接: {addr}")
        self.server.on_client_disconnected = lambda: print("✗ Unity客户端断开")

    def start(self):
        banner = """
╔══════════════════════════════════════════╗
║     BCI-VR 注意力训练系统                  ║
║     基于可穿戴多模态BCI与VR融合的专注力训练  ║
╚══════════════════════════════════════════╝
"""
        print(banner)
        print(f"数据源模式: {self.adapter.mode.upper()}")
        print(f"Unity服务器: {self.config['unity_server']['host']}:{self.config['unity_server']['port']}")
        print(f"信号处理: 窗口={self.config['signal_processing']['window_size']}, "
              f"alpha={self.config['signal_processing']['alpha']}, "
              f"死区={self.config['signal_processing']['imu_deadzone']}")
        print("-" * 50)

        if not self.adapter.start():
            print("启动BCI适配器失败")
            return False
        if not self.server.start():
            print("启动Unity服务器失败")
            return False
        self.recorder.start_session()

        self.is_running = True
        print("系统运行中... 按 Ctrl+C 停止")
        print()
        return True

    def stop(self):
        print("\n正在停止系统...")
        self.is_running = False
        self.adapter.stop()
        self.server.stop()
        self.recorder.end_session()
        print("系统已停止。")

    def _on_bci_data(self, data: BCIData):
        att = data.attention
        if att != 0.0:
            self.current_attention = self.processor.process_attention(att)
        self.current_yaw, self.current_pitch = self.processor.process_imu(data.yaw, data.pitch)

        self.server.send_attention(round(self.current_attention, 1), round(data.signal_quality, 2))
        self.server.send_imu(round(data.yaw, 1), round(data.pitch, 1), round(data.roll, 1))

        if self.recorder:
            self.recorder.record(
                timestamp=data.timestamp,
                attention=self.current_attention,
                yaw=data.yaw, pitch=data.pitch, roll=data.roll,
                screen_x=data.screen_x, screen_y=data.screen_y,
                blink=data.blink, signal_quality=data.signal_quality,
                battery=data.battery,
            )

    def run(self):
        if not self.start():
            return
        try:
            while self.is_running:
                self._status_line()
                time.sleep(0.5)
        except KeyboardInterrupt:
            print()
        finally:
            self.stop()

    def _status_line(self):
        status = self.adapter.get_status()
        connected = "✓" if status["connected"] else "✗"
        line = (f"\r[{time.strftime('%H:%M:%S')}] "
                f"平台:{connected} "
                f"专注力:{self.current_attention:5.1f} "
                f"Yaw:{self.current_yaw:+6.1f} Pitch:{self.current_pitch:+6.1f} "
                f"包:{status['packets_received']} "
                f"运行:{status['uptime_sec']:.0f}s   ")
        sys.stdout.write(line)
        sys.stdout.flush()


def main():
    parser = argparse.ArgumentParser(description="BCI-VR 注意力训练系统")
    parser.add_argument("--config", default=None, help="配置文件路径")
    parser.add_argument("--mode", choices=["mock", "local", "tcp"], default=None, help="数据源模式（覆盖配置）")
    args = parser.parse_args()

    config = load_config(args.config)
    if args.mode:
        config["data_source"]["mode"] = args.mode

    system = BCIVRSystem(config)
    system.run()


if __name__ == "__main__":
    main()
