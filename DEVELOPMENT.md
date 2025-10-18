# PandoDeploy - Guía de Desarrollo

## Estructura del Proyecto

```
PandoDeploy/
├── src/
│   ├── PandoDeploy.Shared/      # Modelos compartidos entre CLI y Server
│   ├── PandoDeploy.Core/        # Lógica de negocio y servicios compartidos
│   ├── PandoDeploy.Server/      # API Server ASP.NET Core
│   └── PandoDeploy.CLI/         # Herramienta CLI global
├── tests/
│   ├── PandoDeploy.Server.Tests/
│   ├── PandoDeploy.CLI.Tests/
│   └── PandoDeploy.Integration.Tests/
├── scripts/                     # Scripts de utilidad
├── examples/                    # Ejemplos de configuración
└── .github/workflows/           # CI/CD con GitHub Actions
```

## Requisitos de Desarrollo

- .NET 8.0 SDK
- Docker Desktop (para testing)
- Visual Studio 2022 / VS Code / Rider
- Git

## Setup Inicial

```bash
# Clonar el repositorio
git clone https://github.com/MathiasGonzalez/PandoDeploy.git
cd PandoDeploy

# Restaurar dependencias
dotnet restore

# Compilar
dotnet build

# Ejecutar tests
dotnet test
```

## Desarrollo del Servidor

### Ejecutar en modo desarrollo

```bash
cd src/PandoDeploy.Server
dotnet run

# O con hot reload
dotnet watch run
```

El servidor estará disponible en `http://localhost:5000`

### Swagger UI

En modo desarrollo, accede a `http://localhost:5000/swagger` para ver la documentación de la API.

### Configuración de desarrollo

Edita `src/PandoDeploy.Server/appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug"
    }
  },
  "PandoDeploy": {
    "ImageStoragePath": "./data/images",
    "DatabasePath": "./data/pandodeploy.db",
    "Authentication": {
      "EnableApiKeys": true,
      "EnableMTLS": false
    }
  }
}
```

## Desarrollo del CLI

### Ejecutar CLI en desarrollo

```bash
cd src/PandoDeploy.CLI
dotnet run -- deploy --image myapp:latest --server http://localhost:5000
```

### Instalar versión local como global tool

```bash
# Build y pack
./scripts/build-pack.sh 1.0.0-dev

# Desinstalar versión anterior
dotnet tool uninstall -g PandoDeploy

# Instalar versión local
dotnet tool install -g PandoDeploy --add-source ./nupkgs --version 1.0.0-dev

# Usar
pandodeploy --version
```

## Testing

### Unit Tests

```bash
# Ejecutar todos los tests
dotnet test

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Solo tests de un proyecto
dotnet test tests/PandoDeploy.Server.Tests/
```

### Integration Tests

Los tests de integración requieren Docker:

```bash
# Asegurar que Docker está corriendo
docker ps

# Ejecutar integration tests
dotnet test tests/PandoDeploy.Integration.Tests/
```

### Testing Manual

1. Iniciar el servidor:
   ```bash
   ./scripts/run-server.sh
   ```

2. Crear una API key:
   ```bash
   ./scripts/create-apikey.sh
   ```

3. Build una imagen Docker de prueba:
   ```bash
   cd tests/TestApp
   docker build -t testapp:latest .
   ```

4. Deployar con el CLI:
   ```bash
   pandodeploy deploy --image testapp:latest --server http://localhost:5000 --api-key YOUR_KEY --port 8080
   ```

## Arquitectura

### Flujo de Deployment

1. **CLI**: Usuario ejecuta comando `deploy`
2. **ImagePackerService**: Exporta imagen de Docker local a tar.gz
3. **DeploymentClient**: Envía stream + metadata al servidor vía HTTP
4. **DeploymentController**: Recibe request y delega a DeploymentService
5. **DeploymentService**: 
   - Guarda imagen en filesystem
   - Carga imagen en Docker del servidor
   - Crea y arranca contenedor
   - Registra deployment en base de datos
6. **Response**: Retorna resultado al CLI

### Componentes Principales

#### PandoDeploy.Shared

Modelos compartidos:
- `DeploymentRequest`: Configuración del deployment
- `DeploymentResponse`: Resultado del deployment
- `DeploymentInfo`: Información de deployments existentes
- `ServerStatusResponse`: Estado del servidor

#### PandoDeploy.Core

Servicios de negocio:
- `IDockerService`: Interacción con Docker API
- `IImagePackerService`: Empaquetado/desempaquetado de imágenes
- `IImageStorageService`: Almacenamiento de imágenes

#### PandoDeploy.Server

- **Controllers**: Endpoints REST
  - `DeploymentController`: CRUD de deployments
  - `StatusController`: Health check y status
  
- **Services**: Lógica de negocio
  - `DeploymentService`: Orquestación del deployment
  
- **Authentication**: Autenticación y autorización
  - `ApiKeyAuthenticationHandler`: Validación de API keys
  
- **Data**: Capa de datos
  - `PandoDeployContext`: EF Core context
  - Entities: Deployment, ApiKey, ClientCertificate

#### PandoDeploy.CLI

- **Commands**: Comandos del CLI
  - `DeployCommand`: Deploy de imágenes
  - `StatusCommand`: Ver estado del servidor
  - `ApiKeyCommand`: Gestión de API keys
  
- **Services**: Servicios del CLI
  - `DeploymentClient`: Cliente HTTP para comunicación con servidor

## Agregar Nueva Funcionalidad

### Ejemplo: Agregar soporte para rollback

1. **Agregar modelo en Shared**:
   ```csharp
   // src/PandoDeploy.Shared/Models/RollbackRequest.cs
   public class RollbackRequest
   {
       public int DeploymentId { get; set; }
   }
   ```

