#!/bin/bash
# BCI-VR 注意力训练系统 - 环境配置脚本

set -e

echo "=========================================="
echo "BCI-VR 注意力训练系统 - 环境配置"
echo "=========================================="

# 检测操作系统
OS="$(uname -s)"
case "${OS}" in
    Linux*)     MACHINE=Linux;;
    Darwin*)    MACHINE=Mac;;
    CYGWIN*)    MACHINE=Cygwin;;
    MINGW*)     MACHINE=MinGw;;
    *)          MACHINE="UNKNOWN:${OS}"
esac
echo "操作系统: ${MACHINE}"

# 检测Python版本
echo ""
echo "检查Python环境..."
if command -v python3 &> /dev/null; then
    PYTHON_VERSION=$(python3 --version)
    echo "Python版本: ${PYTHON_VERSION}"
    PYTHON_CMD=python3
elif command -v python &> /dev/null; then
    PYTHON_VERSION=$(python --version)
    echo "Python版本: ${PYTHON_VERSION}"
    PYTHON_CMD=python
else
    echo "错误: 未找到Python，请先安装Python 3.8+"
    exit 1
fi

# 检测pip
echo ""
echo "检查pip..."
if command -v pip3 &> /dev/null; then
    PIP_VERSION=$(pip3 --version)
    echo "pip版本: ${PIP_VERSION}"
    PIP_CMD=pip3
elif command -v pip &> /dev/null; then
    PIP_VERSION=$(pip --version)
    echo "pip版本: ${PIP_VERSION}"
    PIP_CMD=pip
else
    echo "错误: 未找到pip，请先安装pip"
    exit 1
fi

# 创建虚拟环境
echo ""
echo "创建虚拟环境..."
VENV_DIR="venv"
if [ -d "${VENV_DIR}" ]; then
    echo "虚拟环境已存在: ${VENV_DIR}"
else
    ${PYTHON_CMD} -m venv ${VENV_DIR}
    echo "虚拟环境已创建: ${VENV_DIR}"
fi

# 激活虚拟环境
echo ""
echo "激活虚拟环境..."
if [ "${MACHINE}" = "Linux" ] || [ "${MACHINE}" = "Mac" ]; then
    source ${VENV_DIR}/bin/activate
elif [ "${MACHINE}" = "Cygwin" ] || [ "${MACHINE}" = "MinGw" ]; then
    source ${VENV_DIR}/Scripts/activate
fi
echo "虚拟环境已激活"

# 升级pip
echo ""
echo "升级pip..."
${PIP_CMD} install --upgrade pip

# 安装依赖
echo ""
echo "安装Python依赖..."
${PIP_CMD} install -r requirements.txt

echo ""
echo "=========================================="
echo "环境配置完成！"
echo "=========================================="
echo ""
echo "使用方法："
echo "1. 激活虚拟环境："
if [ "${MACHINE}" = "Linux" ] || [ "${MACHINE}" = "Mac" ]; then
    echo "   source venv/bin/activate"
elif [ "${MACHINE}" = "Cygwin" ] || [ "${MACHINE}" = "MinGw" ]; then
    echo "   source venv/Scripts/activate"
fi
echo ""
echo "2. 运行BCI服务器："
echo "   python src/python/main.py"
echo ""
echo "3. 运行测试："
echo "   python -m pytest src/python/tests/"
echo ""
echo "4. 启动Unity项目："
echo "   使用Unity Hub打开 src/unity/ 目录"
echo ""
