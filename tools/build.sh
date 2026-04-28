#!/bin/bash
# BCI-VR 注意力训练系统 - 构建脚本

set -e

echo "=========================================="
echo "BCI-VR 注意力训练系统 - 构建脚本"
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

# 激活虚拟环境
echo ""
echo "激活虚拟环境..."
VENV_DIR="venv"
if [ "${MACHINE}" = "Linux" ] || [ "${MACHINE}" = "Mac" ]; then
    source ${VENV_DIR}/bin/activate
elif [ "${MACHINE}" = "Cygwin" ] || [ "${MACHINE}" = "MinGw" ]; then
    source ${VENV_DIR}/Scripts/activate
fi

# 运行测试
echo ""
echo "运行Python测试..."
cd src/python
${PYTHON_CMD:-python} -m pytest tests/ -v
cd ../..

# 代码检查
echo ""
echo "运行代码检查..."
cd src/python
${PYTHON_CMD:-python} -m flake8 bci/ --max-line-length=120 --ignore=E501,W503
cd ../..

# 类型检查
echo ""
echo "运行类型检查..."
cd src/python
${PYTHON_CMD:-python} -m mypy bci/ --ignore-missing-imports
cd ../..

# 格式化检查
echo ""
echo "检查代码格式..."
cd src/python
${PYTHON_CMD:-python} -m black --check bci/ || echo "警告: 代码格式不符合black标准"
cd ../..

echo ""
echo "=========================================="
echo "构建完成！"
echo "=========================================="
echo ""
echo "下一步："
echo "1. 运行BCI服务器："
echo "   python src/python/main.py"
echo ""
echo "2. 启动Unity项目："
echo "   使用Unity Hub打开 src/unity/ 目录"
echo ""
