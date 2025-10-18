# PandoDeploy - Guía de Uso Detallada

## Arquitectura

PandoDeploy es una herramienta que permite deployar imágenes Docker sin necesidad de un registry. Consta de dos componentes principales:

1. **Servidor (PandoDeploy.Server)**: Se instala en el VPS y expone un endpoint HTTP/HTTPS
2. **CLI (PandoDeploy.CLI)**: Se instala como herramienta global de .NET y se usa desde CI/CD o localmente

## Flujo de Trabajo

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│ GitHub      │         │ PandoDeploy │         │   Docker    │
│ Actions     │─(1)────▶│    CLI      │─(2)────▶│   Local     │
└─────────────┘         └─────────────┘         └─────────────┘
                              │
                              │ (3) Empaqueta imagen
                              ▼
                        ┌─────────────┐
                        │   Compress  │
                        │   + Stream  │
                        └─────────────┘
                              │
                              │ (4) HTTP POST
                              ▼
                        ┌─────────────┐
                        │ PandoDeploy │
                        │   Server    │
                        │   (VPS)     │
                        └─────────────┘
                              │
                              │ (5) Descomprime y carga
                              ▼
                        ┌─────────────┐
                        │   Docker    │
                        │   en VPS    │
                        └─────────────┘
```

## Instalación del Servidor

### Requisitos Previos
- Ubuntu/Debian VPS (o Windows Server)
- Docker instalado
- .NET 8.0 Runtime instalado

### Instalación en Ubuntu

```bash
# 1. Instalar dependencias
sudo apt update
sudo apt install -y dotnet-runtime-8.0 docker.io

