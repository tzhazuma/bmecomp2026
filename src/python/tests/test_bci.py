"""
BCI数据处理模块测试
"""

import unittest
import time
import threading
import json
import socket
from unittest.mock import MagicMock, patch

import sys
sys.path.insert(0, '..')

from bci.data_collector import BCIDataCollector, BCIData, EEGData, IMUData
from bci.signal_processor import SignalProcessor, FilterConfig
from bci.attention_calculator import AttentionCalculator
from bci.protocol import BCIProtocol, MessageType


class TestBCIDataCollector(unittest.TestCase):
    """测试BCI数据采集器"""
    
    def setUp(self):
        """测试前准备"""
        self.collector = BCIDataCollector(
            host='localhost',
            port=5555,
            buffer_size=100
        )
        
    def test_init(self):
        """测试初始化"""
        self.assertEqual(self.collector.host, 'localhost')
        self.assertEqual(self.collector.port, 5555)
        self.assertEqual(self.collector.buffer_size, 100)
        self.assertFalse(self.collector.is_running)
        self.assertFalse(self.collector.is_connected)
        
    def test_buffers(self):
        """测试缓冲区"""
        # 添加测试数据
        self.collector.attention_buffer.append({'timestamp': time.time(), 'value': 50.0})
        self.assertEqual(len(self.collector.attention_buffer), 1)
        
        # 获取数据
        attention = self.collector.get_attention()
        self.assertEqual(attention, 50.0)
        
        # 清空缓冲区
        self.collector.clear_buffers()
        self.assertEqual(len(self.collector.attention_buffer), 0)
        
    def test_callback(self):
        """测试回调函数"""
        callback_called = False
        callback_data = None
        
        def on_data_received(data):
            nonlocal callback_called, callback_data
            callback_called = True
            callback_data = data
            
        self.collector.on_data_received = on_data_received
        
        # 模拟数据接收
        test_data = {
            'timestamp': time.time(),
            'attention': 75.0,
            'imu': {'yaw': 10.0, 'pitch': 5.0, 'roll': 0.0}
        }
        self.collector._process_data(test_data)
        
        self.assertTrue(callback_called)
        self.assertIsNotNone(callback_data)
        self.assertEqual(callback_data.attention, 75.0)


class TestSignalProcessor(unittest.TestCase):
    """测试信号处理器"""
    
    def setUp(self):
        """测试前准备"""
        self.config = FilterConfig(
            window_size=5,
            alpha=0.7,
            deadzone=5.0
        )
        self.processor = SignalProcessor(self.config)
        
    def test_moving_average(self):
        """测试滑动平均滤波器"""
        values = [10, 20, 30, 40, 50]
        results = []
        
        for value in values:
            result = self.processor.attention_ma.process(value)
            results.append(result)
            
        # 验证平均值
        self.assertAlmostEqual(results[-1], 30.0)
        
    def test_exponential_smoothing(self):
        """测试指数平滑滤波器"""
        values = [10, 20, 30, 40, 50]
        results = []
        
        for value in values:
            result = self.processor.attention_es.process(value)
            results.append(result)
            
        # 验证平滑效果
        self.assertTrue(all(0 <= r <= 100 for r in results))
        
    def test_deadzone(self):
        """测试死区滤波器"""
        # 测试死区内的值
        result = self.processor.imu_deadzone.process(3.0)
        self.assertEqual(result, 0.0)
        
        # 测试死区外的值
        result = self.processor.imu_deadzone.process(10.0)
        self.assertEqual(result, 10.0)
        
    def test_process_attention(self):
        """测试专注力信号处理"""
        values = [50, 60, 70, 80, 90]
        results = []
        
        for value in values:
            result = self.processor.process_attention(value)
            results.append(result)
            
        # 验证处理结果
        self.assertTrue(all(0 <= r <= 100 for r in results))
        
    def test_process_imu(self):
        """测试IMU信号处理"""
        yaw, pitch = self.processor.process_imu(10.0, 5.0)
        
        # 验证处理结果
        self.assertIsInstance(yaw, float)
        self.assertIsInstance(pitch, float)


