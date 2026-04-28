"""
Socket服务器
负责与Unity客户端的通信
"""

import socket
import json
import threading
import time
from typing import Dict, Optional, Callable, Any
from dataclasses import dataclass
from .protocol import BCIProtocol, MessageType


@dataclass
class ServerConfig:
    """服务器配置"""
    host: str = '0.0.0.0'
    port: int = 5555
    max_clients: int = 1
    buffer_size: int = 4096
    timeout: float = 1.0
    heartbeat_interval: float = 1.0


class SocketServer:
    """
    Socket服务器
    负责与Unity客户端的通信
    """
    
    def __init__(self, config: Optional[ServerConfig] = None):
        """
        初始化Socket服务器
        
        Args:
            config: 服务器配置
        """
        if config is None:
            config = ServerConfig()
            
        self.config = config
        
        # 服务器状态
        self.is_running = False
        self.server_socket: Optional[socket.socket] = None
        self.client_socket: Optional[socket.socket] = None
        self.client_address: Optional[tuple] = None
        
        # 线程
        self.accept_thread: Optional[threading.Thread] = None
        self.receive_thread: Optional[threading.Thread] = None
        self.heartbeat_thread: Optional[threading.Thread] = None
        
        # 回调函数
        self.on_client_connected: Optional[Callable[[tuple], None]] = None
        self.on_client_disconnected: Optional[Callable[[], None]] = None
        self.on_message_received: Optional[Callable[[Dict], None]] = None
        self.on_attention_received: Optional[Callable[[float], None]] = None
        self.on_command_received: Optional[Callable[[str, Dict], None]] = None
        
        # 统计信息
        self.total_messages_sent = 0
        self.total_messages_received = 0
        self.last_message_time = 0.0
        
    def start(self) -> bool:
        """
        启动服务器
        
        Returns:
            bool: 是否启动成功
        """
        if self.is_running:
            return True
            
        try:
            # 创建服务器socket
            self.server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.server_socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
            self.server_socket.settimeout(self.config.timeout)
            self.server_socket.bind((self.config.host, self.config.port))
            self.server_socket.listen(self.config.max_clients)
            
            self.is_running = True
            
            # 启动接受连接线程
            self.accept_thread = threading.Thread(
                target=self._accept_connections,
                daemon=True
            )
            self.accept_thread.start()
            
            print(f"服务器启动在 {self.config.host}:{self.config.port}")
            return True
            
        except Exception as e:
            print(f"启动服务器失败: {e}")
            return False
            
    def stop(self):
        """停止服务器"""
        self.is_running = False
        
        # 关闭客户端连接
        self._disconnect_client()
        
        # 关闭服务器socket
        if self.server_socket:
            try:
                self.server_socket.close()
            except:
                pass
            self.server_socket = None
            
        # 等待线程结束
        if self.accept_thread:
            self.accept_thread.join(timeout=2.0)
            
        print("服务器已停止")
        
    def _accept_connections(self):
        """接受客户端连接"""
        while self.is_running:
            try:
                # 检查是否已有客户端连接
                if self.client_socket is not None:
                    time.sleep(0.1)
                    continue
                    
                # 接受新连接
                client_socket, address = self.server_socket.accept()
                client_socket.settimeout(self.config.timeout)
                
                self.client_socket = client_socket
                self.client_address = address
                
                print(f"客户端连接: {address}")
                
                # 触发连接回调
                if self.on_client_connected:
                    self.on_client_connected(address)
                    
                # 启动接收线程
                self.receive_thread = threading.Thread(
                    target=self._receive_messages,
                    daemon=True
                )
                self.receive_thread.start()
                
                # 启动心跳线程
                self.heartbeat_thread = threading.Thread(
                    target=self._send_heartbeats,
                    daemon=True
                )
                self.heartbeat_thread.start()
                
            except socket.timeout:
                continue
            except Exception as e:
                if self.is_running:
                    print(f"接受连接错误: {e}")
                break
                
    def _receive_messages(self):
        """接收客户端消息"""
        buffer = b""
        
        while self.is_running and self.client_socket:
            try:
                data = self.client_socket.recv(self.config.buffer_size)
                if not data:
                    print("客户端断开连接")
                    break
                    
                buffer += data
                
                # 处理完整的消息
                while b'\n' in buffer:
                    message_bytes, buffer = buffer.split(b'\n', 1)
                    if message_bytes:
                        self._process_message(message_bytes.decode('utf-8'))
                        
            except socket.timeout:
                continue
            except ConnectionError:
                print("连接错误")
                break
            except Exception as e:
                print(f"接收消息错误: {e}")
                break
                
        # 客户端断开
        self._disconnect_client()
        
    def _process_message(self, message_str: str):
        """
        处理接收到的消息
        
        Args:
            message_str: 消息字符串
        """
        try:
            message = BCIProtocol.parse_message(message_str)
            if message is None:
                print(f"无效的消息格式: {message_str[:100]}")
                return
                
            self.total_messages_received += 1
            self.last_message_time = time.time()
            
            # 触发消息接收回调
            if self.on_message_received:
                self.on_message_received(message)
                
            # 处理特定类型的消息
            msg_type = BCIProtocol.get_message_type(message)
            if msg_type == MessageType.ATTENTION:
                attention = message['data'].get('attention', 0.0)
                if self.on_attention_received:
                    self.on_attention_received(attention)
            elif msg_type == MessageType.COMMAND:
                command = message['data'].get('command', '')
                parameters = message['data'].get('parameters', {})
                if self.on_command_received:
                    self.on_command_received(command, parameters)
                    
        except Exception as e:
            print(f"处理消息错误: {e}")
            
    def _send_heartbeats(self):
        """发送心跳"""
        while self.is_running and self.client_socket:
            try:
                heartbeat = BCIProtocol.create_heartbeat_message()
                self.send_message(heartbeat)
                time.sleep(self.config.heartbeat_interval)
            except:
                break
                
    def _disconnect_client(self):
        """断开客户端连接"""
        if self.client_socket:
            try:
                self.client_socket.close()
            except:
                pass
            self.client_socket = None
            self.client_address = None
            
            # 触发断开连接回调
            if self.on_client_disconnected:
                self.on_client_disconnected()
                
        # 停止接收和心跳线程
        if self.receive_thread:
            self.receive_thread.join(timeout=1.0)
            self.receive_thread = None
            
        if self.heartbeat_thread:
            self.heartbeat_thread.join(timeout=1.0)
            self.heartbeat_thread = None
            
    def send_message(self, message: str) -> bool:
        """
        发送消息
        
        Args:
            message: 消息字符串
            
        Returns:
            bool: 是否发送成功
        """
        if not self.client_socket:
            return False
            
        try:
            self.client_socket.send(message.encode('utf-8'))
            self.total_messages_sent += 1
            return True
        except Exception as e:
            print(f"发送消息错误: {e}")
            return False
            
    def send_attention(self, attention: float, quality: float = 1.0,
                      alpha_power: float = 0.0, beta_power: float = 0.0) -> bool:
        """
        发送专注力数据
        
        Args:
            attention: 专注力值
            quality: 信号质量
            alpha_power: Alpha波功率
            beta_power: Beta波功率
            
        Returns:
            bool: 是否发送成功
        """
        message = BCIProtocol.create_attention_message(
            attention, quality, alpha_power, beta_power
        )
        return self.send_message(message)
        
    def send_imu(self, yaw: float, pitch: float, roll: float) -> bool:
        """
        发送IMU数据
        
        Args:
            yaw: 偏航角
            pitch: 俯仰角
            roll: 横滚角
            
        Returns:
            bool: 是否发送成功
        """
        message = BCIProtocol.create_imu_message(yaw, pitch, roll)
        return self.send_message(message)
        
    def send_game_state(self, state: str, score: int = 0,
                       attention: float = 0.0, focus_streak: float = 0.0) -> bool:
        """
        发送游戏状态
        
        Args:
            state: 游戏状态
            score: 分数
            attention: 专注力值
            focus_streak: 连续专注时间
            
        Returns:
            bool: 是否发送成功
        """
        message = BCIProtocol.create_game_state_message(
            state, score, attention, focus_streak
        )
        return self.send_message(message)
        
    def send_calibration_result(self, success: bool, 
                               baseline_alpha: float = 0.0,
                               baseline_beta: float = 0.0) -> bool:
        """
        发送校准结果
        
        Args:
            success: 是否成功
            baseline_alpha: Alpha基线
            baseline_beta: Beta基线
            
        Returns:
            bool: 是否发送成功
        """
        data = {
            'success': success,
            'baseline_alpha': baseline_alpha,
            'baseline_beta': baseline_beta
        }
        message = BCIProtocol.create_calibration_message('result', data)
        return self.send_message(message)
        
    def is_client_connected(self) -> bool:
        """
        检查客户端是否连接
        
        Returns:
            bool: 是否连接
        """
        return self.client_socket is not None
        
    def get_connection_info(self) -> Dict[str, Any]:
        """
        获取连接信息
        
        Returns:
            Dict: 连接信息
        """
        return {
            'is_running': self.is_running,
            'is_client_connected': self.is_client_connected(),
            'client_address': self.client_address,
            'host': self.config.host,
            'port': self.config.port,
            'total_messages_sent': self.total_messages_sent,
            'total_messages_received': self.total_messages_received,
            'last_message_time': self.last_message_time
        }
