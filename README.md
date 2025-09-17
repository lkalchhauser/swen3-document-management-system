# Document Management System for SWEN3
[![.NET CI](https://github.com/lkalchhauser/swen3-document-management-system/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/lkalchhauser/swen3-document-management-system/actions/workflows/dotnet-ci.yml)

## Setup
While the `docker-compose.yml` does contain defaults, you should create a `.env` environment file in the root directory.

### Example Environment File
```
ASPNETCORE_ENVIRONMENT=Development

POSTGRES_USER=dmstest
POSTGRES_PASSWORD=dms_pw_test
POSTGRES_DB=dms_test
POSTGRES_PORT=5432

API_PORT=8081

PGADMIN_PORT=9091
PGADMIN_DEFAULT_EMAIL=max@mustermann.at
PGADMIN_DEFAULT_PASSWORD=admin```