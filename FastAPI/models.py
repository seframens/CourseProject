# models.py
from pydantic import BaseModel, ConfigDict, field_validator, model_validator
from datetime import date, datetime
from typing import List, Optional
import re

class ProjectDetails(BaseModel):
    ProjectNumber: int
    ProjectName: str
    Description: Optional[str]
    Employer: str
    ProjectManagerFullName: str
    ProjectManagerQualification: str
    WorkTypeName: str
    ProjectDate: date
    Status: Optional[str]
    model_config = ConfigDict(from_attributes=True)

class ProjectCreateSmart(BaseModel):
    Name: str
    Description: Optional[str] = None
    Employer: str
    ProjectDate: date
    Status: Optional[str] = 'В очереди'
    ProjectManagerFullName: str
    WorkTypeName: str
    
    @field_validator('Name')
    @classmethod
    def validate_name(cls, v):
        if not v or len(v.strip()) == 0:
            raise ValueError('Название проекта не может быть пустым')
        if len(v) > 150:
            raise ValueError('Название проекта не может превышать 150 символов')
        return v.strip()
    
    @field_validator('Description')
    @classmethod
    def validate_description(cls, v):
        if v and len(v) > 500:
            raise ValueError('Описание проекта не может превышать 500 символов')
        return v
    
    @field_validator('Employer')
    @classmethod
    def validate_employer(cls, v):
        if not v or len(v.strip()) == 0:
            raise ValueError('Заказчик не может быть пустым')
        if len(v) > 150:
            raise ValueError('Название заказчика не может превышать 150 символов')
        return v.strip()
    
    @field_validator('ProjectDate')
    @classmethod
    def validate_project_date(cls, v):
        min_date = date(1900, 1, 1)
        if v < min_date:
            raise ValueError('Дата проекта не может быть раньше 01.01.1900')
        if v > date.today():
            raise ValueError('Дата проекта не может быть в будущем')
        return v
    
    @field_validator('Status')
    @classmethod
    def validate_status(cls, v):
        allowed_statuses = ['В очереди', 'Завершен', 'Активен']
        if v and v not in allowed_statuses:
            raise ValueError(f'Статус должен быть одним из: {", ".join(allowed_statuses)}')
        return v
    
    @field_validator('ProjectManagerFullName')
    @classmethod
    def validate_manager_name(cls, v):
        if not v or len(v.strip()) == 0:
            raise ValueError('ФИО руководителя не может быть пустым')
        if len(v) > 150:
            raise ValueError('ФИО руководителя не может превышать 150 символов')
        return v.strip()
    
    @field_validator('WorkTypeName')
    @classmethod
    def validate_work_type(cls, v):
        if not v or len(v.strip()) == 0:
            raise ValueError('Тип работ не может быть пустым')
        if len(v) > 200:
            raise ValueError('Название типа работ не может превышать 200 символов')
        return v.strip()

class ProjectUpdate(BaseModel):
    Name: Optional[str] = None
    Description: Optional[str] = None
    Employer: Optional[str] = None
    ProjectDate: Optional[date] = None
    Status: Optional[str] = None
    ProjectManagerFullName: Optional[str] = None
    WorkTypeName: Optional[str] = None
    
    @field_validator('Name')
    @classmethod
    def validate_name(cls, v):
        if v is not None:
            if len(v.strip()) == 0:
                raise ValueError('Название проекта не может быть пустым')
            if len(v) > 150:
                raise ValueError('Название проекта не может превышать 150 символов')
        return v
    
    @field_validator('Description')
    @classmethod
    def validate_description(cls, v):
        if v and len(v) > 500:
            raise ValueError('Описание проекта не может превышать 500 символов')
        return v
    
    @field_validator('Employer')
    @classmethod
    def validate_employer(cls, v):
        if v is not None:
            if len(v.strip()) == 0:
                raise ValueError('Заказчик не может быть пустым')
            if len(v) > 150:
                raise ValueError('Название заказчика не может превышать 150 символов')
        return v
    
    @field_validator('ProjectDate')
    @classmethod
    def validate_project_date(cls, v):
        if v:
            min_date = date(1900, 1, 1)
            if v < min_date:
                raise ValueError('Дата проекта не может быть раньше 01.01.1900')
            if v > date.today():
                raise ValueError('Дата проекта не может быть в будущем')
        return v
    
    @field_validator('Status')
    @classmethod
    def validate_status(cls, v):
        if v:
            allowed_statuses = ['В очереди', 'Завершен', 'Активен']
            if v not in allowed_statuses:
                raise ValueError(f'Статус должен быть одним из: {", ".join(allowed_statuses)}')
        return v
    
    @field_validator('ProjectManagerFullName')
    @classmethod
    def validate_manager_name(cls, v):
        if v is not None:
            if len(v.strip()) == 0:
                raise ValueError('ФИО руководителя не может быть пустым')
            if len(v) > 150:
                raise ValueError('ФИО руководителя не может превышать 150 символов')
        return v
    
    @field_validator('WorkTypeName')
    @classmethod
    def validate_work_type(cls, v):
        if v is not None:
            if len(v.strip()) == 0:
                raise ValueError('Тип работ не может быть пустым')
            if len(v) > 200:
                raise ValueError('Название типа работ не может превышать 200 символов')
        return v

class UserLogin(BaseModel):
    Login: str
    Password: str
    
    @field_validator('Login')
    @classmethod
    def validate_login(cls, v):
        if not v or len(v.strip()) == 0:
            raise ValueError('Логин не может быть пустым')
        if len(v) > 50:
            raise ValueError('Логин не может превышать 50 символов')
        return v.strip()
    
    @field_validator('Password')
    @classmethod
    def validate_password(cls, v):
        if not v or len(v.strip()) == 0:
            raise ValueError('Пароль не может быть пустым')
        if len(v) > 50:
            raise ValueError('Пароль не может превышать 50 символов')
        return v

class UserAuthResponse(BaseModel):
    success: bool
    user_id: Optional[int] = None
    role_name: Optional[str] = None
    full_name: Optional[str] = None
    message: Optional[str] = None

class ProjectFilter(BaseModel):
    status: Optional[str] = None
    project_date_from: Optional[date] = None
    project_date_to: Optional[date] = None
    project_manager: Optional[str] = None
    search_text: Optional[str] = None
    
    @field_validator('project_date_from', 'project_date_to')
    @classmethod
    def validate_dates(cls, v):
        if v:
            min_date = date(1900, 1, 1)
            if v < min_date:
                raise ValueError('Дата не может быть раньше 01.01.1900')
        return v

class PaginatedResponse(BaseModel):
    projects: List[ProjectDetails]
    total_count: int
    page: int
    total_pages: int
    has_next: bool
    has_prev: bool