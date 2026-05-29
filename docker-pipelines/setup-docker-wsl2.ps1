# Docker + WSL2 Setup Script
# Ensure WSL2 is installed
wsl --install

# Install Docker Desktop
winget install --id Docker.DockerDesktop

# Start Docker Desktop
Start-Process 'C:\Program Files\Docker\Docker\Docker Desktop.exe'

# Validate Docker installation
docker --version
wsl --status

# Add more validation and troubleshooting as needed

