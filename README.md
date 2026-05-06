# 基于可穿戴多模态脑机接口与 VR 融合的认知障碍儿童专注力游戏化训练系统

**脑机接口赛道 · 赛题三**  
**上海科技大学 · 脑机接口赛道团队**

---

## 项目概述

本项目设计一套基于单通道脑电信号（EEG）与头动信号（IMU）的多模态脑机接口系统，结合虚拟现实（VR）技术，构建面向自闭症（ASD）及注意缺陷多动障碍（ADHD）儿童的专注力训练平台。

### 核心特性

- **多模态融合控制**：专注力（EEG）→ 游戏速度/稳定性，头动（IMU）→ 方向控制
- **实时神经反馈闭环**：专注力实时量化 → 驱动 VR 游戏参数 → 即时视觉反馈
- **游戏化训练**：三关渐进式 VR 游戏"星空守护者"，沉浸式训练体验
- **自适应难度**：根据历史表现自动调整难度，个性化训练
- **数据驱动评估**：完整训练数据记录与分析，训练后生成报告
- **低成本易部署**：单通道 EEG 头环 + Meta Quest VR，适合家庭/学校/康复机构

---

## 系统架构

```
┌──────────────────────────────────────────────────────────────┐
│                    硬件层 (Hardware Layer)                     │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐       │
│  │  EEG头环     │  │  VR头显      │  │  计算机      │       │
│  │  (HNNK)      │  │  (Quest 3)   │  │  (PC)        │       │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘       │
└─────────┼──────────────────┼──────────────────┼─────────────┘
          │ 蓝牙             │ Link/AirLink     │
          ▼                  ▼                  ▼
┌──────────────────────────────────────────────────────────────┐
│                    平台层 (Platform Layer)                     │
│  ┌──────────────┐  ┌──────────────┐                        │
│  │ HybridBCI    │  │ Unity XR     │                        │
│  │ 平台APP      │  │ Toolkit      │                        │
│  └──────┬───────┘  └──────────────┘                        │
└─────────┼──────────────────────────────────────────────────┘
          │ QLocalSocket (BigEndian 二进制协议)
          ▼
┌──────────────────────────────────────────────────────────────┐
│                    处理层 (Processing Layer)                   │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              Python 中间件                             │   │
│  │  HybridBCIAdapter → SignalProcessor → SocketServer   │   │
│  │                     ↕ DataRecorder                     │   │
│  └──────────────────────┬───────────────────────────────┘   │
└─────────────────────────┼──────────────────────────────────┘
                          │ TCP Socket (JSON)
                          ▼
┌──────────────────────────────────────────────────────────────┐
│                    应用层 (Application Layer)                  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │              Unity VR 游戏                             │   │
│  │  StarGuardian (星空守护者) - 三关渐进式                │   │
│  └──────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────┘
```

---

## 目录结构

```
bmecomp2026/
├── README.md                         # 项目说明（本文件）
├── config.yaml                       # 统一配置文件
├── requirements.txt                  # Python 依赖
├── .gitignore
├── 设计方案.pdf                       # 原始设计方案
│
├── docs/                             # 文档目录
│   ├── 使用说明.md                    # 详细使用说明
│   ├── 技术文档.md                    # 技术实现文档
│   ├── 文献调研.md                    # 相关文献
│   ├── 硬件调研.md                    # 硬件选型
│   ├── 项目计划.md                    # 项目计划
│   └── 开源资源.md                    # 开源资源
│
├── src/
│   ├── python/                       # Python BCI 中间件
│   │   ├── main.py                   # 主入口
│   │   ├── bci/
│   │   │   ├── __init__.py
│   │   │   ├── hybridbci_adapter.py  # HybridBCI平台适配器（含Mock模式）
│   │   │   ├── data_collector.py     # BCI数据采集器
│   │   │   ├── signal_processor.py   # 三级信号滤波器
│   │   │   ├── attention_calculator.py # 专注力计算器
│   │   │   ├── socket_server.py      # 转发至Unity的Socket服务器
│   │   │   ├── protocol.py           # 通信协议定义
│   │   │   └── data_recorder.py      # 训练数据持久化
│   │   └── tests/
│   │       └── test_bci.py           # 单元测试
│   │
│   └── unity/                        # Unity VR 游戏项目
│       └── Assets/Scripts/
│           ├── Core/
│           │   ├── BCIManager.cs     # BCI数据管理器
│           │   ├── GameManager.cs    # 游戏状态管理器
│           │   └── LevelManager.cs   # 多关卡管理器
│           ├── Player/
│           │   └── PlayerController.cs  # 飞船控制器
│           ├── Gameplay/
│           │   ├── ObstacleManager.cs   # 障碍物生成系统
│           │   ├── CollectibleManager.cs # 收集物系统
│           │   ├── VisualFeedback.cs    # 视觉反馈系统
│           │   └── AdaptiveDifficulty.cs # 自适应难度
│           ├── UI/
│           │   ├── UIManager.cs         # UI管理器
│           │   └── SessionReport.cs     # 训练报告UI
│           └── Data/
│               └── SessionRecorder.cs   # 会话记录
│
└── tools/
    ├── setup.sh                      # 环境配置脚本
    └── build.sh                      # 构建脚本
```

---

## 快速开始

### 环境要求

