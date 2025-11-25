#!/bin/bash

# =============================================================================
# СКРИПТ УПРАВЛЕНИЯ FASTAPI СЕРВЕРОМ
# =============================================================================

# Цвета для красивого вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
PURPLE='\033[0;35m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# =============================================================================
# КОНФИГУРАЦИЯ (ИЗМЕНИТЕ ПОД СВОЙ ПРОЕКТ)
# =============================================================================

# Основные настройки
PROJECT_NAME="FastAPI Project Management"
PROJECT_DIR="/home/user/Code/CourseProject"  # ⚠️ ИЗМЕНИТЕ ЭТОТ ПУТЬ!
SERVICE_NAME="fastapi-project"
SERVICE_FILE="/etc/systemd/system/${SERVICE_NAME}.service"

# Пользователь под которым будет работать сервис
SERVICE_USER=$(whoami)
SERVICE_GROUP=$(whoami)

# =============================================================================
# ВСПОМОГАТЕЛЬНЫЕ ФУНКЦИИ
# =============================================================================

# Функция для вывода информационных сообщений
print_info() {
    echo -e "${BLUE}ℹ️  [INFO]${NC} $1"
}

# Функция для вывода успешных операций
print_success() {
    echo -e "${GREEN}✅ [SUCCESS]${NC} $1"
}

# Функция для вывода ошибок
print_error() {
    echo -e "${RED}❌ [ERROR]${NC} $1"
}

# Функция для вывода предупреждений
print_warning() {
    echo -e "${YELLOW}⚠️  [WARNING]${NC} $1"
}

# Функция для вывода заголовков
print_header() {
    echo -e "${PURPLE}=========================================${NC}"
    echo -e "${PURPLE} $1 ${NC}"
    echo -e "${PURPLE}=========================================${NC}"
}

# Функция для вывода шага процесса
print_step() {
    echo -e "${CYAN}🔹 $1${NC}"
}

# =============================================================================
# ФУНКЦИИ ПРОВЕРКИ
# =============================================================================

# Проверка прав доступа
check_permissions() {
    print_step "Проверка прав доступа..."
    
    if [[ $EUID -eq 0 ]]; then
        print_error "Не запускайте скрипт с правами root (sudo)!"
        print_info "Запускайте от обычного пользователя: ./manage_server.sh"
        exit 1
    fi
    
    print_success "Права доступа в порядке"
}

# Проверка существования проекта
check_project_exists() {
    print_step "Проверка существования проекта..."
    
    if [ ! -d "$PROJECT_DIR" ]; then
        print_error "Папка проекта не найдена: $PROJECT_DIR"
        print_info "Убедитесь что:"
        print_info "1. Проект находится по правильному пути"
        print_info "2. Вы изменили PROJECT_DIR в скрипте"
        print_info "Текущий путь в скрипте: $PROJECT_DIR"
        exit 1
    fi
    
    if [ ! -f "$PROJECT_DIR/main.py" ]; then
        print_error "Основной файл main.py не найден"
        print_info "Убедитесь что main.py находится в папке проекта"
        exit 1
    fi
    
    print_success "Проект найден: $PROJECT_DIR"
}

# Проверка виртуального окружения
check_virtualenv() {
    print_step "Проверка виртуального окружения..."
    
    VENV_DIR="$PROJECT_DIR/venv"
    
    if [ ! -d "$VENV_DIR" ]; then
        print_error "Виртуальное окружение не найдено: $VENV_DIR"
        print_info "Создайте виртуальное окружение:"
        print_info "cd $PROJECT_DIR"
        print_info "python -m venv venv"
        print_info "source venv/bin/activate"
        print_info "pip install -r requirements.txt"
        exit 1
    fi
    
    print_success "Виртуальное окружение найдено"
}

