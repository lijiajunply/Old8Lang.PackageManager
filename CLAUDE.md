# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Old8Lang Package Manager (o8pm) is a NuGet-inspired package management system for the Old8Lang programming language. The system includes:
- CLI tool for package management operations
- ASP.NET Core REST API server with SQLite database
- Vue 3 frontend with TypeScript and Naive UI
- Multi-language package support (Old8Lang, Python, JavaScript/TypeScript)

## Build & Development Commands

### Backend (.NET)

```bash
# Build entire solution
dotnet build Old8Lang.PackageManager.sln

# Run CLI tool
dotnet run --project Old8Lang.PackageManager -- [command] [args]

# Run API server (default port: 5000)
dotnet run --project Old8Lang.PackageManager.Server

# Run all tests
dotnet test

# Run specific test class
dotnet test --filter "FullyQualifiedName~PythonPackageParserTests"
dotnet test --filter "FullyQualifiedName~SimpleMultiLanguageTests"

# Format code
dotnet format Old8Lang.PackageManager.sln

# Apply database migrations
cd Old8Lang.PackageManager.Server
dotnet ef database update
```

### Frontend (Vue 3 + TypeScript)

```bash
cd frontend

# Install dependencies
npm install

# Development server (default port: 5173)
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Lint JavaScript/TypeScript
npm run lint

# Lint CSS
npm run lint:style
```

### Docker Compose

```bash
# Start all services (database, redis, backend, frontend)
docker-compose up

# Rebuild and start
docker-compose up --build

# Stop services
docker-compose down

# View logs
docker-compose logs -f backend
```

## Architecture

### Project Structure

The solution consists of 4 .NET projects targeting .NET 10.0:

1. **Old8Lang.PackageManager.Core** - Core library containing:
   - `Interfaces/`: Core abstractions (IPackageSource, IPackageInstaller, IPackageResolver, IPackageConfigurationManager)
   - `Models/`: Data models (Package, PackageConfiguration)
   - `Services/`: Core implementations
     - `DefaultPackageInstaller` - Package installation/uninstallation
     - `DefaultPackageResolver` - Dependency resolution with backtracking algorithm
     - `DefaultPackageConfigurationManager` - Manages o8packages.json files
     - `PackageSourceManager` - Multi-source package management
     - `PackageRestorer` - Batch dependency restoration
     - `VersionManager` - Semantic versioning and compatibility checking
     - `LocalPackageSource` - Local filesystem package source
   - `Commands/`: CLI command implementations
   - `Versioning/`: Version range parsing and comparison
   - `Resolution/`: Dependency resolution algorithms

2. **Old8Lang.PackageManager** - CLI executable
   - Simple command dispatcher in Program.cs
   - Commands: add, remove, restore, search

3. **Old8Lang.PackageManager.Server** - ASP.NET Core Web API
   - `Controllers/`:
     - `PackagesController` - Main package CRUD and search API
     - `PyPIController` - PyPI compatibility layer
     - `NpmController` - NPM compatibility layer
     - `AdminController` - Administrative functions
   - `Services/`:
     - `PackageManagementService` - Package CRUD operations
     - `PackageStorageService` - File storage on disk
     - `PythonPackageParser` - Parse Python package metadata
     - `JavaScriptPackageParser` - Parse npm package.json
     - `PackageSecurityService` - Signature verification and integrity checks
   - `Data/PackageManagerDbContext` - EF Core context with SQLite
   - `Configuration/` - Options classes for dependency injection
   - API version: 3.0.0, uses Swagger/OpenAPI

4. **Old8Lang.PackageManager.Tests** - Test project
   - Uses xUnit, FluentAssertions, Moq
   - Contains tests for Python parser, multi-language support, package existence checks

### Package Format

Packages use `.o8pkg` format (ZIP archives) containing:
```
MyPackage.1.0.0.o8pkg
├── package.json          # Metadata (id, version, dependencies, frameworks)
├── lib/                  # Compiled libraries per framework
│   └── old8lang-1.0/
└── docs/                 # Documentation
```

Package metadata fields: id, version, description, author, license, homepage, repository, keywords, dependencies, frameworks, publishedAt, checksum, size

### Configuration Files

- **o8packages.json** - Project-level package configuration
  - Version, ProjectName, Framework, InstallPath
  - Sources[] - Array of package sources (local/http)
  - References[] - Installed packages with version constraints

- **appsettings.json** (Server) - API configuration
  - PackageStorage: StoragePath, MaxPackageSize
  - Security: RequireApiKey, EnableSignatureValidation

### Frontend Architecture

Vue 3 application with TypeScript:
- `src/api/` - Axios-based API client
- `src/components/` - Reusable UI components
- `src/views/` - Page-level components
- `src/stores/` - Pinia state management
- `src/router/` - Vue Router configuration
- `src/types/` - TypeScript type definitions
- Uses Naive UI component library, Tailwind CSS

## Key Technical Details

### Dependency Resolution

The resolver uses a backtracking algorithm:
1. Recursively collect all dependencies
2. Detect version conflicts and circular dependencies
3. Select newest version satisfying all constraints
4. Build dependency graph

Supported version ranges: `1.0.0`, `1.0.*`, `>=1.0.0`, `<=2.0.0`, `>1.0.0 <2.0.0`, `~1.0.0`, `^1.0.0`

### Multi-Language Support

The system supports multiple package ecosystems:
- **Old8Lang**: Native .o8pkg format
- **Python**: Parses wheel files and PyPI metadata
- **JavaScript/TypeScript**: Parses package.json and npm tarballs

Language-specific parsers implement parsing logic in `PackageManagementService`.

### Package Storage

Packages are stored on disk with structure:
```
{StoragePath}/
  {packageId}/
    {version}/
      package.o8pkg
      metadata.json
```

### API Endpoints

Main endpoints (prefix: `/api/v3`):
- `GET /packages/search?q={term}` - Search packages
- `GET /packages/{id}` - Get package metadata
- `GET /packages/{id}/{version}` - Get specific version
- `POST /packages` - Upload new package
- `DELETE /packages/{id}/{version}` - Delete package
- `GET /health` - Health check

PyPI compatibility: `/api/pypi/*`
NPM compatibility: `/api/npm/*`

## Testing

Test coverage: 69/69 core tests passing
- Python package parsing: 25/25
- Multi-language support: 21/21
- Package existence checks: 3/3
- Basic package management: 20/20

The test project uses in-memory databases for integration tests.

## Development Notes

- The codebase uses .NET 10.0 features
- Nullable reference types are enabled throughout
- The API uses minimal APIs style with controller-based endpoints
- Database migrations are auto-applied on startup in Program.cs
- CORS is configured to allow all origins (development mode)
- Rate limiting is commented out but implemented
- The frontend expects API at `http://localhost:5000/api` by default
