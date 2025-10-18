# PandoDeploy

🚀 **Docker deployment without registry dependency**

PandoDeploy es una herramienta que permite deployar imágenes Docker directamente desde CI/CD a tu VPS sin necesidad de un container registry (Docker Hub, ghcr.io, etc.). Funciona transfiriendo las imágenes empaquetadas directamente del origen al servidor de destino.

## ✨ Características

- ✅ **Sin Registry**: Despliega imágenes directamente sin Docker Hub, ghcr.io, etc.
- 🔐 **Seguridad**: Autenticación con API Keys y/o mTLS
- 💾 **Base de Datos**: SQLite para tracking de deployments
- 🛠️ **CLI Global**: Instalable como herramienta global de .NET
- 🔄 **CI/CD Ready**: Integración lista para GitHub Actions, GitLab CI, etc.
- 📦 **Compresión**: Las imágenes se comprimen automáticamente para transferencia eficiente
- 🐳 **Docker Native**: Usa Docker.DotNet para interacción directa con Docker

## 🏗️ Arquitectura

```
┌──────────────┐         ┌──────────────┐         ┌──────────────┐
│  GitHub      │         │  PandoDeploy │         │  Docker      │
│  Actions     │────────▶│     CLI      │────────▶│  Local       │
└──────────────┘         └──────────────┘         └──────────────┘
                               │                          
                               │ Empaqueta y comprime     
                               │                          
                               ▼                          
                         ┌──────────────┐                
                         │   HTTP POST  │                
                         │   (gzipped)  │                
                         └──────────────┘                
                               │                          
                               ▼                          
                         ┌──────────────┐                
                         │ PandoDeploy  │                
                         │   Server     │                
                         │   (VPS)      │                
                         └──────────────┘                
                               │                          
                               │ Carga y ejecuta          
                               ▼                          
                         ┌──────────────┐                
                         │   Docker     │                
                         │   en VPS     │                
                         └──────────────┘                
```

## 🚀 Quick Start

### Instalación del CLI

```bash
# Instalar desde NuGet (cuando esté publicado)
dotnet tool install -g PandoDeploy

# O build local
git clone https://github.com/MathiasGonzalez/PandoDeploy.git
cd PandoDeploy
./scripts/build-pack.sh
dotnet tool install -g PandoDeploy --add-source ./nupkgs
```

### Servidor en VPS

```bash
# Ubuntu/Debian
sudo apt install -y dotnet-runtime-8.0 docker.io
git clone https://github.com/MathiasGonzalez/PandoDeploy.git
cd PandoDeploy

# Ejecutar servidor
./scripts/run-server.sh

# O instalar como servicio systemd
sudo cp examples/systemd-service-example.service /etc/systemd/system/pandodeploy.service
sudo systemctl enable --now pandodeploy
```

### Crear API Key

```bash
./scripts/create-apikey.sh
export PANDODEPLOY_API_KEY=your-generated-key
```

### Deployar

```bash
# Build tu imagen
docker build -t myapp:latest .

# Deploy al servidor
pandodeploy deploy \
  --image myapp:latest \
  --server http://your-vps:5000 \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080
```

## 📖 Documentación

- [QUICKSTART.md](QUICKSTART.md) - Guía rápida de inicio
- [USAGE.md](USAGE.md) - Guía de uso detallada
- [DEVELOPMENT.md](DEVELOPMENT.md) - Guía para desarrolladores
- [examples/](examples/) - Ejemplos de configuración

## 🔧 Uso en CI/CD

### GitHub Actions

```yaml
- name: Deploy with PandoDeploy
  run: |
    dotnet tool install -g PandoDeploy
    pandodeploy deploy \
      --image ${{ github.repository }}:${{ github.sha }} \
      --server ${{ secrets.PANDODEPLOY_SERVER }} \
      --api-key ${{ secrets.PANDODEPLOY_API_KEY }} \
      --port 8080
```

Ver [examples/github-actions-example.yml](examples/github-actions-example.yml) para un ejemplo completo.

## 🛡️ Seguridad

### API Keys

```bash
# Crear API key
./scripts/create-apikey.sh /var/lib/pandodeploy/pandodeploy.db my-key-name

# Usar en deployment
pandodeploy deploy --api-key YOUR_KEY ...
```

### mTLS (Mutual TLS)

