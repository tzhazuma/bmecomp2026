"""
专注力计算器
负责从EEG信号计算专注力指标
"""

import numpy as np
from typing import List, Optional, Dict
from dataclasses import dataclass
from collections import deque
import time


@dataclass
class AttentionResult:
    """专注力计算结果"""
    timestamp: float
    raw_value: float           # 原始值
    filtered_value: float      # 滤波后值
    normalized_value: float    # 归一化值 (0-100)
    quality: float             # 信号质量
    alpha_power: float         # Alpha波功率
    beta_power: float          # Beta波功率
    beta_ratio: float          # Beta波比例


class AttentionCalculator:
    """
    专注力计算器
    基于EEG信号计算专注力指标
    """
    
    def __init__(self, 
                 window_size: int = 256,
                 update_rate: float = 50.0):
        """
        初始化专注力计算器
        
        Args:
            window_size: 分析窗口大小（采样点数）
            update_rate: 更新频率 (Hz)
        """
        self.window_size = window_size
        self.update_rate = update_rate
        
        # 数据缓冲区
        self.eeg_buffer: deque = deque(maxlen=window_size * 2)
        self.attention_history: deque = deque(maxlen=1000)
        
        # 校准数据
        self.baseline_alpha: float = 0.0
        self.baseline_beta: float = 0.0
        self.is_calibrated: bool = False
        self.calibration_data: List[Dict] = []
        
        # 阈值设置
        self.min_attention: float = 0.0
        self.max_attention: float = 100.0
        self.focus_threshold: float = 70.0
        
    def add_eeg_sample(self, ch1: float, ch2: float, ch3: float, timestamp: float = None):
        """
        添加EEG采样数据
        
        Args:
            ch1: 通道1数据
            ch2: 通道2数据
            ch3: 通道3数据
            timestamp: 时间戳
        """
        if timestamp is None:
            timestamp = time.time()
            
        sample = {
            'timestamp': timestamp,
            'ch1': ch1,
            'ch2': ch2,
            'ch3': ch3
        }
        self.eeg_buffer.append(sample)
        
    def calculate_attention(self) -> Optional[AttentionResult]:
        """
        计算专注力指标
        
        Returns:
            Optional[AttentionResult]: 专注力计算结果，如果数据不足则返回None
        """
        if len(self.eeg_buffer) < self.window_size:
            return None
            
        # 获取窗口数据
        window_data = list(self.eeg_buffer)[-self.window_size:]
        ch1_data = [d['ch1'] for d in window_data]
        timestamp = window_data[-1]['timestamp']
        
        # 计算功率谱密度
        alpha_power = self._calculate_band_power(ch1_data, 8, 13)
        beta_power = self._calculate_band_power(ch1_data, 13, 30)
        
        # 计算Beta波比例
        total_power = alpha_power + beta_power
        if total_power > 0:
            beta_ratio = beta_power / total_power
        else:
            beta_ratio = 0.5
            
        # 计算原始专注力值
        raw_attention = beta_ratio * 100
        
        # 如果已校准，使用校准数据归一化
        if self.is_calibrated:
            normalized_attention = self._normalize_with_calibration(raw_attention)
        else:
            normalized_attention = np.clip(raw_attention, 0, 100)
            
        # 计算信号质量
        quality = self._calculate_signal_quality(ch1_data)
        
        # 创建结果
        result = AttentionResult(
            timestamp=timestamp,
            raw_value=raw_attention,
            filtered_value=normalized_attention,
            normalized_value=normalized_attention,
            quality=quality,
            alpha_power=alpha_power,
            beta_power=beta_power,
            beta_ratio=beta_ratio
        )
        
        # 保存历史
        self.attention_history.append(result)
        
        return result
        
    def _calculate_band_power(self, signal: List[float], low_freq: float, high_freq: float) -> float:
        """
        计算频带功率
        
        Args:
            signal: 信号数据
            low_freq: 低频边界
            high_freq: 高频边界
            
        Returns:
            float: 频带功率
        """
        # 简化实现：使用信号方差作为功率估计
        # 实际应用中应该使用FFT分析
        if not signal:
            return 0.0
            
        signal_array = np.array(signal)
        
        # 去除直流分量
        signal_array = signal_array - np.mean(signal_array)
        
        # 计算方差作为功率估计
        power = np.var(signal_array)
        
        # 根据频带范围调整权重
        # Beta波（13-30Hz）通常与专注力相关
        if low_freq >= 13:
            return power * 1.5
        else:
            return power * 0.8
            
    def _normalize_with_calibration(self, raw_value: float) -> float:
        """
        使用校准数据归一化专注力值
        
        Args:
            raw_value: 原始专注力值
            
        Returns:
            float: 归一化后的专注力值 (0-100)
        """
        if self.baseline_beta == 0:
            return np.clip(raw_value, 0, 100)
            
        # 相对于基线的百分比
        relative = (raw_value - self.baseline_beta) / self.baseline_beta * 100
        
        # 限制范围
        return np.clip(relative, 0, 100)
        
    def _calculate_signal_quality(self, signal: List[float]) -> float:
        """
        计算信号质量
        
        Args:
            signal: 信号数据
            
        Returns:
            float: 信号质量 (0-1)
        """
        if not signal:
            return 0.0
            
        signal_array = np.array(signal)
        
        # 检查信号幅度
        amplitude = np.max(np.abs(signal_array))
        if amplitude > 1000:  # 信号过大
            return 0.1
        if amplitude < 1:  # 信号过小
            return 0.1
            
        # 检查信号方差
        variance = np.var(signal_array)
        if variance < 0.1:  # 信号太平坦
            return 0.3
            
        # 计算信噪比（简化）
        mean_val = np.mean(signal_array)
        snr = abs(mean_val) / (np.std(signal_array) + 1e-6)
        
        # 归一化到0-1
        quality = min(1.0, snr / 10.0)
        
        return quality
        
    def calibrate(self, duration: float = 10.0, sample_rate: float = 256.0):
        """
        进行校准
        
        Args:
            duration: 校准持续时间（秒）
            sample_rate: 采样率
        """
        print(f"开始校准，持续{duration}秒...")
        
        self.calibration_data = []
        start_time = time.time()
        
        # 收集校准数据
        while time.time() - start_time < duration:
            if len(self.eeg_buffer) > 0:
                sample = self.eeg_buffer[-1]
                self.calibration_data.append(sample)
            time.sleep(1.0 / sample_rate)
            
        # 计算基线
        if self.calibration_data:
            ch1_data = [d['ch1'] for d in self.calibration_data]
            self.baseline_alpha = self._calculate_band_power(ch1_data, 8, 13)
            self.baseline_beta = self._calculate_band_power(ch1_data, 13, 30)
            self.is_calibrated = True
            
            print(f"校准完成:")
            print(f"  Alpha基线: {self.baseline_alpha:.2f}")
            print(f"  Beta基线: {self.baseline_beta:.2f}")
        else:
            print("校准失败: 没有收集到数据")
            
    def get_attention_history(self, n_samples: Optional[int] = None) -> List[AttentionResult]:
        """
        获取专注力历史
        
        Args:
            n_samples: 获取的样本数量
            
        Returns:
            List[AttentionResult]: 专注力历史
        """
        if n_samples is None:
            return list(self.attention_history)
        return list(self.attention_history)[-n_samples:]
        
    def get_average_attention(self, n_samples: int = 100) -> float:
        """
        获取平均专注力
        
        Args:
            n_samples: 计算平均值的样本数量
            
        Returns:
            float: 平均专注力值
        """
        if not self.attention_history:
            return 0.0
            
        history = list(self.attention_history)[-n_samples:]
        values = [r.normalized_value for r in history]
        return np.mean(values)
        
    def get_focus_streak(self) -> float:
        """
        获取当前连续专注时间（秒）
        
        Returns:
            float: 连续专注时间
        """
        if not self.attention_history:
            return 0.0
            
        streak = 0.0
        for result in reversed(list(self.attention_history)):
            if result.normalized_value >= self.focus_threshold:
                streak += 1.0 / self.update_rate
            else:
                break
                
        return streak
        
    def is_focused(self) -> bool:
        """
        判断是否处于专注状态
        
        Returns:
            bool: 是否专注
        """
        if not self.attention_history:
            return False
            
        latest = self.attention_history[-1]
        return latest.normalized_value >= self.focus_threshold
        
    def reset(self):
        """重置计算器"""
        self.eeg_buffer.clear()
        self.attention_history.clear()
        self.is_calibrated = False
        self.calibration_data.clear()
        
    def get_state(self) -> dict:
        """获取计算器状态"""
        return {
            'is_calibrated': self.is_calibrated,
            'baseline_alpha': self.baseline_alpha,
            'baseline_beta': self.baseline_beta,
            'buffer_size': len(self.eeg_buffer),
            'history_size': len(self.attention_history),
            'average_attention': self.get_average_attention(),
            'focus_streak': self.get_focus_streak()
        }
