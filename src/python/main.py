"""
BCI-VR注意力训练系统主模块
"""

import sys
import os
import argparse
import time
import signal

# 添加父目录到路径
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))

from bci.data_collector import BCIDataCollector
from bci.signal_processor import SignalProcessor
from bci.attention_calculator import AttentionCalculator
from bci.socket_server import SocketServer, ServerConfig
from bci.protocol import BCIProtocol


class BCIVRSystem:
    """
    BCI-VR系统主类
    整合所有BCI处理模块
    """
    
    def __init__(self, host='0.0.0.0', port=5555):
        """
        初始化系统
        
        Args:
            host: 服务器地址
            port: 服务器端口
        """
        self.host = host
        self.port = port
        
        # 初始化组件
        self.server = SocketServer(ServerConfig(host=host, port=port))
        self.processor = SignalProcessor()
        self.calculator = AttentionCalculator()
        
        # 状态
        self.is_running = False
        self.current_attention = 0.0
        self.current_yaw = 0.0
        self.current_pitch = 0.0
        
        # 注册回调
        self.server.on_client_connected = self._on_client_connected
        self.server.on_client_disconnected = self._on_client_disconnected
        
    def start(self):
        """启动系统"""
        print("=" * 50)
        print("BCI-VR 注意力训练系统")
        print("=" * 50)
        print(f"服务器地址: {self.host}:{self.port}")
        print("=" * 50)
        
        # 启动服务器
        if not self.server.start():
            print("启动服务器失败")
            return False
            
        self.is_running = True
        print("系统已启动，等待客户端连接...")
        print("按 Ctrl+C 停止系统")
        print("=" * 50)
        
        return True
        
    def stop(self):
        """停止系统"""
        print("\n正在停止系统...")
        self.is_running = False
        self.server.stop()
        print("系统已停止")
        
    def run(self):
        """运行系统主循环"""
        if not self.start():
            return
            
        try:
            while self.is_running:
                # 处理用户输入
                self._process_input()
                
                # 更新状态
                self._update_status()
                
                time.sleep(0.1)
                
        except KeyboardInterrupt:
            print("\n收到停止信号")
        finally:
            self.stop()
            
    def _process_input(self):
        """处理用户输入"""
        # 这里可以添加命令行交互
        pass
        
    def _update_status(self):
        """更新状态显示"""
        # 每10秒显示一次状态
        if int(time.time()) % 10 == 0:
            status = self.server.get_connection_info()
            if status['is_client_connected']:
                print(f"\r客户端已连接 | 消息数: {status['total_messages_received']} | "
                      f"专注力: {self.current_attention:.1f}", end='', flush=True)
                      
    def _on_client_connected(self, address):
        """客户端连接回调"""
        print(f"\n客户端已连接: {address}")
        
    def _on_client_disconnected(self):
        """客户端断开回调"""
        print("\n客户端已断开连接")
        
    def process_attention(self, raw_attention):
        """处理专注力数据"""
        # 信号处理
        filtered = self.processor.process_attention(raw_attention)
        
        # 计算专注力
        self.calculator.add_eeg_sample(
            ch1=raw_attention,
            ch2=raw_attention * 0.5,
            ch3=raw_attention * 0.3
        )
        result = self.calculator.calculate_attention()
        
        if result:
            self.current_attention = result.normalized_value
            
            # 发送到Unity
            self.server.send_attention(
                attention=self.current_attention,
                quality=result.quality,
                alpha_power=result.alpha_power,
                beta_power=result.beta_power
            )
            
        return self.current_attention
        
    def process_imu(self, yaw, pitch, roll):
        """处理IMU数据"""
        # 信号处理
        processed_yaw, processed_pitch = self.processor.process_imu(yaw, pitch)
        
        self.current_yaw = processed_yaw
        self.current_pitch = processed_pitch
        
        # 发送到Unity
        self.server.send_imu(processed_yaw, processed_pitch, roll)
        
        return processed_yaw, processed_pitch


def main():
    """主函数"""
    parser = argparse.ArgumentParser(description='BCI-VR 注意力训练系统')
    parser.add_argument('--host', default='0.0.0.0', help='服务器地址')
    parser.add_argument('--port', type=int, default=5555, help='服务器端口')
    parser.add_argument('--test', action='store_true', help='运行测试模式')
    
    args = parser.parse_args()
    
    if args.test:
        # 测试模式
        print("运行测试模式...")
        run_test_mode(args.host, args.port)
    else:
        # 正常模式
        system = BCIVRSystem(host=args.host, port=args.port)
        system.run()


def run_test_mode(host, port):
    """运行测试模式"""
    print(f"启动测试服务器 {host}:{port}")
    
    server = SocketServer(ServerConfig(host=host, port=port))
    
    def on_client_connected(address):
        print(f"客户端已连接: {address}")
        
        # 发送测试数据
        import random
        for i in range(100):
            attention = random.uniform(50, 100)
            yaw = random.uniform(-30, 30)
            pitch = random.uniform(-20, 20)
            
            server.send_attention(attention)
            server.send_imu(yaw, pitch, 0)
            
            print(f"发送数据: 专注力={attention:.1f}, 偏航={yaw:.1f}, 俯仰={pitch:.1f}")
            time.sleep(0.1)
            
    server.on_client_connected = on_client_connected
    
    if server.start():
        print("测试服务器已启动，等待连接...")
        print("按 Ctrl+C 停止")
        
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("\n停止测试服务器")
            server.stop()


if __name__ == '__main__':
    main()
