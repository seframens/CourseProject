# main.py
from fastapi import FastAPI, Depends, HTTPException, Query
from sqlalchemy.orm import Session
from sqlalchemy import text
from typing import List, Optional
import logging
from datetime import date
from math import ceil

from database import get_db, Project, ProjectManager, User, Role, WorkType, Qualification
from models import (ProjectDetails, ProjectCreateSmart, ProjectUpdate, 
                   UserLogin, UserAuthResponse, ProjectFilter, PaginatedResponse)

app = FastAPI(title="ИНТИ API", version="1.0.0")

# Настройка логирования
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Константы
PROJECTS_PER_PAGE = 15
MIN_DATE = date(1900, 1, 1)

@app.get("/")
def read_root():
    return {"message": "Project Management API"}

# === АВТОРИЗАЦИЯ ===

@app.post("/auth/login", response_model=UserAuthResponse)
def login_user(login_data: UserLogin, db: Session = Depends(get_db)):
    """Авторизация пользователя"""
    try:
        user = db.query(User).filter(
            User.Login == login_data.Login,
            User.Password == login_data.Password
        ).first()
        
        if not user:
            logger.warning(f"Неудачная попытка входа для пользователя: {login_data.Login}")
            return UserAuthResponse(
                success=False,
                message="Неверный логин или пароль"
            )
        
        role = db.query(Role).filter(Role.RoleId == user.RoleId).first()
        role_name = role.Name if role else "Неизвестная роль"
        
        logger.info(f"Успешный вход для пользователя: {user.FullName}, роль: {role_name}")
        
        return UserAuthResponse(
            success=True,
            user_id=user.UserId,
            role_name=role_name,
            full_name=user.FullName,
            message="Авторизация успешна"
        )
        
    except Exception as e:
        logger.error(f"Ошибка входа: {str(e)}")
        return UserAuthResponse(
            success=False,
            message=f"Ошибка сервера: {str(e)}"
        )

# === ОСНОВНОЙ ИНТЕРФЕЙС С ФИЛЬТРАЦИЕЙ И ПАГИНАЦИЕЙ ===

@app.post("/projects/details", response_model=PaginatedResponse)
def get_projects_details(
    filters: ProjectFilter,
    db: Session = Depends(get_db),
    page: int = Query(1, ge=1, description="Номер страницы")
):
    """Получить объединенные данные проектов с фильтрацией и пагинацией"""
    try:
        # Запрос для подсчета общего количества
        count_query = """
        SELECT COUNT(*) as total_count
        FROM "Project" p
        JOIN "ProjectManager" pm ON p."ProjectManagerId" = pm."ProjectManagerId" 
        JOIN "Qualification" q ON pm."QualificationId" = q."QualificationId"
        JOIN "WorkType" wt ON p."WorkTypeId" = wt."WorkTypeId"
        """
        
        # Запрос для получения данных
        data_query = """
        SELECT 
            p."ProjectId" as project_number,
            p."Name" as project_name,
            p."Description" as description,
            p."Employer" as employer,
            pm."FullName" as project_manager_full_name,
            q."Name" as project_manager_qualification,
            wt."Name" as work_type_name,
            p."ProjectDate" as project_date,
            p."Status" as status
        FROM "Project" p
        JOIN "ProjectManager" pm ON p."ProjectManagerId" = pm."ProjectManagerId" 
        JOIN "Qualification" q ON pm."QualificationId" = q."QualificationId"
        JOIN "WorkType" wt ON p."WorkTypeId" = wt."WorkTypeId"
        """
        
        conditions = []
        params = {}
        
        # Фильтр по статусу
        if filters.status:
            conditions.append('p."Status" = :status')
            params['status'] = filters.status
            
        # Фильтр по дате (от)
        if filters.project_date_from:
            conditions.append('p."ProjectDate" >= :date_from')
            params['date_from'] = filters.project_date_from
            
        # Фильтр по дате (до)
        if filters.project_date_to:
            conditions.append('p."ProjectDate" <= :date_to')
            params['date_to'] = filters.project_date_to
            
        # Фильтр по руководителю
        if filters.project_manager:
            conditions.append('pm."FullName" ILIKE :manager')
            params['manager'] = f"%{filters.project_manager}%"
            
        # Поиск по названию проекта
        if filters.search_text:
            conditions.append('p."Name" ILIKE :search')
            params['search'] = f"%{filters.search_text}%"
        
        # Добавляем условия в запросы
        where_clause = ""
        if conditions:
            where_clause = " WHERE " + " AND ".join(conditions)
        
        count_query += where_clause
        data_query += where_clause
        
        # Добавляем сортировку и пагинацию к data_query
        data_query += " ORDER BY p.\"ProjectId\" LIMIT :limit OFFSET :offset"
        
        # Вычисляем offset для пагинации
        offset = (page - 1) * PROJECTS_PER_PAGE
        params['limit'] = PROJECTS_PER_PAGE
        params['offset'] = offset
        
        # Выполняем запрос для подсчета общего количества
        count_result = db.execute(text(count_query), params)
        total_count = count_result.scalar()
        
        # Выполняем запрос для получения данных
        result = db.execute(text(data_query), params)
        projects_data = result.fetchall()
        
        # Преобразуем данные в модели
        projects = [
            ProjectDetails(
                ProjectNumber=project.project_number,
                ProjectName=project.project_name,
                Description=project.description,
                Employer=project.employer,
                ProjectManagerFullName=project.project_manager_full_name,
                ProjectManagerQualification=project.project_manager_qualification,
                WorkTypeName=project.work_type_name,
                ProjectDate=project.project_date,
                Status=project.status
            )
            for project in projects_data
        ]
        
        # Вычисляем общее количество страниц
        total_pages = ceil(total_count / PROJECTS_PER_PAGE) if total_count > 0 else 1
        
        return PaginatedResponse(
            projects=projects,
            total_count=total_count,
            page=page,
            total_pages=total_pages,
            has_next=page < total_pages,
            has_prev=page > 1
        )
        
    except Exception as e:
        logger.error(f"Ошибка при получении проектов: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Ошибка при получении проектов: {str(e)}")

