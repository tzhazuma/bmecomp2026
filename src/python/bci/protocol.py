"""
BCI通信协议
定义BCI系统各组件之间的通信格式
"""

import json
import time
from typing import Dict, Any, Optional
from dataclasses import dataclass, asdict
from enum import Enum


class MessageType(Enum):
    """消息类型枚举"""
    ATTENTION = "attention"      # 专注力数据
    EEG = "eeg"                 # EEG数据
    IMU = "imu"                 # IMU数据
    STATUS = "status"           # 状态信息
    COMMAND = "command"         # 控制命令
    RESPONSE = "response"       # 响应
    ERROR = "error"             # 错误
    HEARTBEAT = "heartbeat"     # 心跳
    CALIBRATION = "calibration" # 校准
    GAME_STATE = "game_state"   # 游戏状态


@dataclass
class Message:
    """消息基类"""
    type: str
    timestamp: float
    data: Dict[str, Any]
    source: str = "bci"
    version: str = "1.0"


class BCIProtocol:
    """
    BCI通信协议
    负责消息的创建、解析和验证
    """
    
    PROTOCOL_VERSION = "1.0"
    
    @staticmethod
    def create_message(msg_type: MessageType, data: Dict[str, Any], 
                      source: str = "bci") -> str:
        """
        创建消息
        
        Args:
            msg_type: 消息类型
            data: 消息数据
            source: 消息来源
            
        Returns:
            str: JSON格式的消息字符串
        """
        message = Message(
            type=msg_type.value,
            timestamp=time.time(),
            data=data,
            source=source,
            version=BCIProtocol.PROTOCOL_VERSION
        )
        return json.dumps(asdict(message)) + "\n"
        
    @staticmethod
    def parse_message(message: str) -> Optional[Dict[str, Any]]:
        """
        解析消息
        
        Args:
            message: JSON格式的消息字符串
            
        Returns:
            Optional[Dict[str, Any]]: 解析后的消息，解析失败返回None
        """
        try:
            # 移除可能的换行符
            message = message.strip()
            if not message:
                return None
                
            data = json.loads(message)
            
            # 验证必需字段
            if 'type' not in data or 'timestamp' not in data:
                return None
                
            return data
        except json.JSONDecodeError:
            return None
            
    @staticmethod
    def create_attention_message(attention: float, quality: float = 1.0,
                                alpha_power: float = 0.0, 
                                beta_power: float = 0.0) -> str:
        """
        创建专注力消息
        
        Args:
            attention: 专注力值 (0-100)
            quality: 信号质量 (0-1)
            alpha_power: Alpha波功率
            beta_power: Beta波功率
            
        Returns:
            str: 消息字符串
        """
        data = {
            'attention': attention,
            'quality': quality,
            'alpha_power': alpha_power,
            'beta_power': beta_power
        }
        return BCIProtocol.create_message(MessageType.ATTENTION, data)
        
    @staticmethod
    def create_eeg_message(ch1: float, ch2: float, ch3: float,
                          quality: float = 1.0) -> str:
        """
        创建EEG消息
        
        Args:
            ch1: 通道1
            ch2: 通道2
            ch3: 通道3
            quality: 信号质量
            
        Returns:
            str: 消息字符串
        """
        data = {
            'ch1': ch1,
            'ch2': ch2,
            'ch3': ch3,
            'quality': quality
        }
        return BCIProtocol.create_message(MessageType.EEG, data)
        
    @staticmethod
    def create_imu_message(yaw: float, pitch: float, roll: float,
                          accel_x: float = 0.0, accel_y: float = 0.0,
                          accel_z: float = 0.0) -> str:
        """
        创建IMU消息
        
        Args:
            yaw: 偏航角
            pitch: 俯仰角
            roll: 横滚角
            accel_x: X轴加速度
            accel_y: Y轴加速度
            accel_z: Z轴加速度
            
        Returns:
            str: 消息字符串
        """
        data = {
            'yaw': yaw,
            'pitch': pitch,
            'roll': roll,
            'accel_x': accel_x,
            'accel_y': accel_y,
            'accel_z': accel_z
        }
        return BCIProtocol.create_message(MessageType.IMU, data)
        
    @staticmethod
    def create_status_message(status: str, battery: float = 100.0,
                             signal_quality: float = 1.0,
                             connected: bool = True) -> str:
        """
        创建状态消息
        
        Args:
            status: 状态描述
            battery: 电池电量
            signal_quality: 信号质量
            connected: 是否连接
            
        Returns:
            str: 消息字符串
        """
        data = {
            'status': status,
            'battery': battery,
            'signal_quality': signal_quality,
            'connected': connected
        }
        return BCIProtocol.create_message(MessageType.STATUS, data)
        
    @staticmethod
    def create_command_message(command: str, parameters: Dict[str, Any] = None) -> str:
        """
        创建命令消息
        
        Args:
            command: 命令名称
            parameters: 命令参数
            
        Returns:
            str: 消息字符串
        """
        if parameters is None:
            parameters = {}
            
        data = {
            'command': command,
            'parameters': parameters
        }
        return BCIProtocol.create_message(MessageType.COMMAND, data)
        
    @staticmethod
    def create_response_message(success: bool, data: Dict[str, Any] = None,
                               error: str = None) -> str:
        """
        创建响应消息
        
        Args:
            success: 是否成功
            data: 响应数据
            error: 错误信息
            
        Returns:
            str: 消息字符串
        """
        if data is None:
            data = {}
            
        response_data = {
            'success': success,
            'data': data
        }
        if error:
            response_data['error'] = error
            
        return BCIProtocol.create_message(MessageType.RESPONSE, response_data)
        
    @staticmethod
    def create_heartbeat_message() -> str:
        """
        创建心跳消息
        
        Returns:
            str: 消息字符串
        """
        return BCIProtocol.create_message(MessageType.HEARTBEAT, {})
        
    @staticmethod
    def create_calibration_message(action: str, parameters: Dict[str, Any] = None) -> str:
        """
        创建校准消息
        
        Args:
            action: 校准动作 (start/stop/result)
            parameters: 校准参数
            
        Returns:
            str: 消息字符串
        """
        if parameters is None:
            parameters = {}
            
        data = {
            'action': action,
            'parameters': parameters
        }
        return BCIProtocol.create_message(MessageType.CALIBRATION, data)
        
    @staticmethod
    def create_game_state_message(state: str, score: int = 0,
                                 attention: float = 0.0,
                                 focus_streak: float = 0.0) -> str:
        """
        创建游戏状态消息
        
        Args:
            state: 游戏状态
            score: 分数
            attention: 专注力值
            focus_streak: 连续专注时间
            
        Returns:
            str: 消息字符串
        """
        data = {
            'state': state,
            'score': score,
            'attention': attention,
            'focus_streak': focus_streak
        }
        return BCIProtocol.create_message(MessageType.GAME_STATE, data)
        
    @staticmethod
    def validate_message(message: Dict[str, Any]) -> bool:
        """
        验证消息格式
        
        Args:
            message: 消息字典
            
        Returns:
            bool: 是否有效
        """
        # 检查必需字段
        if 'type' not in message:
            return False
        if 'timestamp' not in message:
            return False
        if 'data' not in message:
            return False
            
        # 检查消息类型
        try:
            MessageType(message['type'])
        except ValueError:
            return False
            
        # 检查时间戳
        if not isinstance(message['timestamp'], (int, float)):
            return False
            
        return True
        
    @staticmethod
    def get_message_type(message: Dict[str, Any]) -> Optional[MessageType]:
        """
        获取消息类型
        
        Args:
            message: 消息字典
            
        Returns:
            Optional[MessageType]: 消息类型
        """
        if 'type' not in message:
            return None
        try:
            return MessageType(message['type'])
        except ValueError:
            return None