# Проверка зависимостей
check_dependencies() {
    print_step "Проверка зависимостей..."
    
    # Проверяем Uvicorn
    if ! $PROJECT_DIR/venv/bin/pip show uvicorn > /dev/null 2>&1; then
        print_warning "Uvicorn не установлен"
        print_info "Устанавливаю Uvicorn..."
        
        if $PROJECT_DIR/venv/bin/pip install uvicorn; then
            print_success "Uvicorn успешно установлен"
        else
            print_error "Ошибка установки Uvicorn"
            exit 1
        fi
    else
        print_success "Uvicorn уже установлен"
    fi
    
    # Проверяем остальные зависимости
    REQUIRED_PACKAGES=("fastapi" "sqlalchemy" "psycopg2-binary")
    
    for package in "${REQUIRED_PACKAGES[@]}"; do
        if ! $PROJECT_DIR/venv/bin/pip show "$package" > /dev/null 2>&1; then
            print_warning "$package не установлен"
        fi
    done
}

# Проверка конфигурации Uvicorn
check_uvicorn_config() {
    print_step "Проверка конфигурации Uvicorn..."
    
    if [ ! -f "$PROJECT_DIR/uvicorn_config.py" ]; then
        print_warning "Конфигурационный файл Uvicorn не найден"
        print_info "Создаю конфигурационный файл..."
        
        cat > "$PROJECT_DIR/uvicorn_config.py" << 'EOF'
import uvicorn

# Конфигурация Uvicorn
config = uvicorn.Config(
    "main:app",
    host="0.0.0.0",
    port=8000,
    workers=1,
    log_level="info",
    access_log=True,
    timeout_keep_alive=5,
    limit_max_requests=1000,
)

# Создаем сервер
server = uvicorn.Server(config)

if __name__ == "__main__":
    server.run()
EOF
        print_success "Конфигурационный файл Uvicorn создан"
    else
        print_success "Конфигурационный файл Uvicorn найден"
    fi
}

# =============================================================================
# ФУНКЦИИ УПРАВЛЕНИЯ СЕРВИСОМ
# =============================================================================

# Создание systemd сервиса для Uvicorn
create_uvicorn_service() {
    print_step "Создание systemd сервиса для Uvicorn..."
    
    # Создаем временный файл
    TEMP_SERVICE_FILE="/tmp/${SERVICE_NAME}.service"
    
    cat > "$TEMP_SERVICE_FILE" << EOF
[Unit]
Description=$PROJECT_NAME (Uvicorn)
After=network.target postgresql.service
Wants=postgresql.service
Documentation=https://github.com/yourusername/yourproject

[Service]
Type=simple
User=$SERVICE_USER
Group=$SERVICE_GROUP
WorkingDirectory=$PROJECT_DIR
Environment=PATH=$PROJECT_DIR/venv/bin
Environment=PYTHONPATH=$PROJECT_DIR
ExecStart=$PROJECT_DIR/venv/bin/python uvicorn_config.py
ExecReload=/bin/kill -s HUP \$MAINPID
Restart=always
RestartSec=5
StandardOutput=journal
StandardError=journal

# Безопасность
NoNewPrivileges=yes
PrivateTmp=yes
ProtectSystem=strict
ProtectHome=yes
ReadWritePaths=$PROJECT_DIR

[Install]
WantedBy=multi-user.target
EOF

    print_success "Сервисный файл создан временно"
    
    # Копируем с правами sudo
    print_step "Копирование сервисного файла в systemd..."
    sudo cp "$TEMP_SERVICE_FILE" "$SERVICE_FILE"
    sudo chmod 644 "$SERVICE_FILE"
    
    # Удаляем временный файл
    rm -f "$TEMP_SERVICE_FILE"
    
    print_success "Uvicorn сервис установлен: $SERVICE_FILE"
}

# Проверка существования сервиса
check_service_exists() {
    if [ ! -f "$SERVICE_FILE" ]; then
        print_error "Сервис не установлен"
        print_info "Сначала выполните установку: ./manage_server.sh install"
        exit 1
    fi
}