# === "УМНАЯ" ВСТАВКА ПРОЕКТА ===

@app.post("/projects/create", response_model=dict)
def create_project_smart(project_data: ProjectCreateSmart, db: Session = Depends(get_db)):
    """Создать проект с автоматическим подбором менеджера и типа работ"""
    try:
        # Ищем менеджера по ФИО
        manager_query = """
        SELECT pm."ProjectManagerId"
        FROM "ProjectManager" pm
        WHERE pm."FullName" = :full_name
        """
        
        manager_result = db.execute(
            text(manager_query), 
            {'full_name': project_data.ProjectManagerFullName}
        )
        manager = manager_result.fetchone()
        
        if not manager:
            raise HTTPException(
                status_code=400, 
                detail=f"Менеджер '{project_data.ProjectManagerFullName}' не найден"
            )
        
        # Ищем тип работ по названию
        work_type_query = """
        SELECT wt."WorkTypeId"
        FROM "WorkType" wt
        WHERE wt."Name" = :work_type_name
        """
        
        work_type_result = db.execute(
            text(work_type_query), 
            {'work_type_name': project_data.WorkTypeName}
        )
        work_type = work_type_result.fetchone()
        
        if not work_type:
            raise HTTPException(
                status_code=400, 
                detail=f"Тип работ '{project_data.WorkTypeName}' не найден"
            )
        
        # Создаем проект
        db_project = Project(
            ProjectManagerId=manager.ProjectManagerId,
            WorkTypeId=work_type.WorkTypeId,
            Name=project_data.Name,
            Description=project_data.Description,
            Employer=project_data.Employer,
            ProjectDate=project_data.ProjectDate,
            Status=project_data.Status
        )
        
        db.add(db_project)
        db.commit()
        db.refresh(db_project)
        
        logger.info(f"Проект успешно создан: {project_data.Name}")
        return {
            "success": True,
            "project_id": db_project.ProjectId,
            "message": "Проект успешно создан"
        }
        
    except HTTPException:
        raise
    except Exception as e:
        db.rollback()
        logger.error(f"Ошибка при создании проекта: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Ошибка при создании проекта: {str(e)}")

# === ОБНОВЛЕНИЕ ПРОЕКТА ===

