# 基于可穿戴多模态脑机接口与VR融合的认知障碍儿童专注力游戏化训练系统

## 项目概述

本项目旨在设计一套基于单通道脑电信号（EEG）与头动信号（IMU）的多模态脑机接口系统，并结合虚拟现实（VR）技术，构建面向自闭症（ASD）及注意缺陷多动障碍（ADHD）儿童的专注力训练平台。

### 核心特性

- **多模态融合控制**：脑控状态 + 头控行为的协同控制
- **实时神经反馈**：专注力量化指标驱动VR游戏参数
- **沉浸式训练环境**：基于Unity XR Toolkit的VR场景
- **低成本易部署**：单通道EEG设备，适合家庭和机构使用

## 目录结构

```
bmecomp2026/
├── README.md                    # 项目说明文档
├── requirements.txt             # Python依赖包
├── .gitignore                   # Git忽略文件
├── 设计方案.pdf                 # 原始设计方案
│
├── docs/                        # 文档目录
│   ├── 项目计划.md              # 详细项目计划
│   ├── 技术文档.md              # 技术实现文档
│   ├── 文献调研.md              # 相关文献调研
│   ├── 硬件调研.md              # 硬件设备调研
│   └── 开源资源.md              # 开源VR游戏资源
│
├── src/                         # 源代码目录
│   ├── python/                  # Python数据处理模块
│   │   ├── __init__.py          # 包初始化
│   │   ├── main.py              # 主程序入口
│   │   ├── bci/                 # BCI核心模块
│   │   │   ├── __init__.py      # BCI包初始化
│   │   │   ├── data_collector.py    # 数据采集器
│   │   │   ├── signal_processor.py  # 信号处理器
│   │   │   ├── attention_calculator.py # 专注力计算器
│   │   │   ├── socket_server.py     # Socket服务器
│   │   │   └── protocol.py          # 通信协议
│   │   ├── utils/               # 工具模块
│   │   └── tests/               # 测试代码
│   │       └── test_bci.py      # BCI模块测试
│   │
│   └── unity/                   # Unity VR游戏项目
│       └── Assets/
│           ├── Scripts/         # C#脚本
│           │   ├── Core/        # 核心管理器
│           │   │   ├── BCIManager.cs      # BCI管理器
│           │   │   └── GameManager.cs     # 游戏管理器
│           │   ├── Player/      # 玩家控制
│           │   │   └── PlayerController.cs # 玩家控制器
│           │   ├── Gameplay/    # 游戏逻辑
│           │   │   ├── SpeedMapper.cs     # 速度映射
│           │   │   └── RewardSystem.cs    # 奖励系统
│           │   ├── UI/          # 用户界面
│           │   │   └── UIManager.cs       # UI管理器
│           │   ├── Environment/ # 环境
│           │   │   └── StarField.cs       # 星空背景
│           │   ├── Debug/       # 调试工具
│           │   │   └── DebugUI.cs         # 调试UI
│           │   └── Network/     # 网络通信
│           ├── Scenes/          # 场景文件
│           ├── Prefabs/         # 预制体
│           ├── Materials/       # 材质
│           ├── Textures/        # 纹理
│           └── Audio/           # 音效
│
├── assets/                      # 资源文件
│   ├── models/                  # 3D模型
│   ├── textures/                # 纹理贴图
│   └── sounds/                  # 音效文件
│
└── tools/                       # 工具脚本
    ├── setup.sh                 # 环境配置脚本
    └── build.sh                 # 构建脚本
```

## 快速开始

### 环境要求

- **Unity**: 2021.3 LTS 或更高版本
- **Python**: 3.8+
- **VR设备**: Meta Quest 2/3
- **脑电设备**: HNNK单通道脑电头环

### 安装步骤

#### 1. 克隆仓库

```bash
git clone https://github.com/tzhazuma/bmecomp2026.git
cd bmecomp2026
```

#### 2. 配置Python环境

**Linux/Mac:**
```bash
# 运行环境配置脚本
./tools/setup.sh

# 或手动配置
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

**Windows:**
```bash
# 手动配置
python -m venv venv
venv\Scripts\activate
pip install -r requirements.txt
```

#### 3. 启动BCI服务器

```bash
# 激活虚拟环境后
python src/python/main.py

