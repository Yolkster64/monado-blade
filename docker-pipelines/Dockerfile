# Hermes Fleet Deployable Image
FROM mcr.microsoft.com/windows/servercore:ltsc2022

# Install Python
SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]
RUN Invoke-WebRequest -Uri https://www.python.org/ftp/python/3.10.0/python-3.10.0-amd64.exe -OutFile python-installer.exe; Start-Process -Wait -FilePath .\python-installer.exe -ArgumentList '/quiet InstallAllUsers=1 PrependPath=1'; Remove-Item python-installer.exe

# Install .NET Desktop Runtime
RUN winget install --id Microsoft.DotNet.DesktopRuntime.6 -e --accept-package-agreements --accept-source-agreements

# Install SQL Server Express
RUN Invoke-WebRequest -Uri https://go.microsoft.com/fwlink/?linkid=866658 -OutFile sqlexpress.exe; Start-Process -Wait -FilePath .\sqlexpress.exe -ArgumentList '/Q /IACCEPTSQLSERVERLICENSETERMS'; Remove-Item sqlexpress.exe

# Copy project files
COPY . C:/app

# Set up entrypoint (customize as needed)
CMD ["powershell.exe", "-NoLogo", "-ExecutionPolicy", "Bypass", "-File", "C:/app/docker-pipelines/dev-env-setup.ps1"]
