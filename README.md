# Document Management System (SWEN3)

[![.NET CI](https://github.com/lkalchhauser/swen3-document-management-system/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/lkalchhauser/swen3-document-management-system/actions/workflows/dotnet-ci.yml)

A modern, microservices-based document management system built with .NET 8, React, and RabbitMQ for asynchronous processing.

## 📋 Table of Contents
- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Technologies](#technologies)
- [Getting Started](#getting-started)
- [API Documentation](#api-documentation)
- [Testing](#testing)
- [Logging](#logging)
- [Project Structure](#project-structure)

## 🎯 Overview

This Document Management System (DMS) is a full-stack application designed to manage documents with features like metadata tracking, tagging, and asynchronous OCR processing. The system follows clean architecture principles with clear separation of concerns across multiple layers.

### Key Highlights
- **Microservices Architecture**: Separate services for API and OCR processing
- **Asynchronous Communication**: RabbitMQ message queue for document processing
- **Comprehensive Logging**: NLog implementation across all layers
- **Extensive Testing**: 31+ unit tests with mocking framework
- **Modern Tech Stack**: .NET 8, React 18, PostgreSQL, RabbitMQ
- **Containerized Deployment**: Docker Compose for easy setup

## 🏗️ Architecture

### System Architecture

```
┌─────────────┐     ┌──────────────┐     ┌─────────────┐
│   React UI  │────▶│  REST API    │────▶│  PostgreSQL │
│  (Port 80)  │     │  (Port 8081) │     │  (Port 5432)│
└─────────────┘     └──────┬───────┘     └─────────────┘
                           │
                           │ Publish
                           ▼
                    ┌─────────────┐
                    │  RabbitMQ   │
                    │ (Port 5672) │
                    └──────┬──────┘
                           │ Consume
                           ▼
                    ┌─────────────┐
                    │ OCR Worker  │
                    └─────────────┘
```

### Layer Architecture

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │
│  - DocumentManagementSystem.REST        │
│  - DocumentManagementSystem.UI          │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│         Application Layer               │
│  - DocumentManagementSystem.Application │
│  - Business Logic & Services            │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│         Data Access Layer               │
│  - DocumentManagementSystem.DAL         │
│  - Repository Pattern & EF Core         │
└────────────────┬────────────────────────┘
                 │
┌────────────────▼────────────────────────┐
│         Domain Layer                    │
│  - DocumentManagementSystem.Model       │
│  - Entities & DTOs                      │
└─────────────────────────────────────────┘

┌─────────────────────────────────────────┐
│    Cross-Cutting Concerns               │
│  - DocumentManagementSystem.Messaging   │
│  - DocumentManagementSystem.OcrWorker   │
└─────────────────────────────────────────┘
```

## ✨ Features

### Implemented Features
- ✅ **CRUD Operations**: Create, Read, Update, Delete documents
- ✅ **Document Upload**: Multi-part form file upload with metadata
- ✅ **Tag Management**: Associate multiple tags with documents
- ✅ **Metadata Tracking**: Automatic tracking of file size, content type, timestamps
- ✅ **Asynchronous Processing**: RabbitMQ queue for OCR worker communication
- ✅ **RESTful API**: Complete REST API with Swagger documentation
- ✅ **Modern UI**: React-based responsive user interface
- ✅ **Database Management**: PostgreSQL with EF Core and migrations
- ✅ **Comprehensive Logging**: Structured logging with NLog
- ✅ **Unit Testing**: 31+ tests covering controllers, services, repositories
- ✅ **Validation**: Input validation across all layers
- ✅ **Exception Handling**: Consistent error handling throughout

## 🛠️ Technologies

### Backend
- **.NET 8** - Framework
- **ASP.NET Core** - Web API
- **Entity Framework Core 9** - ORM
- **PostgreSQL** - Database
- **RabbitMQ** - Message Queue
- **AutoMapper** - Object mapping
- **NLog** - Logging framework

### Frontend
- **React 18** - UI Framework
- **TypeScript** - Type safety
- **Vite** - Build tool
- **Axios** - HTTP client

### Testing
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **AutoFixture** - Test data generation
- **EF Core InMemory** - In-memory database for tests

### DevOps
- **Docker & Docker Compose** - Containerization
- **GitHub Actions** - CI/CD
- **pgAdmin** - Database management UI

## 🚀 Getting Started

### Prerequisites
- Docker Desktop
- (Optional) .NET 8 SDK for local development
- (Optional) Node.js 18+ for UI development

### Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd swen3-document-management-system
   ```

2. **Create environment file** (optional, defaults provided)
   ```bash
   cp .env.example .env
   ```

3. **Start all services**
   ```bash
   docker-compose up --build
   ```

4. **Access the application**
   - UI: http://localhost:80
   - API: http://localhost:8081
   - Swagger: http://localhost:8081/swagger
   - RabbitMQ Management: http://localhost:9093
   - pgAdmin: http://localhost:9091

### Environment Configuration

Create a `.env` file in the root directory:

```dotenv
# ASP.NET Core
ASPNETCORE_ENVIRONMENT=Development

# PostgreSQL
POSTGRES_USER=dms
POSTGRES_PASSWORD=dms_pw
POSTGRES_DB=dms
POSTGRES_PORT=5432

# API
API_PORT=8081

# UI
UI_PORT=80

# RabbitMQ
RABBITMQ_PORT=5672
RABBITMQ_UI_PORT=9093

# pgAdmin
PGADMIN_PORT=9091
PGADMIN_DEFAULT_EMAIL=admin@example.com
PGADMIN_DEFAULT_PASSWORD=admin_pw
```

## 📚 API Documentation

### Base URL
```
http://localhost:8081/api
```

### Endpoints

#### Documents

**Get All Documents**
```http
GET /api/document
```

**Get Document by ID**
```http
GET /api/document/{id}
```

**Create Document**
```http
POST /api/document
Content-Type: application/json

{
  "fileName": "example.pdf",
  "fileSize": 1024,
  "contentType": "application/pdf",
  "tags": ["important", "archive"]
}
```

**Update Document**
```http
PUT /api/document/{id}
Content-Type: application/json

{
  "fileName": "updated.pdf",
  "fileSize": 2048,
  "contentType": "application/pdf",
  "tags": ["updated"]
}
```

**Upload File**
```http
POST /api/document/upload
Content-Type: multipart/form-data

file: [binary]
tags: "tag1,tag2,tag3"
```

**Delete Document**
```http
DELETE /api/document/{id}
```

### Response Format

**Success Response**
```json
{
  "id": "0199c27e-2c31-7b6e-9c32-629e45249cb8",
  "fileName": "example.pdf",
  "metadata": {
    "id": "0199c27e-2c4d-7907-bc70-648aba5083d8",
    "createdAt": "2025-10-08T06:24:32.779Z",
    "updatedAt": null,
    "fileSize": 1024,
    "contentType": "application/pdf",
    "storagePath": null,
    "ocrText": null,
    "summary": null
  },
  "tags": ["important", "archive"]
}
```

**Error Response**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "traceId": "00-..."
}
```

## 🧪 Testing

### Running Unit Tests

**All Tests**
```bash
dotnet test
```

**Specific Project**
```bash
dotnet test DocumentManagementSystem.Application.Tests/
dotnet test DocumentManagementSystem.DAL.Tests/
```

**With Coverage**
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage

| Component | Tests | Coverage |
|-----------|-------|----------|
| DocumentController | 13 tests | Controllers, validation, error handling |
| DocumentService | 7 tests | Business logic, CRUD operations |
| DocumentRepository | 5 tests | Database operations, EF Core |
| MessagePublisherService | 3 tests | RabbitMQ publishing |
| MessageConsumerService | 3 tests | RabbitMQ consuming |
| **Total** | **31 tests** | All critical paths |

### Test Structure
```
Tests/
├── Controllers/
│   └── DocumentControllerTests.cs
├── Services/
│   └── DocumentServiceTests.cs
├── Repositories/
│   └── DocumentRepositoryTests.cs
└── Messaging/
    ├── MessagePublisherServiceTests.cs
    └── MessageConsumerServiceTests.cs
```

## 📝 Logging

### NLog Configuration

Logging is implemented across all layers using **NLog** with structured logging.

**Log Levels Used:**
- `DEBUG` - Detailed diagnostic information
- `INFO` - General informational messages
- `WARN` - Warning messages for non-critical issues
- `ERROR` - Error messages for failures

**Log Files:**
- REST API: `logs/rest-all-{date}.log`, `logs/rest-own-{date}.log`
- OCR Worker: `logs/worker-all-{date}.log`, `logs/worker-own-{date}.log`

**Console Output:**
- Color-coded log levels
- Timestamps and structured parameters
- Real-time monitoring during development

### Viewing Logs

**Docker Logs:**
```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f api
docker-compose logs -f ocrworker
```

**Log Files (inside container):**
```bash
docker exec -it dms_api cat logs/rest-own-$(date +%Y-%m-%d).log
```

## 📁 Project Structure

```
swen3-document-management-system/
├── DocumentManagementSystem.REST/          # REST API
│   ├── Controllers/                        # API Controllers
│   ├── Program.cs                          # Application entry point
│   ├── appsettings.json                   # Configuration
│   ├── nlog.config                        # Logging configuration
│   └── Dockerfile                         # Container definition
│
├── DocumentManagementSystem.Application/   # Business Logic Layer
│   ├── Services/                          # Business services
│   │   ├── DocumentService.cs
│   │   └── Interfaces/
│   └── Mapper/                            # AutoMapper profiles
│
├── DocumentManagementSystem.DAL/          # Data Access Layer
│   ├── Repositories/                      # Repository pattern
│   │   ├── DocumentRepository.cs
│   │   └── Interfaces/
│   └── DocumentManagementSystemContext.cs # EF Core DbContext
│
├── DocumentManagementSystem.Model/        # Domain Layer
│   ├── ORM/                              # Entity models
│   │   ├── Document.cs
│   │   ├── DocumentMetadata.cs
│   │   └── Tag.cs
│   └── DTO/                              # Data Transfer Objects
│       ├── DocumentDTO.cs
│       └── DocumentCreateDTO.cs
│
├── DocumentManagementSystem.Messaging/    # Message Queue
│   ├── MessagePublisherService.cs
│   ├── MessageConsumerService.cs
│   └── Model/
│       └── RabbitMQOptions.cs
│
├── DocumentManagementSystem.OcrWorker/    # OCR Worker Service
│   ├── Services/
│   │   └── OcrWorkerService.cs
│   ├── Program.cs
│   ├── nlog.config
│   └── Dockerfile
│
├── DocumentManagementSystem.UI/           # React Frontend
│   ├── src/
│   │   ├── components/
│   │   ├── services/
│   │   └── types/
│   ├── Dockerfile
│   └── nginx.conf
│
├── DocumentManagementSystem.Application.Tests/  # Unit Tests
├── DocumentManagementSystem.DAL.Tests/          # DAL Tests
│
├── docker-compose.yml                     # Container orchestration
├── .env                                   # Environment variables
└── README.md                              # This file
```

## 🏛️ Design Patterns & Principles

### SOLID Principles
- **Single Responsibility**: Each class has one responsibility
- **Open/Closed**: Open for extension, closed for modification
- **Liskov Substitution**: Interfaces used throughout
- **Interface Segregation**: Focused, specific interfaces
- **Dependency Inversion**: Depend on abstractions, not concretions

### Design Patterns
- **Repository Pattern**: Data access abstraction
- **Dependency Injection**: ASP.NET Core DI container
- **DTO Pattern**: Data transfer objects for API
- **Factory Pattern**: Service creation (MessagePublisherService)
- **Template Method**: MessageConsumerService abstract class
- **Facade Pattern**: Service layer abstracts complexity

### Database Migrations

```bash
# Add migration
dotnet ef migrations add MigrationName --project DocumentManagementSystem.DAL

# Update database
dotnet ef database update --project DocumentManagementSystem.DAL
```

## 📊 Monitoring

### RabbitMQ Management UI
- URL: http://localhost:9093
- Username: `guest`
- Password: `guest`
- Monitor queues, messages, connections

### pgAdmin
- URL: http://localhost:9091
- Configure connection to PostgreSQL container
- View tables, execute queries, manage database

