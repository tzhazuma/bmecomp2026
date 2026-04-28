"""
BCI数据采集器
负责从脑电头环采集EEG和IMU数据
"""

import socket
import json
import threading
import time
from collections import deque
from typing import Dict, Optional, List, Callable
from dataclasses import dataclass, field


@dataclass
class EEGData:
    """EEG数据结构"""
    timestamp: float
    channel_1: float  # 主通道
    channel_2: float  # 参考通道1
    channel_3: float  # 参考通道2
    quality: float    # 信号质量


@dataclass
class IMUData:
    """IMU数据结构"""
    timestamp: float
    yaw: float      # 偏航角
    pitch: float    # 俯仰角
    roll: float     # 横滚角
    accel_x: float  # X轴加速度
    accel_y: float  # Y轴加速度
    accel_z: float  # Z轴加速度


@dataclass
class BCIData:
    """BCI完整数据结构"""
    timestamp: float
    attention: float  # 专注力值 (0-100)
    eeg: Optional[EEGData] = None
    imu: Optional[IMUData] = None
    battery: float = 100.0
    signal_quality: float = 1.0


class BCIDataCollector:
    """
    BCI数据采集器
    负责从脑电头环采集数据并缓存
    """
    
    def __init__(self, 
                 host: str = 'localhost', 
                 port: int = 5555, 
                 buffer_size: int = 1000,
                 auto_reconnect: bool = True):
        """
        初始化BCI数据采集器
        
        Args:
            host: 服务器地址
            port: 服务器端口
            buffer_size: 数据缓冲区大小
            auto_reconnect: 是否自动重连
        """
        self.host = host
        self.port = port
        self.buffer_size = buffer_size
        self.auto_reconnect = auto_reconnect
        
        # 数据缓冲区
        self.attention_buffer: deque = deque(maxlen=buffer_size)
        self.eeg_buffer: deque = deque(maxlen=buffer_size)
        self.imu_buffer: deque = deque(maxlen=buffer_size)
        self.raw_data_buffer: deque = deque(maxlen=buffer_size)
        
        # 连接状态
        self.is_running = False
        self.is_connected = False
        self.socket: Optional[socket.socket] = None
        self.receive_thread: Optional[threading.Thread] = None
        
        # 回调函数
        self.on_data_received: Optional[Callable[[BCIData], None]] = None
        self.on_attention_updated: Optional[Callable[[float], None]] = None
        self.on_connection_changed: Optional[Callable[[bool], None]] = None
        
        # 统计信息
        self.total_packets = 0
        self.last_receive_time = 0.0
        self.receive_rate = 0.0
        
    def start(self) -> bool:
        """
        启动数据采集
        
        Returns:
            bool: 是否启动成功
        """
        if self.is_running:
            return True
            
        try:
            self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            self.socket.settimeout(5.0)  # 5秒超时
            self.socket.connect((self.host, self.port))
            
            self.is_connected = True
            self.is_running = True
            
            # 启动接收线程
            self.receive_thread = threading.Thread(
                target=self._receive_data_loop,
                daemon=True
            )
            self.receive_thread.start()
            
            # 触发连接状态回调
            if self.on_connection_changed:
                self.on_connection_changed(True)
                
            print(f"已连接到BCI服务器 {self.host}:{self.port}")
            return True
            
        except Exception as e:
            print(f"连接BCI服务器失败: {e}")
            self.is_connected = False
            return False
            
    def stop(self):
        """停止数据采集"""
        self.is_running = False
        self.is_connected = False
        
        if self.socket:
            try:
                self.socket.close()
            except:
                pass
            self.socket = None
            
        if self.receive_thread:
            self.receive_thread.join(timeout=2.0)
            self.receive_thread = None
            
        # 触发连接状态回调
        if self.on_connection_changed:
            self.on_connection_changed(False)
            
        print("已断开BCI服务器连接")
        
    def _receive_data_loop(self):
        """数据接收循环"""
        buffer = b""
        last_time = time.time()
        packet_count = 0
        
        while self.is_running and self.is_connected:
            try:
                data = self.socket.recv(4096)
                if not data:
                    print("服务器关闭连接")
                    break
                    
                buffer += data
                
                # 处理完整的消息
                while b'\n' in buffer:
                    message, buffer = buffer.split(b'\n', 1)
                    if message:
                        self._process_message(message.decode('utf-8'))
                        packet_count += 1
                        
                # 计算接收速率
                current_time = time.time()
                if current_time - last_time >= 1.0:
                    self.receive_rate = packet_count / (current_time - last_time)
                    packet_count = 0
                    last_time = current_time
                    
            except socket.timeout:
                continue
            except ConnectionError:
                print("连接错误")
                break
            except Exception as e:
                print(f"接收数据错误: {e}")
                break
                
        # 连接断开
        self.is_connected = False
        if self.on_connection_changed:
            self.on_connection_changed(False)
            
        # 自动重连
        if self.auto_reconnect and self.is_running:
            print("尝试重新连接...")
            time.sleep(2.0)
            self.start()
            
    def _process_message(self, message: str):
        """
        处理接收到的消息
        
        Args:
            message: JSON格式的消息字符串
        """
        try:
            data = json.loads(message)
            self._process_data(data)
        except json.JSONDecodeError as e:
            print(f"JSON解析错误: {e}")
        except Exception as e:
            print(f"处理消息错误: {e}")
            
    def _process_data(self, data: Dict):
        """
        处理解析后的数据
        
        Args:
            data: 解析后的数据字典
        """
        timestamp = data.get('timestamp', time.time())
        
        # 创建BCI数据对象
        bci_data = BCIData(timestamp=timestamp, attention=0.0)
        
        # 处理专注力数据
        if 'attention' in data:
            attention = float(data['attention'])
            bci_data.attention = attention
            self.attention_buffer.append({
                'timestamp': timestamp,
                'value': attention
            })
            if self.on_attention_updated:
                self.on_attention_updated(attention)
                
        # 处理EEG数据
        if 'eeg' in data:
            eeg_data = data['eeg']
            bci_data.eeg = EEGData(
                timestamp=timestamp,
                channel_1=eeg_data.get('ch1', 0.0),
                channel_2=eeg_data.get('ch2', 0.0),
                channel_3=eeg_data.get('ch3', 0.0),
                quality=eeg_data.get('quality', 1.0)
            )
            self.eeg_buffer.append(bci_data.eeg)
            
        # 处理IMU数据
        if 'imu' in data:
            imu_data = data['imu']
            bci_data.imu = IMUData(
                timestamp=timestamp,
                yaw=imu_data.get('yaw', 0.0),
                pitch=imu_data.get('pitch', 0.0),
                roll=imu_data.get('roll', 0.0),
                accel_x=imu_data.get('accel_x', 0.0),
                accel_y=imu_data.get('accel_y', 0.0),
                accel_z=imu_data.get('accel_z', 0.0)
            )
            self.imu_buffer.append(bci_data.imu)
            
        # 处理电池信息
        if 'battery' in data:
            bci_data.battery = float(data['battery'])
            
        # 处理信号质量
        if 'signal_quality' in data:
            bci_data.signal_quality = float(data['signal_quality'])
            
        # 保存原始数据
        self.raw_data_buffer.append(data)
        
        # 更新统计
        self.total_packets += 1
        self.last_receive_time = timestamp
        
        # 触发数据接收回调
        if self.on_data_received:
            self.on_data_received(bci_data)
            
    def get_attention(self) -> Optional[float]:
        """
        获取最新专注力值
        
        Returns:
            Optional[float]: 最新专注力值，如果没有数据则返回None
        """
        if self.attention_buffer:
            return self.attention_buffer[-1]['value']
        return None
        
    def get_imu(self) -> Optional[Dict]:
        """
        获取最新IMU数据
        
        Returns:
            Optional[Dict]: 最新IMU数据，如果没有数据则返回None
        """
        if self.imu_buffer:
            return self.imu_buffer[-1]
        return None
        
    def get_attention_history(self, n_samples: Optional[int] = None) -> List[Dict]:
        """
        获取专注力历史数据
        
        Args:
            n_samples: 获取的样本数量，None表示获取所有
            
        Returns:
            List[Dict]: 专注力历史数据列表
        """
        if n_samples is None:
            return list(self.attention_buffer)
        return list(self.attention_buffer)[-n_samples:]
        
    def get_average_attention(self, n_samples: int = 100) -> float:
        """
        获取平均专注力值
        
        Args:
            n_samples: 计算平均值的样本数量
            
        Returns:
            float: 平均专注力值
        """
        if not self.attention_buffer:
            return 0.0
            
        samples = list(self.attention_buffer)[-n_samples:]
        values = [s['value'] for s in samples]
        return sum(values) / len(values)
        
    def get_connection_status(self) -> Dict:
        """
        获取连接状态信息
        
        Returns:
            Dict: 连接状态信息
        """
        return {
            'is_connected': self.is_connected,
            'is_running': self.is_running,
            'host': self.host,
            'port': self.port,
            'total_packets': self.total_packets,
            'receive_rate': self.receive_rate,
            'buffer_size': len(self.attention_buffer)
        }
        
    def clear_buffers(self):
        """清空所有数据缓冲区"""
        self.attention_buffer.clear()
        self.eeg_buffer.clear()
        self.imu_buffer.clear()
        self.raw_data_buffer.clear()