# 2. Clonar o copiar archivos del servidor
sudo mkdir -p /opt/pandodeploy
sudo cp -r src/PandoDeploy.Server/bin/Release/net9.0/* /opt/pandodeploy/

# 3. Crear usuario del servicio
sudo useradd -r -s /bin/false -G docker pandodeploy

# 4. Crear directorios de datos
sudo mkdir -p /var/lib/pandodeploy/images
sudo chown -R pandodeploy:docker /var/lib/pandodeploy

# 5. Configurar el servicio systemd
sudo nano /etc/systemd/system/pandodeploy.service
```

Contenido del servicio:
```ini
[Unit]
Description=PandoDeploy Server
After=network.target docker.service
Requires=docker.service

[Service]
Type=notify
User=pandodeploy
Group=docker
WorkingDirectory=/opt/pandodeploy
ExecStart=/usr/bin/dotnet /opt/pandodeploy/PandoDeploy.Server.dll
Restart=always
RestartSec=10

Environment="ASPNETCORE_URLS=http://+:5000"
Environment="PandoDeploy__Port=5000"
Environment="PandoDeploy__DockerEndpoint=unix:///var/run/docker.sock"
Environment="PandoDeploy__ImageStoragePath=/var/lib/pandodeploy/images"
Environment="PandoDeploy__DatabasePath=/var/lib/pandodeploy/pandodeploy.db"

[Install]
WantedBy=multi-user.target
```

```bash
# 6. Habilitar e iniciar el servicio
sudo systemctl daemon-reload
sudo systemctl enable pandodeploy
sudo systemctl start pandodeploy

# 7. Verificar el estado
sudo systemctl status pandodeploy
```

## Configuración de API Keys

### Crear API Key

```bash
# Usando el script proporcionado
./scripts/create-apikey.sh /var/lib/pandodeploy/pandodeploy.db my-api-key

# O manualmente con SQLite
sqlite3 /var/lib/pandodeploy/pandodeploy.db
INSERT INTO ApiKeys (Key, Name, IsActive, CreatedAt) 
VALUES ('your-generated-key-here', 'production', 1, datetime('now'));
.exit
```

### Usar API Key

```bash
# Como variable de entorno
export PANDODEPLOY_API_KEY=your-api-key-here

# O directamente en el comando
pandodeploy deploy --api-key your-api-key-here ...
```

## Uso del CLI

### Instalación del CLI

```bash
# Instalar desde NuGet (cuando esté publicado)
dotnet tool install -g PandoDeploy

# O instalar desde fuente local
./scripts/build-pack.sh
dotnet tool install -g PandoDeploy --add-source ./nupkgs
```

### Comandos Disponibles

#### 1. Deploy

Despliega una imagen Docker al servidor:

```bash
pandodeploy deploy \
  --image myapp:latest \
  --server http://your-vps:5000 \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080
```

**Opciones:**
- `--image`: Nombre y tag de la imagen (requerido)
- `--server`: URL del servidor PandoDeploy
- `--api-key`: API key para autenticación
- `--port`: Puerto a exponer
- `--name`: Nombre del contenedor
- `--env`: Variables de entorno (repetible)
- `--label`: Etiquetas del contenedor (repetible)
- `--volume`: Montajes de volúmenes (repetible)
- `--cert`: Certificado para mTLS
- `--cert-password`: Contraseña del certificado

**Ejemplo completo:**

```bash
pandodeploy deploy \
  --image mywebapp:v1.2.3 \
  --server https://vps.example.com:5000 \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080 \
  --name mywebapp-prod \
  --env "ASPNETCORE_ENVIRONMENT=Production" \
  --env "DATABASE_URL=postgres://..." \
  --label "app=mywebapp" \
  --label "version=1.2.3" \
  --volume "/host/data:/app/data" \
  --volume "/host/logs:/app/logs"
```

#### 2. Status

Obtiene el estado del servidor y deployments:

```bash
# Estado básico del servidor
pandodeploy status --server http://your-vps:5000

# Con lista de deployments
pandodeploy status --server http://your-vps:5000 --list --limit 20
```

#### 3. API Key Management

```bash
# Crear nueva API key (TODO: implementar endpoint)
pandodeploy apikey create --name my-key --server http://your-vps:5000
```

## Integración con CI/CD

### GitHub Actions

Archivo `.github/workflows/deploy.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]
    tags:
      - 'v*'

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v4
    
    - name: Set up Docker Buildx
      uses: docker/setup-buildx-action@v3
    
    - name: Build Docker image
      run: |
        docker build -t myapp:${{ github.sha }} .
    
    - name: Install PandoDeploy
      run: dotnet tool install -g PandoDeploy
    
    - name: Deploy to VPS
      env:
        PANDODEPLOY_API_KEY: ${{ secrets.PANDODEPLOY_API_KEY }}
      run: |
        pandodeploy deploy \
          --image myapp:${{ github.sha }} \
          --server ${{ secrets.PANDODEPLOY_SERVER }} \
          --port 8080 \
          --name myapp-${{ github.run_number }} \
          --env "VERSION=${{ github.sha }}" \
          --env "BUILD_NUMBER=${{ github.run_number }}"
    
    - name: Verify deployment
      env:
        PANDODEPLOY_API_KEY: ${{ secrets.PANDODEPLOY_API_KEY }}
      run: |
        pandodeploy status \
          --server ${{ secrets.PANDODEPLOY_SERVER }} \
          --list
```

**Secrets requeridos:**
- `PANDODEPLOY_API_KEY`: La API key generada en el servidor
- `PANDODEPLOY_SERVER`: URL del servidor (ej: `http://123.456.789.0:5000`)

### GitLab CI

Archivo `.gitlab-ci.yml`:

```yaml
stages:
  - build
  - deploy

variables:
  DOCKER_IMAGE: myapp:$CI_COMMIT_SHA

build:
  stage: build
  script:
    - docker build -t $DOCKER_IMAGE .

deploy:
  stage: deploy
  script:
    - dotnet tool install -g PandoDeploy
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - pandodeploy deploy
        --image $DOCKER_IMAGE
        --server $PANDODEPLOY_SERVER
        --api-key $PANDODEPLOY_API_KEY
        --port 8080
  only:
    - main
```

## Seguridad: mTLS

### Generación de Certificados

```bash
# 1. Certificado del servidor (CA)
openssl req -x509 -newkey rsa:4096 \
  -keyout server-key.pem \
  -out server-cert.pem \
  -days 365 -nodes \
  -subj "/CN=pandodeploy-server"

# 2. Certificado del cliente
openssl req -newkey rsa:4096 \
  -keyout client-key.pem \
  -out client.csr -nodes \
  -subj "/CN=pandodeploy-client"

# 3. Firmar certificado del cliente
openssl x509 -req -in client.csr \
  -CA server-cert.pem \
  -CAkey server-key.pem \
  -out client-cert.pem \
  -days 365 -CAcreateserial

# 4. Crear PFX para el cliente (opcional)
openssl pkcs12 -export \
  -out client-cert.pfx \
  -inkey client-key.pem \
  -in client-cert.pem
```

### Configurar Servidor con mTLS

```json
{
  "PandoDeploy": {
    "Authentication": {
      "EnableMTLS": true,
      "EnableApiKeys": true,
      "RequireClientCertificate": true
    }
  },
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://+:5000",
        "Certificate": {
          "Path": "/etc/pandodeploy/certs/server-cert.pfx",
          "Password": "your-password"
        },
        "ClientCertificateMode": "RequireCertificate"
      }
    }
  }
}
```

### Usar CLI con mTLS

```bash
pandodeploy deploy \
  --image myapp:latest \
  --server https://your-vps:5000 \
  --cert /path/to/client-cert.pem \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080
```

## Base de Datos SQLite

### Estructura

La base de datos SQLite contiene las siguientes tablas:

1. **Deployments**: Registro de todos los deployments
2. **ApiKeys**: API keys para autenticación
3. **ClientCertificates**: Certificados autorizados para mTLS

### Backup

```bash
# Backup simple
cp /var/lib/pandodeploy/pandodeploy.db /backup/pandodeploy-$(date +%Y%m%d).db

# Backup con SQLite
sqlite3 /var/lib/pandodeploy/pandodeploy.db ".backup /backup/pandodeploy.db"
```

### Consultas Útiles

```sql
-- Ver todos los deployments
SELECT * FROM Deployments ORDER BY CreatedAt DESC LIMIT 10;

-- Ver API keys activas
SELECT * FROM ApiKeys WHERE IsActive = 1;

-- Ver deployments activos
SELECT * FROM Deployments WHERE Status = 'Running';

-- Estadísticas
SELECT Status, COUNT(*) as Count 
FROM Deployments 
GROUP BY Status;
```

## Troubleshooting

### El servidor no inicia

```bash
# Verificar logs
sudo journalctl -u pandodeploy -n 50 --no-pager

# Verificar que Docker esté corriendo
sudo systemctl status docker

# Verificar permisos
ls -la /var/lib/pandodeploy
```

### Error al deployar

```bash
# Ver logs del servidor
sudo journalctl -u pandodeploy -f

# Verificar conectividad
curl http://your-vps:5000/api/status

# Verificar autenticación
curl -H "X-API-Key: your-key" http://your-vps:5000/api/deployment
```

### Imagen no se carga

```bash
# Verificar espacio en disco
df -h

# Verificar límite de tamaño en configuración
cat /opt/pandodeploy/appsettings.json | grep MaxImageSize

# Verificar Docker
docker images
docker ps -a
```

## Mejores Prácticas

1. **Seguridad**:
   - Siempre usa HTTPS en producción
   - Habilita mTLS para mayor seguridad
   - Rota las API keys periódicamente
   - Usa firewall para limitar acceso al puerto del servidor

2. **Rendimiento**:
   - Limpia imágenes antiguas periódicamente
   - Monitorea el espacio en disco
   - Configura límites de recursos para contenedores

3. **Monitoreo**:
   - Revisa logs regularmente
   - Configura alertas para fallos de deployment
   - Monitorea el estado del servidor

4. **Backup**:
   - Realiza backups de la base de datos
   - Guarda las API keys de forma segura
   - Documenta la configuración del servidor