# =============================================================================
# ОСНОВНЫЕ КОМАНДЫ
# =============================================================================

# Установка и настройка
cmd_install() {
    print_header "УСТАНОВКА И НАСТРОЙКА СЕРВЕРА (Uvicorn)"
    
    # Выполняем проверки
    check_permissions
    check_project_exists
    check_virtualenv
    check_dependencies
    check_uvicorn_config
    
    # Создаем сервис
    create_uvicorn_service
    
    # Обновляем systemd
    print_step "Обновление systemd..."
    if sudo systemctl daemon-reload; then
        print_success "Systemd обновлен"
    else
        print_error "Ошибка обновления systemd"
        exit 1
    fi
    
    # Включаем автозапуск
    print_step "Включение автозапуска..."
    if sudo systemctl enable "$SERVICE_NAME"; then
        print_success "Автозапуск включен"
    else
        print_error "Ошибка включения автозапуска"
        exit 1
    fi
    
    print_success "Установка завершена успешно!"
    echo ""
    print_info "Для запуска сервера выполните: ./manage_server.sh start"
    print_info "Для проверки статуса: ./manage_server.sh status"
    print_info "Для просмотра логов: ./manage_server.sh logs"
}

# Запуск сервера
cmd_start() {
    print_header "ЗАПУСК СЕРВЕРА"
    
    check_service_exists
    
    print_step "Запуск сервиса $SERVICE_NAME..."
    if sudo systemctl start "$SERVICE_NAME"; then
        print_success "Сервер запущен"
    else
        print_error "Ошибка запуска сервера"
        exit 1
    fi
    
    # Даем время на запуск
    sleep 3
    
    # Показываем статус
    cmd_status
}

# Остановка сервера
cmd_stop() {
    print_header "ОСТАНОВКА СЕРВЕРА"
    
    check_service_exists
    
    print_step "Остановка сервиса $SERVICE_NAME..."
    if sudo systemctl stop "$SERVICE_NAME"; then
        print_success "Сервер остановлен"
    else
        print_error "Ошибка остановки сервера"
        exit 1
    fi
    
    sleep 2
    cmd_status
}

# Перезапуск сервера
cmd_restart() {
    print_header "ПЕРЕЗАПУСК СЕРВЕРА"
    
    check_service_exists
    
    print_step "Перезапуск сервиса $SERVICE_NAME..."
    if sudo systemctl restart "$SERVICE_NAME"; then
        print_success "Сервер перезапущен"
    else
        print_error "Ошибка перезапуска сервера"
        exit 1
    fi
    
    sleep 2
    cmd_status
}

# Проверка статуса
cmd_status() {
    print_header "СТАТУС СЕРВЕРА"
    
    check_service_exists
    
    print_step "Статус сервиса $SERVICE_NAME:"
    sudo systemctl status "$SERVICE_NAME" --no-pager -l
}

# Просмотр логов в реальном времени
cmd_logs() {
    print_header "ЖИВЫЕ ЛОГИ СЕРВЕРА"
    
    check_service_exists
    
    print_info "Для выхода нажмите Ctrl+C"
    echo ""
    sudo journalctl -u "$SERVICE_NAME" -f
}

# Просмотр последних логов
cmd_logs_tail() {
    print_header "ПОСЛЕДНИЕ ЛОГИ СЕРВЕРА"
    
    check_service_exists
    
    print_step "Последние 100 строк логов:"
    sudo journalctl -u "$SERVICE_NAME" --no-pager -n 100
}

