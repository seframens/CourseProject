# database.py
from sqlalchemy import create_engine, Column, Integer, String, Date, ForeignKey, CheckConstraint, text
from sqlalchemy.ext.declarative import declarative_base
from sqlalchemy.orm import sessionmaker, relationship
from sqlalchemy import UniqueConstraint

# Подключение к базе данных
DATABASE_URL = "postgresql://admin:1@localhost/intidatabase"
engine = create_engine(DATABASE_URL)
SessionLocal = sessionmaker(autocommit=False, autoflush=False, bind=engine)
Base = declarative_base()

class Qualification(Base):
    __tablename__ = 'Qualification'
    __table_args__ = {'schema': 'IntiScheme'}
    
    QualificationId = Column('QualificationId', Integer, primary_key=True)
    Name = Column('Name', String(200), nullable=False)
    
    project_managers = relationship("ProjectManager", back_populates="qualification")

class ProjectManager(Base):
    __tablename__ = 'ProjectManager'
    __table_args__ = {'schema': 'IntiScheme'}
    
    ProjectManagerId = Column('ProjectManagerId', Integer, primary_key=True)
    QualificationId = Column('QualificationId', Integer, ForeignKey('IntiScheme.Qualification.QualificationId'))
    FullName = Column('FullName', String(150), nullable=False)
    BirthDate = Column('BirthDate', Date, nullable=False)
    Phone = Column('Phone', String(20))
    
    qualification = relationship("Qualification", back_populates="project_managers")
    projects = relationship("Project", back_populates="project_manager")

class Role(Base):
    __tablename__ = 'Role'
    __table_args__ = {'schema': 'IntiScheme'}
    
    RoleId = Column('RoleId', Integer, primary_key=True)
    Name = Column('Name', String(100), nullable=False)
    
    users = relationship("User", back_populates="role")

class User(Base):
    __tablename__ = 'User'
    __table_args__ = {'schema': 'IntiScheme'}
    
    UserId = Column('UserId', Integer, primary_key=True)
    RoleId = Column('RoleId', Integer, ForeignKey('IntiScheme.Role.RoleId'))
    FullName = Column('FullName', String(150), nullable=False)
    Login = Column('Login', String(50), nullable=False)
    Password = Column('Password', String(50), nullable=False)
    Phone = Column('Phone', String(20))
    
    role = relationship("Role", back_populates="users")
    
    __table_args__ = (
        UniqueConstraint('Login', name='uq_user_login'),
        UniqueConstraint('Phone', name='uq_user_phone'),
        {'schema': 'IntiScheme'}
    )

# НОВАЯ ТАБЛИЦА WorkType
class WorkType(Base):
    __tablename__ = 'WorkType'
    __table_args__ = {'schema': 'IntiScheme'}
    
    WorkTypeId = Column('WorkTypeId', Integer, primary_key=True)
    Name = Column('Name', String(150), nullable=False)
    
    projects = relationship("Project", back_populates="work_type")

# ОБНОВЛЕННАЯ ТАБЛИЦА Project
class Project(Base):
    __tablename__ = 'Project'
    __table_args__ = {'schema': 'IntiScheme'}
    
    ProjectId = Column('ProjectId', Integer, primary_key=True)
    ProjectManagerId = Column('ProjectManagerId', Integer, ForeignKey('IntiScheme.ProjectManager.ProjectManagerId'))
    WorkTypeId = Column('WorkTypeId', Integer, ForeignKey('IntiScheme.WorkType.WorkTypeId'))
    Name = Column('Name', String(150), nullable=False)
    Description = Column('Description', String(500))
    Employer = Column('Employer', String(150), nullable=False)
    ProjectDate = Column('ProjectDate', Date, nullable=False)
    Status = Column('Status', String(30))
    
    project_manager = relationship("ProjectManager", back_populates="projects")
    work_type = relationship("WorkType", back_populates="projects")
    
    __table_args__ = (
        CheckConstraint(
            "Status IN ('В очереди', 'Завершен', 'Активен')", 
            name='check_project_status'
        ),
        {'schema': 'IntiScheme'}
    )

def get_db():
    db = SessionLocal()
    try:
        db.execute(text('SET search_path TO "IntiScheme"'))
        yield db
    finally:
        db.close()