2. **Implementar servicio en Server**:
   ```csharp
   // src/PandoDeploy.Server/Services/DeploymentService.cs
   public async Task<DeploymentResponse> RollbackAsync(int deploymentId)
   {
       // TODO: Implementación
   }
   ```

3. **Agregar endpoint en Controller**:
   ```csharp
   // src/PandoDeploy.Server/Controllers/DeploymentController.cs
   [HttpPost("rollback")]
   public async Task<ActionResult<DeploymentResponse>> Rollback([FromBody] RollbackRequest request)
   {
       var result = await _deploymentService.RollbackAsync(request.DeploymentId);
       return Ok(result);
   }
   ```

4. **Agregar comando en CLI**:
   ```csharp
   // src/PandoDeploy.CLI/Commands/RollbackCommand.cs
   public static class RollbackCommand
   {
       public static Command Create(IServiceProvider serviceProvider)
       {
           // TODO: Implementación
       }
   }
   ```

5. **Agregar tests**:
   ```csharp
   // tests/PandoDeploy.Server.Tests/Services/DeploymentServiceTests.cs
   [Fact]
   public async Task RollbackAsync_ShouldRestorePreviousVersion()
   {
       // TODO: Implementación
   }
   ```

## Base de Datos

### Migraciones con EF Core

```bash
# Agregar migración
cd src/PandoDeploy.Server
dotnet ef migrations add MigrationName

# Aplicar migraciones
dotnet ef database update

# Revertir migración
dotnet ef database update PreviousMigrationName

# Generar script SQL
dotnet ef migrations script
```

### Consultas Directas

```bash
# Abrir base de datos
sqlite3 ./data/pandodeploy.db

# Ver esquema
.schema

# Consultar deployments
SELECT * FROM Deployments;
```

## Debugging

### Visual Studio / Rider

1. Abrir `PandoDeploy.sln`
2. Configurar múltiples proyectos de inicio:
   - PandoDeploy.Server
   - PandoDeploy.CLI (con argumentos)
3. F5 para iniciar debugging

### VS Code

Archivo `.vscode/launch.json`:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Debug Server",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/PandoDeploy.Server/bin/Debug/net9.0/PandoDeploy.Server.dll",
      "cwd": "${workspaceFolder}/src/PandoDeploy.Server",
      "env": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    {
      "name": "Debug CLI",
      "type": "coreclr",
      "request": "launch",
      "preLaunchTask": "build",
      "program": "${workspaceFolder}/src/PandoDeploy.CLI/bin/Debug/net9.0/pandodeploy.dll",
      "args": ["deploy", "--image", "testapp:latest", "--server", "http://localhost:5000"],
      "cwd": "${workspaceFolder}/src/PandoDeploy.CLI"
    }
  ]
}
```

## Code Style

### Principios

- **DRY** (Don't Repeat Yourself): Evitar duplicación de código
- **YAGNI** (You Aren't Gonna Need It): No agregar funcionalidad innecesaria
- **SOLID**: Principios de diseño orientado a objetos
- **Clean Code**: Código legible y mantenible

### Convenciones

- Usar `async`/`await` para operaciones I/O
- Inyección de dependencias para todos los servicios
- Logging estructurado con Serilog
- Manejo de errores con try-catch y logging apropiado
- XML comments para APIs públicas

### Linting

```bash
# Formatear código
dotnet format

# Analizar código
dotnet build /p:EnforceCodeStyleInBuild=true
```

## Publicación

### Publicar a NuGet

1. Actualizar versión en `Directory.Build.props`
2. Crear tag de versión:
   ```bash
   git tag v1.0.0
   git push origin v1.0.0
   ```
3. GitHub Actions automáticamente publicará a NuGet

### Publicar manualmente

```bash
# Build y pack
dotnet pack src/PandoDeploy.CLI/PandoDeploy.CLI.csproj -c Release -o ./nupkgs

# Publicar
dotnet nuget push ./nupkgs/PandoDeploy.1.0.0.nupkg --api-key YOUR_API_KEY --source https://api.nuget.org/v3/index.json
```

## Contribuir

1. Fork el repositorio
2. Crear branch de feature: `git checkout -b feature/nueva-funcionalidad`
3. Commit cambios: `git commit -am 'Agregar nueva funcionalidad'`
4. Push al branch: `git push origin feature/nueva-funcionalidad`
5. Crear Pull Request

### Checklist para PRs

- [ ] Tests pasan localmente
- [ ] Nuevo código tiene tests
- [ ] Documentación actualizada
- [ ] Sin warnings de compilación
- [ ] Código formateado con `dotnet format`

## Recursos

- [Docker.DotNet Documentation](https://github.com/dotnet/Docker.DotNet)
- [System.CommandLine](https://github.com/dotnet/command-line-api)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)

## TODOs y Mejoras Futuras

### Corto Plazo
- [ ] Implementar manejo completo de mTLS en CLI
- [ ] Agregar endpoint para crear API keys desde CLI
- [ ] Implementar comando `server` para instalar como servicio
- [ ] Agregar más tests de integración

### Mediano Plazo
- [ ] Soporte para múltiples estrategias de deployment (blue-green, canary)
- [ ] Dashboard web para monitorear deployments
- [ ] Notificaciones (webhook, email) de deployment
- [ ] Métricas y monitoreo con Prometheus

### Largo Plazo
- [ ] Soporte para Kubernetes
- [ ] CLI en otros lenguajes (Python, Go)
- [ ] Clustering y alta disponibilidad
- [ ] Plugin system para extensibilidad