```bash
# Generar certificados
openssl req -x509 -newkey rsa:4096 -keyout server-key.pem -out server-cert.pem -days 365 -nodes
openssl req -newkey rsa:4096 -keyout client-key.pem -out client.csr -nodes
openssl x509 -req -in client.csr -CA server-cert.pem -CAkey server-key.pem -out client-cert.pem -days 365

# Deployar con mTLS
pandodeploy deploy \
  --image myapp:latest \
  --server https://your-vps:5000 \
  --cert client-cert.pem \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080
```

## 📦 Estructura del Proyecto

```
PandoDeploy/
├── src/
│   ├── PandoDeploy.Shared/       # Modelos compartidos
│   ├── PandoDeploy.Core/         # Servicios de negocio
│   ├── PandoDeploy.Server/       # API Server (ASP.NET Core)
│   └── PandoDeploy.CLI/          # CLI Tool (System.CommandLine)
├── tests/
│   ├── PandoDeploy.Server.Tests/
│   ├── PandoDeploy.CLI.Tests/
│   └── PandoDeploy.Integration.Tests/
├── scripts/                      # Scripts de utilidad
└── examples/                     # Ejemplos de configuración
```

## 🔨 Comandos Disponibles

### Deploy
```bash
pandodeploy deploy --image myapp:latest --server http://vps:5000 --port 8080
```

### Status
```bash
pandodeploy status --server http://vps:5000 --list
```

### API Keys (TODO)
```bash
pandodeploy apikey create --name my-key
```

## 🧪 Testing

```bash
# Unit tests
dotnet test

# Con cobertura
dotnet test --collect:"XPlat Code Coverage"

# Integration tests (requiere Docker)
dotnet test tests/PandoDeploy.Integration.Tests/
```

## 🛠️ Tecnologías

- **.NET 8.0**: Framework principal
- **ASP.NET Core**: API Server
- **Entity Framework Core**: ORM con SQLite
- **Docker.DotNet**: Interacción con Docker API
- **System.CommandLine**: CLI framework
- **Serilog**: Logging estructurado
- **xUnit**: Testing framework

## 📝 TODOs

Ver [DEVELOPMENT.md](DEVELOPMENT.md) para la lista completa de mejoras planificadas.

### Implementados ✅

- [x] Estructura base del proyecto
- [x] Servidor ASP.NET Core con endpoints
- [x] CLI con comandos deploy y status
- [x] Autenticación con API Keys
- [x] Base de datos SQLite
- [x] Servicios de Docker
- [x] Empaquetado y compresión de imágenes
- [x] Tests básicos
- [x] Scripts de utilidad
- [x] GitHub Actions para CI/CD

### Pendientes 🚧

- [ ] Implementación completa de mTLS en CLI
- [ ] Endpoint para crear API keys desde CLI
- [ ] Comando para instalar servidor como servicio
- [ ] Dashboard web para monitoreo
- [ ] Soporte para rollback
- [ ] Notificaciones de deployment
  
### A Evaluar 🤔

- [ ] Admin UI web
- [ ] Integración con herramientas de monitoreo (Prometheus, Grafana)
- [ ] Mejora en la gestión de secretos
- [ ] Optimización de imágenes Docker

## 🤝 Contribuir

Las contribuciones son bienvenidas! Por favor:

1. Fork el proyecto
2. Crea tu feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit tus cambios (`git commit -m 'Add some AmazingFeature'`)
4. Push al branch (`git push origin feature/AmazingFeature`)
5. Abre un Pull Request

Ver [DEVELOPMENT.md](DEVELOPMENT.md) para guía de desarrollo.

## 📄 Licencia

Este proyecto está bajo la licencia MIT. Ver [LICENSE](LICENSE) para más detalles.

## 👥 Autores

- Mathías González Benítez - [GitHub](https://github.com/MathiasGonzalez)

## 🙏 Agradecimientos

- Docker.DotNet por la excelente biblioteca
- .NET Team por las herramientas de CLI
- Comunidad open source

## 📮 Contacto

- Issues: https://github.com/MathiasGonzalez/PandoDeploy/issues
- Discussions: https://github.com/MathiasGonzalez/PandoDeploy/discussions

---

**¿Por qué PandoDeploy?**

PandoDeploy nació de la necesidad de desplegar aplicaciones rápidamente sin la complejidad y costos de mantener un container registry. Es ideal para:

- 🏢 Equipos pequeños que quieren simplicidad
- 💰 Proyectos con presupuesto limitado
- 🚀 Deployments rápidos desde CI/CD
- 🔒 Organizaciones que necesitan control total de sus imágenes
- 🌐 Escenarios donde el registry no es accesible

**Hecho con ❤️ para la comunidad .NET y DevOps**
