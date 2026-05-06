"""
信号处理器
负责对BCI采集的原始信号进行滤波和处理
"""

import numpy as np
from collections import deque
from typing import List, Optional, Tuple
from dataclasses import dataclass


@dataclass
class FilterConfig:
    """滤波器配置"""
    window_size: int = 10
    alpha: float = 0.7
    deadzone: float = 5.0
    min_value: float = 0.0
    max_value: float = 100.0


class MovingAverageFilter:
    """
    滑动平均滤波器
    用于平滑信号，减少噪声
    """
    
    def __init__(self, window_size: int = 10):
        """
        初始化滑动平均滤波器
        
        Args:
            window_size: 窗口大小
        """
        self.window_size = window_size
        self.buffer: deque = deque(maxlen=window_size)
        
    def process(self, value: float) -> float:
        """
        处理新数据
        
        Args:
            value: 输入值
            
        Returns:
            float: 滤波后的值
        """
        self.buffer.append(value)
        return np.mean(list(self.buffer))
        
    def reset(self):
        """重置滤波器"""
        self.buffer.clear()
        
    def get_state(self) -> dict:
        """获取滤波器状态"""
        return {
            'window_size': self.window_size,
            'buffer_size': len(self.buffer),
            'current_value': np.mean(list(self.buffer)) if self.buffer else 0.0
        }


class ExponentialSmoothingFilter:
    """
    指数平滑滤波器
    用于信号平滑，对最新数据给予更高权重
    """
    
    def __init__(self, alpha: float = 0.7):
        """
        初始化指数平滑滤波器
        
        Args:
            alpha: 平滑系数 (0-1)，越大越重视最新数据
        """
        self.alpha = alpha
        self.last_value: Optional[float] = None
        
    def process(self, value: float) -> float:
        """
        处理新数据
        
        Args:
            value: 输入值
            
        Returns:
            float: 平滑后的值
        """
        if self.last_value is None:
            self.last_value = value
        else:
            self.last_value = self.alpha * value + (1 - self.alpha) * self.last_value
        return self.last_value
        
    def reset(self):
        """重置滤波器"""
        self.last_value = None
        
    def get_state(self) -> dict:
        """获取滤波器状态"""
        return {
            'alpha': self.alpha,
            'last_value': self.last_value,
            'is_initialized': self.last_value is not None
        }


class KalmanFilter:
    """
    卡尔曼滤波器
    用于最优估计，适合动态系统
    """
    
    def __init__(self, process_noise: float = 0.01, measurement_noise: float = 0.1):
        """
        初始化卡尔曼滤波器
        
        Args:
            process_noise: 过程噪声协方差
            measurement_noise: 测量噪声协方差
        """
        self.process_noise = process_noise
        self.measurement_noise = measurement_noise
        
        # 状态估计
        self.x = 0.0  # 状态
        self.P = 1.0  # 估计误差协方差
        
        # 状态转移矩阵
        self.F = 1.0
        
        # 观测矩阵
        self.H = 1.0
        
    def process(self, measurement: float) -> float:
        """
        处理新测量值
        
        Args:
            measurement: 测量值
            
        Returns:
            float: 估计值
        """
        # 预测
        x_pred = self.F * self.x
        P_pred = self.F * self.P * self.F + self.process_noise
        
        # 更新
        K = P_pred * self.H / (self.H * P_pred * self.H + self.measurement_noise)
        self.x = x_pred + K * (measurement - self.H * x_pred)
        self.P = (1 - K * self.H) * P_pred
        
        return self.x
        
    def reset(self):
        """重置滤波器"""
        self.x = 0.0
        self.P = 1.0
        
    def get_state(self) -> dict:
        """获取滤波器状态"""
        return {
            'state': self.x,
            'covariance': self.P,
            'process_noise': self.process_noise,
            'measurement_noise': self.measurement_noise
        }