@app.put("/projects/{project_id}", response_model=dict)
def update_project(project_id: int, project_data: ProjectUpdate, db: Session = Depends(get_db)):
    """Обновить проект"""
    try:
        db_project = db.query(Project).filter(Project.ProjectId == project_id).first()
        if not db_project:
            raise HTTPException(status_code=404, detail="Проект не найден")
        
        update_data = project_data.dict(exclude_unset=True)
        
        # Если обновляется менеджер - ищем его ID
        if project_data.ProjectManagerFullName:
            manager_query = """
            SELECT pm."ProjectManagerId"
            FROM "ProjectManager" pm
            WHERE pm."FullName" = :full_name
            """
            
            manager_result = db.execute(
                text(manager_query), 
                {'full_name': project_data.ProjectManagerFullName}
            )
            manager = manager_result.fetchone()
            
            if not manager:
                raise HTTPException(
                    status_code=400, 
                    detail=f"Менеджер '{project_data.ProjectManagerFullName}' не найден"
                )
            
            db_project.ProjectManagerId = manager.ProjectManagerId
        
        # Если обновляется тип работ - ищем его ID
        if project_data.WorkTypeName:
            work_type_query = """
            SELECT wt."WorkTypeId"
            FROM "WorkType" wt
            WHERE wt."Name" = :work_type_name
            """
            
            work_type_result = db.execute(
                text(work_type_query), 
                {'work_type_name': project_data.WorkTypeName}
            )
            work_type = work_type_result.fetchone()
            
            if not work_type:
                raise HTTPException(
                    status_code=400, 
                    detail=f"Тип работ '{project_data.WorkTypeName}' не найден"
                )
            
            db_project.WorkTypeId = work_type.WorkTypeId
        
        # Обновляем остальные поля
        for field, value in update_data.items():
            if field not in ['ProjectManagerFullName', 'WorkTypeName'] and value is not None:
                setattr(db_project, field, value)
        
        db.commit()
        
        logger.info(f"Проект успешно обновлен: {project_id}")
        return {
            "success": True,
            "message": "Проект успешно обновлен"
        }
        
    except HTTPException:
        raise
    except Exception as e:
        db.rollback()
        logger.error(f"Ошибка при обновлении проекта: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Ошибка при обновлении проекта: {str(e)}")

# === УДАЛЕНИЕ ПРОЕКТА ===

@app.delete("/projects/{project_id}")
def delete_project(project_id: int, db: Session = Depends(get_db)):
    """Удалить проект"""
    try:
        db_project = db.query(Project).filter(Project.ProjectId == project_id).first()
        if not db_project:
            raise HTTPException(status_code=404, detail="Проект не найден")
        
        db.delete(db_project)
        db.commit()
        
        logger.info(f"Проект успешно удален: {project_id}")
        return {
            "success": True,
            "message": "Проект успешно удален"
        }
        
    except Exception as e:
        db.rollback()
        logger.error(f"Ошибка при удалении проекта: {str(e)}")
        raise HTTPException(status_code=500, detail=f"Ошибка при удалении проекта: {str(e)}")

# === СПИСКИ ДЛЯ ФОРМ ===

@app.get("/managers/list")
def get_managers_list(db: Session = Depends(get_db)):
    """Получить список менеджеров с их квалификациями"""
    try:
        query = """SELECT
                       pm."FullName",
                       q."Name" as qualification -- Изменен алиас на строчный
                   FROM "ProjectManager" pm
                   JOIN "Qualification" q ON pm."QualificationId" = q."QualificationId"
                   ORDER BY pm."FullName"
               """
        result = db.execute(text(query))
        managers = result.fetchall()

        return [{"full_name": m.FullName, "qualification": m.qualification} for m in managers]
    except Exception as e:
        logger.error(f"Ошибка при получении списка менеджеров: {str(e)}") 
        raise HTTPException(status_code=500, detail=f"Ошибка при получении списка менеджеров: {str(e)}")

@app.get("/work-types/list")
def get_work_types_list(db: Session = Depends(get_db)):
    """Получить список типов работ"""
    try:
        work_types = db.query(WorkType).order_by(WorkType.Name).all()
        return [
            {
                "name": wt.Name
            }
            for wt in work_types
        ]
    except Exception as e:
        raise HTTPException(status_code=500, detail=f"Ошибка при получении списка типов работ: {str(e)}")

@app.get("/statuses/list")
def get_statuses_list():
    """Получить список возможных статусов"""
    return [
        {"name": "В очереди"},
        {"name": "Активен"}, 
        {"name": "Завершен"}
    ]

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=8000)