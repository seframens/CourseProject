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