# 或指定参数
python src/python/main.py --host 0.0.0.0 --port 5555

# 运行测试模式
python src/python/main.py --test
```

#### 4. 打开Unity项目

1. 打开 Unity Hub
2. 点击 "Open" 并选择 `src/unity/` 目录
3. 安装必要的包：
   - XR Interaction Toolkit
   - Meta XR SDK (如果使用Quest)
4. 配置XR设置：
   - Edit > Project Settings > XR Plug-in Management
   - 启用 Oculus 或 OpenXR

#### 5. 运行游戏

1. 在Unity中打开 `Assets/Scenes/MainMenu.unity`
2. 点击 Play 按钮
3. 确保BCI服务器正在运行
4. 戴上VR头显开始游戏

## 技术架构

```
┌─────────────────────────────────────────────────────────────┐
│                    硬件层 (Hardware Layer)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │  EEG头环     │  │  VR头显      │  │  计算机      │       │
│  │  (HNNK)      │  │  (Quest 3)   │  │  (PC)        │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
└─────────┼──────────────────┼──────────────────┼─────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    平台层 (Platform Layer)                    │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ HybridBCI    │  │ Unity XR     │  │ Python       │       │
│  │ SDK/API      │  │ Toolkit      │  │ Socket Server│       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
└─────────┼──────────────────┼──────────────────┼─────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    处理层 (Processing Layer)                  │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ 专注力解算   │  │ 头动信号解析 │  │ 信号滤波     │       │
│  │ (EEG→专注力) │  │ (IMU→方向)   │  │ (滑动窗口)   │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
└─────────┼──────────────────┼──────────────────┼─────────────┘
          │                  │                  │
          ▼                  ▼                  ▼