| 组件 | 要求 |
|------|------|
| Python | 3.8+ |
| Unity | 2021.3 LTS+ |
| VR 头显 | Meta Quest 3 / 3S (开发阶段可选) |
| 脑电设备 | HNNK 单通道脑电头环 (开发阶段可选) |

### 1. 配置 Python 环境

```bash
# 克隆仓库
git clone git@github.com:tzhazuma/bmecomp2026.git
cd bmecomp2026

# 运行配置脚本
./tools/setup.sh

# 或手动配置
python3 -m venv venv
source venv/bin/activate
pip install -r requirements.txt
```

### 2. 启动 BCI 中间件（Mock 模式，无需硬件）

```bash
python src/python/main.py
```

默认使用 config.yaml 中的 mock 模式，自动生成模拟数据。

```bash
# 指定模式和参数
python src/python/main.py --mode mock
python src/python/main.py --mode tcp        # 连接真实平台
python src/python/main.py --mode local      # QLocalSocket 模式
python src/python/main.py --config /path/to/config.yaml
```

### 3. 启动 Unity 项目

1. 打开 Unity Hub，点击 **Open** 选择 `src/unity/` 目录
2. 安装必要 Packages：
   - XR Interaction Toolkit
   - Meta XR SDK (若使用 Quest)
   - TextMeshPro
   - Post Processing
3. 配置 XR：Edit → Project Settings → XR Plug-in Management → 启用 Oculus/OpenXR
4. 打开 `Assets/Scenes/MainMenu.unity`，点击 **Play**

### 4. 数据确认

Unity 场景运行后，BCIManager 会自动连接 Python 中间件（127.0.0.1:5555）。
如果 BCI 数据显示正常，说明通信链路通畅。

---

## 数据源模式说明

### Mock 模式（开发/测试，无需硬件）

```
python src/python/main.py
```

自动生成模拟专注力数据（正弦波 + 高斯噪声），IMU 数据，可用于 VR 游戏开发的端到端测试。

### TCP 模式（连接 HybridBCI 平台）

需要连接华南脑控 HybridBCI 平台，通过 TCP 端口获取真实 EEG/IMU 数据。

### Local 模式（QLocalSocket 连接）

使用 PyQt5 的 QLocalSocket 连接 HybridBCI 平台本地管道（默认 `HNNKPlatform`）。

---

## 游戏设计

### 星空守护者

玩家驾驶 VR 飞船穿越星空，通过专注力控制飞行状态，头动控制飞行方向，躲避障碍、收集星光。

| 关卡 | 名称 | 时长 | 核心机制 | 训练目标 |
|------|------|------|----------|----------|
| 1 | 星尘航道 | 180s | 基础避障 + 收集 | 基础专注力维持 |
| 2 | 彗星风暴 | 240s | 护盾系统（高专注=护盾） | 专注力抑制控制 |
| 3 | 迷失星域 | 300s | 路径显示（专注>60才显示） | 持续注意力 |

### 核心控制映射

```
专注力 A(t) → 飞船速度: v = v_min + (v_max - v_min) × A(t)/100
专注力 A(t) → 护盾强度: shield = clamp((A-30)/60, 0, 1)
专注力 A(t) → 稳定性: A<30 → 相机抖动，A>70 → 稳定
头动 Yaw     → 水平移动
头动 Pitch   → 垂直移动
```

### 反馈机制

| 专注力范围 | 视觉反馈 | 游戏表现 |
|------------|----------|----------|
| < 40 | 暗角 + 去饱和 + 相机抖动 + 灰暗星空 | 速度慢、无护盾、易碰撞 |
| 40 - 70 | 正常色彩、轻微光晕 | 正常速度、部分护盾 |
| > 70 | 暖色调、强光晕、金色星空 | 高速、护盾激活、连击模式 |
| 连击 ≥ 10s | 金色粒子爆发 + "专注连击!" | 收集物 3x 积分 |

---

## 配置参数

所有配置集中在 `config.yaml`：

```yaml
data_source:
  mode: mock              # mock | local | tcp

local:
  server_name: "HNNKPlatform"

tcp:
  host: "127.0.0.1"
  port: 8000

mock:
  attention_min: 30.0      # 模拟专注力最小值
  attention_max: 90.0      # 模拟专注力最大值
  attention_freq: 0.1      # 正弦波动频率
  imu_noise: 3.0           # IMU 噪声程度
  update_rate: 50          # 数据推送频率

unity_server:
  host: "0.0.0.0"
  port: 5555

signal_processing:
  window_size: 7           # 滑动窗口大小
  alpha: 0.7               # 指数平滑系数
  imu_deadzone: 5.0        # 头动死区角度

game:
  default_level: 1
  session_duration_sec: 180
  focus_threshold: 70
  combo_threshold_sec: 10

recording:
  enabled: true
  output_dir: "./session_data"
```

---

## 测试

```bash
python -m pytest src/python/tests/ -v
```

---

## 项目成员

- 毛亚轩 - 项目负责人 / Unity 开发
- 叶铮皓 - BCI 中间件 / Python 开发
- 夏陆贤 - BCI 集成 / 系统架构
- 唐志昊 - Unity 场景 / VR 交互

指导老师：熊泽

## 单位

上海科技大学 · 脑机接口赛道团队

## 相关资源

- HybridBCI 科研科创平台: https://www.pazhoulab.com/2026/03/8165/
- 华南脑控: http://www.ihnnk.cn
- 注意力训练官网: https://attention.ihnnk.com
