# PandoDeploy - Quick Start Guide

## Installation

### Install as Global Tool

```bash
dotnet tool install -g PandoDeploy
```

## Server Setup

### Option 1: Run Locally (Development)

```bash
# Clone the repository
git clone https://github.com/MathiasGonzalez/PandoDeploy.git
cd PandoDeploy

# Run the server
./scripts/run-server.sh  # Linux/Mac
# or
.\scripts\run-server.ps1  # Windows
```

### Option 2: Deploy to VPS (Production)

```bash
# On your VPS (Ubuntu example)
sudo apt update
sudo apt install -y dotnet-sdk-8.0 docker.io

# Clone and build
git clone https://github.com/MathiasGonzalez/PandoDeploy.git
cd PandoDeploy
dotnet build -c Release

# Create service user
sudo useradd -r -s /bin/false -G docker pandodeploy

# Copy files
sudo mkdir -p /opt/pandodeploy
sudo cp -r src/PandoDeploy.Server/bin/Release/net9.0/* /opt/pandodeploy/

# Create data directory
sudo mkdir -p /var/lib/pandodeploy/images
sudo chown -R pandodeploy:docker /var/lib/pandodeploy

# Install systemd service
sudo cp examples/systemd-service-example.service /etc/systemd/system/pandodeploy.service
sudo systemctl daemon-reload
sudo systemctl enable pandodeploy
sudo systemctl start pandodeploy

# Check status
sudo systemctl status pandodeploy
```

## Create API Key

```bash
# Run the script to create an API key
./scripts/create-apikey.sh  # Linux/Mac
# or
.\scripts\create-apikey.ps1  # Windows

# Export the API key
export PANDODEPLOY_API_KEY=your-api-key-here
```

## Deploy an Image

### From Local Docker Image

```bash
# Build your Docker image
docker build -t myapp:latest .

# Deploy to server
pandodeploy deploy \
  --image myapp:latest \
  --server http://your-vps:5000 \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080 \
  --name myapp-production
```

### From GitHub Actions

Add to your `.github/workflows/deploy.yml`:

```yaml
- name: Install PandoDeploy
  run: dotnet tool install -g PandoDeploy

- name: Deploy
  run: |
    pandodeploy deploy \
      --image ${{ github.repository }}:${{ github.sha }} \
      --server ${{ secrets.PANDODEPLOY_SERVER }} \
      --api-key ${{ secrets.PANDODEPLOY_API_KEY }} \
      --port 8080
```

## Check Status

```bash
pandodeploy status --server http://your-vps:5000 --api-key $PANDODEPLOY_API_KEY
```

## List Deployments

```bash
pandodeploy status --server http://your-vps:5000 --api-key $PANDODEPLOY_API_KEY --list
```

## Advanced Usage

### With Environment Variables

```bash
pandodeploy deploy \
  --image myapp:latest \
  --server http://your-vps:5000 \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080 \
  --env "ASPNETCORE_ENVIRONMENT=Production" \
  --env "DATABASE_URL=postgres://..." \
  --name myapp-prod
```

### With Volume Mounts

```bash
pandodeploy deploy \
  --image myapp:latest \
  --server http://your-vps:5000 \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080 \
  --volume "/host/data:/app/data" \
  --volume "/host/logs:/app/logs"
```

### With Labels

```bash
pandodeploy deploy \
  --image myapp:latest \
  --server http://your-vps:5000 \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080 \
  --label "app=myapp" \
  --label "environment=production"
```

## Security: Enable mTLS

### Generate Certificates

```bash
# Server certificate
openssl req -x509 -newkey rsa:4096 -keyout server-key.pem -out server-cert.pem -days 365 -nodes

# Client certificate
openssl req -newkey rsa:4096 -keyout client-key.pem -out client.csr -nodes
openssl x509 -req -in client.csr -CA server-cert.pem -CAkey server-key.pem -out client-cert.pem -days 365 -CAcreateserial
```

### Configure Server

Update `appsettings.json`:

```json
{
  "PandoDeploy": {
    "Authentication": {
      "EnableMTLS": true,
      "EnableApiKeys": true
    }
  }
}
```

### Deploy with mTLS

```bash
pandodeploy deploy \
  --image myapp:latest \
  --server https://your-vps:5000 \
  --cert client-cert.pem \
  --cert-password "your-password" \
  --api-key $PANDODEPLOY_API_KEY \
  --port 8080
```

## Troubleshooting

### Check server logs

```bash
# If running with systemd
sudo journalctl -u pandodeploy -f

# If running directly
tail -f logs/pandodeploy-*.log
```

### Test connection

```bash
curl http://your-vps:5000/api/status
```

### Check Docker

```bash
docker ps  # See running containers
docker images  # See available images
```

## Next Steps

- See [README.md](README.md) for full documentation
- Check [examples/](examples/) for more configuration examples
- Report issues at https://github.com/MathiasGonzalez/PandoDeploy/issues

