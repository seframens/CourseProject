# check_environment.sh
#!/bin/bash

echo "🔍 ПРОВЕРКА ОКРУЖЕНИЯ"
echo "===================="

# Проверка текущей директории
echo "Текущая директория: $(pwd)"
echo ""

# Проверка Python
echo "Python версия:"
python --version
echo ""

# Проверка pip
echo "Pip версия:"
pip --version
echo ""

# Проверка виртуального окружения
if [ -d "venv" ]; then
    echo "✅ Виртуальное окружение найдено"
    echo "Путь: $(pwd)/venv"
else
    echo "❌ Виртуальное окружение НЕ найдено"
    echo "Создайте его командой: python -m venv venv"
fi
echo ""

# Проверка зависимостей
echo "Установленные пакеты:"
if [ -d "venv" ]; then
    venv/bin/pip list --format=columns
else
    pip list --format=columns
fi
echo ""

# Проверка файлов проекта
echo "Файлы проекта:"
ls -la *.py
echo ""

echo "Проверка завершена! ✅"