# Удаление сервиса
cmd_uninstall() {
    print_header "УДАЛЕНИЕ СЕРВИСА"
    
    if [ ! -f "$SERVICE_FILE" ]; then
        print_error "Сервис не установлен"
        exit 1
    fi
    
    print_step "Остановка сервиса..."
    sudo systemctl stop "$SERVICE_NAME" 2>/dev/null || true
    
    print_step "Отключение автозапуска..."
    sudo systemctl disable "$SERVICE_NAME" 2>/dev/null || true
    
    print_step "Удаление сервисного файла..."
    sudo rm -f "$SERVICE_FILE"
    
    print_step "Обновление systemd..."
    sudo systemctl daemon-reload
    sudo systemctl reset-failed
    
    print_success "Сервис полностью удален!"
}

# Запуск в режиме разработки
cmd_dev() {
    print_header "РЕЖИМ РАЗРАБОТКИ"
    
    check_project_exists
    check_virtualenv
    
    print_info "Запуск сервера в режиме разработки..."
    print_info "Сервер будет перезагружаться при изменениях кода"
    print_info "Для остановки нажмите Ctrl+C"
    echo ""
    
    cd "$PROJECT_DIR" || exit 1
    source venv/bin/activate
    uvicorn main:app --host 0.0.0.0 --port 8000 --reload
}

# Проверка окружения
cmd_check() {
    print_header "ПРОВЕРКА ОКРУЖЕНИЯ"
    
    check_permissions
    check_project_exists
    check_virtualenv
    check_dependencies
    check_uvicorn_config
    
    if [ -f "$SERVICE_FILE" ]; then
        print_success "Systemd сервис установлен"
        cmd_status
    else
        print_warning "Systemd сервис не установлен"
        print_info "Для установки выполните: ./manage_server.sh install"
    fi
}

# Показать справку
cmd_help() {
    print_header "СПРАВКА ПО УПРАВЛЕНИЮ СЕРВЕРОМ"
    
    echo "Использование: $0 {команда}"
    echo ""
    echo "Доступные команды:"
    echo ""
    echo -e "${GREEN}Установка и настройка:${NC}"
    echo "  install     - Полная установка и настройка автозапуска (Uvicorn)"
    echo "  check       - Проверка окружения и зависимостей"
    echo ""
    echo -e "${BLUE}Управление сервером:${NC}"
    echo "  start       - Запуск сервера"
    echo "  stop        - Остановка сервера"
    echo "  restart     - Перезапуск сервера"
    echo "  status      - Показать статус сервера"
    echo ""
    echo -e "${YELLOW}Мониторинг:${NC}"
    echo "  logs        - Просмотр логов в реальном времени"
    echo "  logs_tail   - Показать последние логи"
    echo ""
    echo -e "${RED}Удаление:${NC}"
    echo "  uninstall   - Полное удаление сервиса"
    echo ""
    echo -e "${PURPLE}Разработка:${NC}"
    echo "  dev         - Запуск в режиме разработки (с авто-перезагрузкой)"
    echo "  help        - Показать эту справку"
    echo ""
    echo "Примеры:"
    echo "  ./manage_server.sh install    # Первая установка"
    echo "  ./manage_server.sh start      # Запуск сервера"
    echo "  ./manage_server.sh logs       # Просмотр логов"
    echo ""
}

# =============================================================================
# ОСНОВНАЯ ЛОГИКА
# =============================================================================

# Проверяем что передана команда
if [ $# -eq 0 ]; then
    print_error "Не указана команда"
    echo ""
    cmd_help
    exit 1
fi

# Обрабатываем команду
case "$1" in
    install)
        cmd_install
        ;;
    start)
        cmd_start
        ;;
    stop)
        cmd_stop
        ;;
    restart)
        cmd_restart
        ;;
    status)
        cmd_status
        ;;
    logs)
        cmd_logs
        ;;
    logs_tail)
        cmd_logs_tail
        ;;
    uninstall)
        cmd_uninstall
        ;;
    dev)
        cmd_dev
        ;;
    check)
        cmd_check
        ;;
    help|--help|-h)
        cmd_help
        ;;
    *)
        print_error "Неизвестная команда: $1"
        echo ""
        cmd_help
        exit 1
        ;;
esac