class TestAttentionCalculator(unittest.TestCase):
    """测试专注力计算器"""
    
    def setUp(self):
        """测试前准备"""
        self.calculator = AttentionCalculator(
            window_size=100,
            update_rate=50.0
        )
        
    def test_add_eeg_sample(self):
        """测试添加EEG采样"""
        for i in range(150):
            self.calculator.add_eeg_sample(
                ch1=float(i),
                ch2=float(i) * 0.5,
                ch3=float(i) * 0.3
            )
            
        self.assertEqual(len(self.calculator.eeg_buffer), 150)
        
    def test_calculate_attention(self):
        """测试专注力计算"""
        # 添加足够的数据
        for i in range(150):
            self.calculator.add_eeg_sample(
                ch1=float(i % 100),
                ch2=float(i % 100) * 0.5,
                ch3=float(i % 100) * 0.3
            )
            
        result = self.calculator.calculate_attention()
        
        self.assertIsNotNone(result)
        self.assertTrue(0 <= result.normalized_value <= 100)
        
    def test_calibration(self):
        """测试校准"""
        # 添加校准数据
        for i in range(100):
            self.calculator.eeg_buffer.append({
                'timestamp': time.time(),
                'ch1': 50.0,
                'ch2': 25.0,
                'ch3': 15.0
            })
            
        self.calculator.calibrate(duration=0.1)
        
        self.assertTrue(self.calculator.is_calibrated)
        
    def test_focus_streak(self):
        """测试连续专注时间"""
        # 添加高专注力数据
        for i in range(100):
            self.calculator.attention_history.append(
                MagicMock(normalized_value=80.0)
            )
            
        streak = self.calculator.get_focus_streak()
        self.assertGreater(streak, 0)


class TestBCIProtocol(unittest.TestCase):
    """测试BCI通信协议"""
    
    def test_create_attention_message(self):
        """测试创建专注力消息"""
        message = BCIProtocol.create_attention_message(
            attention=75.0,
            quality=0.9,
            alpha_power=0.4,
            beta_power=0.6
        )
        
        self.assertIsInstance(message, str)
        
        # 解析消息
        parsed = BCIProtocol.parse_message(message)
        self.assertIsNotNone(parsed)
        self.assertEqual(parsed['type'], 'attention')
        
    def test_create_imu_message(self):
        """测试创建IMU消息"""
        message = BCIProtocol.create_imu_message(
            yaw=10.0,
            pitch=5.0,
            roll=0.0
        )
        
        self.assertIsInstance(message, str)
        
        # 解析消息
        parsed = BCIProtocol.parse_message(message)
        self.assertIsNotNone(parsed)
        self.assertEqual(parsed['type'], 'imu')
        
    def test_create_command_message(self):
        """测试创建命令消息"""
        message = BCIProtocol.create_command_message(
            command='start_calibration',
            parameters={'duration': 10}
        )
        
        self.assertIsInstance(message, str)
        
        # 解析消息
        parsed = BCIProtocol.parse_message(message)
        self.assertIsNotNone(parsed)
        self.assertEqual(parsed['type'], 'command')
        
    def test_validate_message(self):
        """测试消息验证"""
        # 有效消息
        valid_message = {
            'type': 'attention',
            'timestamp': time.time(),
            'data': {'attention': 75.0}
        }
        self.assertTrue(BCIProtocol.validate_message(valid_message))
        
        # 无效消息（缺少字段）
        invalid_message = {
            'type': 'attention'
        }
        self.assertFalse(BCIProtocol.validate_message(invalid_message))
        
    def test_get_message_type(self):
        """测试获取消息类型"""
        message = {
            'type': 'attention',
            'timestamp': time.time(),
            'data': {}
        }
        
        msg_type = BCIProtocol.get_message_type(message)
        self.assertEqual(msg_type, MessageType.ATTENTION)


class TestIntegration(unittest.TestCase):
    """集成测试"""
    
    def test_data_flow(self):
        """测试数据流"""
        # 创建处理器
        processor = SignalProcessor()
        
        # 模拟数据流
        test_values = [50, 55, 60, 65, 70, 75, 80]
        results = []
        
        for value in test_values:
            processed = processor.process_attention(value)
            results.append(processed)
            
        # 验证数据流
        self.assertEqual(len(results), len(test_values))
        self.assertTrue(all(isinstance(r, float) for r in results))
        
    def test_protocol_flow(self):
        """测试协议流"""
        # 创建消息
        attention_msg = BCIProtocol.create_attention_message(75.0)
        imu_msg = BCIProtocol.create_imu_message(10.0, 5.0, 0.0)
        
        # 解析消息
        attention_parsed = BCIProtocol.parse_message(attention_msg)
        imu_parsed = BCIProtocol.parse_message(imu_msg)
        
        # 验证解析结果
        self.assertIsNotNone(attention_parsed)
        self.assertIsNotNone(imu_parsed)
        self.assertEqual(attention_parsed['type'], 'attention')
        self.assertEqual(imu_parsed['type'], 'imu')


if __name__ == '__main__':
    unittest.main()