┌─────────────────────────────────────────────────────────────┐
│                    应用层 (Application Layer)                 │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │ VR游戏逻辑   │  │ 神经反馈     │  │ 数据记录     │       │
│  │ (Unity C#)   │  │ (视觉/奖励)  │  │ (CSV/JSON)   │       │
│  └──────────────┘  └──────────────┘  └──────────────┘       │
└─────────────────────────────────────────────────────────────┘
```

## 游戏设计

### 星空守护者

玩家驾驶VR飞船穿越星空，通过专注力控制飞行状态，头动控制飞行方向，躲避障碍、收集星光。

#### 核心机制

1. **状态控制**：专注力 A(t) → 控制游戏速度与稳定性
   ```
   v = vmin + (vmax - vmin) * A(t) / 100
   ```

2. **空间控制**：
   - Yaw（偏航角）→ 左右移动
   - Pitch（俯仰角）→ 上下移动

3. **奖励机制**：
   - 连续专注 > 10秒 → 触发"专注连击模式"
   - 收集星光 → 积分累计
   - 关卡完成 → 动画鼓励

#### 目标人群

6-12岁ADHD/ASD等认知障碍儿童

## Python模块说明

### BCI数据采集器 (`bci/data_collector.py`)

负责从脑电头环采集EEG和IMU数据。

```python
from bci.data_collector import BCIDataCollector

collector = BCIDataCollector(host='localhost', port=5555)
collector.on_attention_updated = lambda att: print(f"专注力: {att}")
collector.start()
```

### 信号处理器 (`bci/signal_processor.py`)

对采集的原始信号进行滤波和处理。

```python
from bci.signal_processor import SignalProcessor

processor = SignalProcessor()
filtered_attention = processor.process_attention(raw_attention)
processed_yaw, processed_pitch = processor.process_imu(yaw, pitch)
```

### 专注力计算器 (`bci/attention_calculator.py`)

从EEG信号计算专注力指标。

```python
from bci.attention_calculator import AttentionCalculator

calculator = AttentionCalculator()
calculator.add_eeg_sample(ch1, ch2, ch3)
result = calculator.calculate_attention()
```

### Socket服务器 (`bci/socket_server.py`)

负责与Unity客户端的通信。

```python
from bci.socket_server import SocketServer, ServerConfig

server = SocketServer(ServerConfig(host='0.0.0.0', port=5555))
server.on_client_connected = lambda addr: print(f"客户端连接: {addr}")
server.start()
```

## Unity脚本说明

### BCIManager (`Scripts/Core/BCIManager.cs`)

BCI数据管理器，负责与Python服务器通信。

```csharp
// 获取BCI管理器实例
BCIManager bci = BCIManager.Instance;

// 获取专注力值
float attention = bci.GetAttention();

// 获取IMU数据
float yaw = bci.GetYaw();
float pitch = bci.GetPitch();

// 注册事件
bci.OnAttentionUpdated += (attention) => {
    Debug.Log($"专注力: {attention}");
};
```

### GameManager (`Scripts/Core/GameManager.cs`)

游戏状态管理器。

```csharp
// 获取游戏管理器实例
GameManager game = GameManager.Instance;

// 开始游戏
game.StartGame();

// 暂停游戏
game.PauseGame();

// 获取分数
int score = game.GetScore();
```

### PlayerController (`Scripts/Player/PlayerController.cs`)

玩家飞船控制器。

```csharp
// 获取玩家控制器
PlayerController player = FindObjectOfType<PlayerController>();

// 获取当前速度
float speed = player.GetCurrentSpeed();

// 获取速度比例
float ratio = player.GetSpeedRatio();
```

## 硬件需求

| 设备 | 型号 | 价格范围 | 用途 |
|------|------|----------|------|
| 脑电头环 | HNNK单通道 | ¥500-2000 | EEG信号采集 |
| VR头显 | Meta Quest 3 | ¥2800-4600 | 沉浸式显示 |
| 计算机 | i5-10代+RTX2060 | ¥5000-8000 | 运行Unity |

### 推荐配置

**经济型方案**: ~¥8,500
- HNNK基础版 + Quest 3S 128G + 性价比PC

**标准型方案**: ~¥13,400
- HNNK专业版 + Quest 3 128G + 高性能PC

**专业型方案**: ~¥28,800
- HNNK专业版 x2 + Quest 3 512G x2 + 高性能PC x2

## 测试

### 运行Python测试

```bash
# 激活虚拟环境
source venv/bin/activate

# 运行所有测试
python -m pytest src/python/tests/ -v

# 运行特定测试
python -m pytest src/python/tests/test_bci.py::TestBCIDataCollector -v
```

### 运行代码检查

```bash
# 代码风格检查
python -m flake8 src/python/bci/

# 类型检查
python -m mypy src/python/bci/

# 格式化检查
python -m black --check src/python/bci/
```

## 文档

- [项目计划](docs/项目计划.md) - 详细的项目计划和时间线
- [技术文档](docs/技术文档.md) - 技术实现细节
- [文献调研](docs/文献调研.md) - 相关学术文献
- [硬件调研](docs/硬件调研.md) - 硬件设备选型
- [开源资源](docs/开源资源.md) - 开源VR游戏资源

## 团队成员

- 毛亚轩 - 项目负责人
- 叶铮皓 - BCI开发
- 夏陆贤 - Unity开发
- 唐志昊 - 数据处理

指导老师：熊泽

## 单位

上海科技大学 · 脑机接口赛道团队

## 致谢

- 琶洲实验室
- 华南脑控

## 相关文献

1. Lim CG, et al. (2023). Home-based brain–computer interface attention training program for ADHD. *Child and Adolescent Mental Health*.

2. Raza MZ, et al. (2025). BCI-based attention training game system for children with ADHD. *Neuroregulation*.

3. Teo SHJ, et al. (2021). BCI based attention and social cognition training for children with ASD and ADHD. *Research in Autism Spectrum Disorders*.

4. 陆凯, 等. (2023). 神经反馈训练中的虚拟现实技术综述. *计算机辅助设计与图形学学报*.

5. 万象隆, 等. (2025). 基于BCI与VR的认知诊疗应用. *工程科学学报*.

## 许可证

本项目仅供学术研究使用。

## 联系方式

如有问题，请通过GitHub Issues联系我们。