class DeadZoneFilter:
    """
    死区滤波器
    用于消除微小抖动
    """
    
    def __init__(self, threshold: float = 5.0):
        """
        初始化死区滤波器
        
        Args:
            threshold: 死区阈值
        """
        self.threshold = threshold
        
    def process(self, value: float) -> float:
        """
        处理新数据
        
        Args:
            value: 输入值
            
        Returns:
            float: 处理后的值
        """
        if abs(value) < self.threshold:
            return 0.0
        return value
        
    def process_with_sign(self, value: float) -> float:
        """
        处理新数据，保留符号
        
        Args:
            value: 输入值
            
        Returns:
            float: 处理后的值
        """
        if abs(value) < self.threshold:
            return 0.0
        return value - self.threshold * np.sign(value)


class SignalProcessor:
    """
    信号处理器
    组合多种滤波器，对BCI信号进行综合处理
    """
    
    def __init__(self, config: Optional[FilterConfig] = None,
                 window_size: int = 10, alpha: float = 0.7,
                 deadzone: float = 5.0):
        if config is None:
            config = FilterConfig(window_size=window_size, alpha=alpha, deadzone=deadzone)
        self.config = config
        
        # 初始化滤波器
        self.attention_ma = MovingAverageFilter(config.window_size)
        self.attention_es = ExponentialSmoothingFilter(config.alpha)
        self.attention_kf = KalmanFilter()
        
        self.imu_deadzone = DeadZoneFilter(config.deadzone)
        self.imu_ma_yaw = MovingAverageFilter(config.window_size)
        self.imu_ma_pitch = MovingAverageFilter(config.window_size)
        
    def process_attention(self, raw_attention: float) -> float:
        """
        处理专注力信号
        
        Args:
            raw_attention: 原始专注力值
            
        Returns:
            float: 处理后的专注力值 (0-100)
        """
        # 限制范围
        raw_attention = np.clip(raw_attention, self.config.min_value, self.config.max_value)
        
        # 滑动平均滤波
        ma_value = self.attention_ma.process(raw_attention)
        
        # 指数平滑
        es_value = self.attention_es.process(ma_value)
        
        # 卡尔曼滤波
        kf_value = self.attention_kf.process(es_value)
        
        # 再次限制范围
        return np.clip(kf_value, self.config.min_value, self.config.max_value)
        
    def process_imu(self, yaw: float, pitch: float) -> Tuple[float, float]:
        """
        处理IMU信号
        
        Args:
            yaw: 偏航角
            pitch: 俯仰角
            
        Returns:
            Tuple[float, float]: 处理后的偏航角和俯仰角
        """
        # 死区处理
        yaw = self.imu_deadzone.process(yaw)
        pitch = self.imu_deadzone.process(pitch)
        
        # 滑动平均滤波
        yaw = self.imu_ma_yaw.process(yaw)
        pitch = self.imu_ma_pitch.process(pitch)
        
        return yaw, pitch
        
    def process_eeg(self, ch1: float, ch2: float, ch3: float) -> dict:
        """
        处理EEG信号
        
        Args:
            ch1: 通道1
            ch2: 通道2
            ch3: 通道3
            
        Returns:
            dict: 处理后的EEG数据
        """
        # 计算功率谱密度
        # 这里简化处理，实际应用中需要更复杂的算法
        return {
            'ch1': ch1,
            'ch2': ch2,
            'ch3': ch3,
            'alpha_power': self._calculate_alpha_power(ch1),
            'beta_power': self._calculate_beta_power(ch1)
        }
        
    def _calculate_alpha_power(self, signal: float) -> float:
        """计算Alpha波功率 (8-13Hz)"""
        # 简化实现，实际应用中需要FFT分析
        return abs(signal) * 0.4
        
    def _calculate_beta_power(self, signal: float) -> float:
        """计算Beta波功率 (13-30Hz)"""
        # 简化实现，实际应用中需要FFT分析
        return abs(signal) * 0.6
        
    def reset(self):
        """重置所有滤波器"""
        self.attention_ma.reset()
        self.attention_es.reset()
        self.attention_kf.reset()
        self.imu_ma_yaw.reset()
        self.imu_ma_pitch.reset()
        
    def get_state(self) -> dict:
        """获取处理器状态"""
        return {
            'attention_ma': self.attention_ma.get_state(),
            'attention_es': self.attention_es.get_state(),
            'attention_kf': self.attention_kf.get_state(),
            'imu_ma_yaw': self.imu_ma_yaw.get_state(),
            'imu_ma_pitch': self.imu_ma_pitch.get_state()
        }
