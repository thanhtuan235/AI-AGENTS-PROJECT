FROM mcr.microsoft.com/mssql/server:2025-CU11-ubuntu-24.04

USER root
RUN apt-get update && \
    apt-get install -y --no-install-recommends curl gnupg ca-certificates && \
    curl -fsSL https://packages.microsoft.com/keys/microsoft.asc \
            | gpg --batch --yes --dearmor -o /usr/share/keyrings/microsoft-prod.gpg && \
    cp /usr/share/keyrings/microsoft-prod.gpg /etc/apt/trusted.gpg.d/microsoft.gpg && \
    curl -fsSL https://packages.microsoft.com/config/ubuntu/24.04/mssql-server-2025.list \
      -o /etc/apt/sources.list.d/mssql-server-2025.list && \
    apt-get update && \
    apt-get install -y mssql-server-fts && \
    apt-get clean && rm -rf /var/lib/apt/lists/*
USER mssql