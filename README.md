# Enterprise Multi-Tenant Organization Management System

A full-stack enterprise application for hierarchical organization management with role-based access control (RBAC).

## Tech Stack

### Backend
- .NET 8 / ASP.NET Core Web API
- Clean Architecture with CQRS (MediatR)
- Entity Framework Core 8
- SQL Server
- JWT Authentication with Refresh Tokens
- FluentValidation

### Frontend
- Angular 17 (Standalone Components)
- Angular Material
- TypeScript
- Reactive Forms

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 18+](https://nodejs.org/)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (or Docker)

## Project Structure

```
├── backend/
│   └── src/
│       ├── OrgManagement.Domain/           # Entities, Enums, Interfaces
│       ├── OrgManagement.Application/      # CQRS Commands/Queries, Validators
│       ├── OrgManagement.Infrastructure/   # EF Core, Services, Authorization
│       └── OrgManagement.WebApi/           # Controllers, Middleware
├── frontend/
│   └── src/app/
│       ├── core/                           # Auth, Guards, Interceptors
│       ├── layout/                         # Main Layout, Header, Sidebar
│       └── features/                       # Feature Modules
│           ├── auth/                       # Login, Forgot Password
│           ├── dashboard/                  # Dashboard
│           ├── organizations/              # Organization Management
│           ├── users/                      # User Management
│           ├── roles/                      # Role Management
│           └── audit/                      # Audit Logs
└── docker-compose.yml
```

## Getting Started

### 1. Database Setup

**Option A: Using Docker**
```bash
docker-compose up -d sqlserver
```

**Option B: Local SQL Server**

Update the connection string in `backend/src/OrgManagement.WebApi/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=OrgManagement;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

### 2. Run the Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/OrgManagement.WebApi
```

The API will be available at `https://localhost:5001` (or `http://localhost:5000`).

Swagger UI: `https://localhost:5001/swagger`

### 3. Run the Frontend

```bash
cd frontend
npm install
ng serve
```

The application will be available at `http://localhost:4200`.

## Default Credentials

On first run, the database is seeded with:

| Email | Password | Role |
|-------|----------|------|
| `superadmin@system.local` | `Password1,` | Super Admin |

**Note:** You will be required to change the password on first login.

## Features

### Organization Management
- Create and manage organizations
- Hierarchical sub-organizations (up to 5 levels)
- Materialized path pattern for efficient hierarchy queries
- Cascade deactivation (org → sub-orgs → users)

### User Management
- Create users with organization/sub-organization assignment
- Role assignment
- Account status management (Active, Inactive, Locked)
- Password reset functionality
- Account lockout after 5 failed attempts

### Role-Based Access Control
- 5 pre-configured system roles:
  - **Super Admin** - Full system access
  - **Organization Admin** - Full access within assigned organization
  - **Sub-Organization Manager** - Manage assigned sub-organization
  - **User Manager** - Create and manage users
  - **Viewer** - Read-only access
- Custom role creation with granular permissions
- Permission categories: Organizations, Users, Roles, Audit Logs, Dashboard, Settings

### Audit Logging
- Tracks all administrative actions
- Filter by entity type, action, date range
- View detailed change history

### Security
- JWT authentication with refresh token rotation
- BCrypt password hashing
- Policy-based authorization
- CORS configuration for Angular frontend

## API Endpoints

### Authentication
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/login` | Login |
| POST | `/api/auth/refresh` | Refresh access token |
| POST | `/api/auth/logout` | Logout |
| POST | `/api/auth/forgot-password` | Request password reset |
| POST | `/api/auth/reset-password` | Reset password |
| POST | `/api/auth/change-password` | Change password |

### Organizations
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/organizations` | List organizations (paginated) |
| GET | `/api/organizations/{id}` | Get organization details |
| POST | `/api/organizations` | Create organization |
| PUT | `/api/organizations/{id}` | Update organization |
| POST | `/api/organizations/{id}/deactivate` | Deactivate organization |
| POST | `/api/organizations/{id}/reactivate` | Reactivate organization |

### Users
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/users` | List users (paginated, filterable) |
| GET | `/api/users/{id}` | Get user details |
| POST | `/api/users` | Create user |
| PUT | `/api/users/{id}` | Update user |
| POST | `/api/users/{id}/deactivate` | Deactivate user |
| POST | `/api/users/{id}/activate` | Activate user |
| POST | `/api/users/{id}/unlock` | Unlock user account |

### Roles
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/roles` | List roles |
| GET | `/api/roles/{id}` | Get role details |
| POST | `/api/roles` | Create role |
| PUT | `/api/roles/{id}` | Update role |
| DELETE | `/api/roles/{id}` | Delete role |
| GET | `/api/permissions` | List all permissions |

### Dashboard & Audit
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/dashboard` | Get dashboard metrics |
| GET | `/api/audit-logs` | List audit logs (paginated, filterable) |

## Environment Configuration

### Backend (`appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=OrgManagement;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
  },
  "JwtSettings": {
    "SecretKey": "your-256-bit-secret-key-here-minimum-32-chars",
    "Issuer": "OrgManagement",
    "Audience": "OrgManagement",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

### Frontend (`environment.ts`)
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:5001/api'
};
```

## Docker Deployment

Build and run the entire stack:
```bash
docker-compose up --build
```

Services:
- **API**: http://localhost:5000
- **Frontend**: http://localhost:4200
- **SQL Server**: localhost:1433

## Development

### Backend Commands
```bash
# Build
dotnet build

# Run with hot reload
dotnet watch run --project src/OrgManagement.WebApi

# Run tests
dotnet test
```

### Frontend Commands
```bash
# Development server
ng serve

# Build for production
ng build --configuration production

# Run tests
ng test

# Lint
ng lint
```

## License

MIT
