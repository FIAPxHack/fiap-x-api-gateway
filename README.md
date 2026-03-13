# FIAP X - API Gateway

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![YARP](https://img.shields.io/badge/YARP-2.0-5C2D91?logo=microsoft&logoColor=white)](https://microsoft.github.io/reverse-proxy/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Gateway centralizado para roteamento, autenticação e autorização do sistema FIAP X, construído com **ASP.NET Core 9.0** e **YARP (Yet Another Reverse Proxy)**.

---

## 📋 Índice

- [Sobre o Projeto](#-sobre-o-projeto)
- [Arquitetura](#-arquitetura)
- [Tecnologias](#-tecnologias)
- [Pré-requisitos](#-pré-requisitos)
- [Instalação e Execução](#-instalação-e-execução)
- [Rotas e Endpoints](#-rotas-e-endpoints)
- [Autenticação e Autorização](#-autenticação-e-autorização)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Variáveis de Ambiente](#-variáveis-de-ambiente)
- [Microsserviços Integrados](#-microsserviços-integrados)
- [Deployment](#-deployment)
- [Contribuição](#-contribuição)
- [Licença](#-licença)

---

## 🎯 Sobre o Projeto

O **fiap-x-api-gateway** é o ponto de entrada único para todos os microsserviços do sistema FIAP X. Ele atua como uma camada de abstração que centraliza:

### Funcionalidades Principais

- ✅ **Roteamento Inteligente** - Distribui requisições para os microsserviços corretos
- ✅ **Autenticação JWT** - Validação centralizada de tokens de acesso
- ✅ **Autorização por Roles** - Controle de acesso baseado em perfis (ADMIN, USER)
- ✅ **Claims Transformation** - Extração e validação de claims JWT
- ✅ **Proxy Reverso** - Encaminhamento transparente de requisições/respostas
- ✅ **CORS Configurável** - Suporte para aplicações frontend
- ✅ **Health Checks** - Monitoramento de saúde do gateway

---

## 🏗️ Arquitetura

O API Gateway segue o padrão **API Gateway Pattern** e atua como único ponto de entrada para a arquitetura de microsserviços:

```
┌───────────────┐
│    Cliente    │
│  (Web/Mobile) │
└───────┬───────┘
        │
        │ HTTPS
        ▼
┌─────────────────────────────────────┐
│         API Gateway :8080           │
│  ┌─────────────────────────────┐   │
│  │  Autenticação JWT           │   │
│  │  Autorização (Roles)        │   │
│  │  Roteamento (YARP)          │   │
│  └─────────────────────────────┘   │
└───┬──────┬──────┬──────┬───────────┘
    │      │      │      │
    ▼      ▼      ▼      ▼
┌────────┬────────┬────────┬──────────┐
│  User  │  Auth  │ Video  │  Notif.  │
│ :8081  │ :8082  │ :5003  │  :5004   │
└────────┴────────┴────────┴──────────┘
```

### Fluxo de Requisição

1. Cliente envia requisição para o Gateway (`:8080`)
2. Gateway valida token JWT (se necessário)
3. Gateway verifica autorização baseada em roles
4. Gateway encaminha para o microsserviço correspondente
5. Microsserviço processa e retorna resposta
6. Gateway repassa resposta diretamente ao cliente

---

## 🚀 Tecnologias

### Core
- **[.NET 9.0](https://dotnet.microsoft.com/)** - Runtime e Framework
- **[ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)** - Framework Web
- **[C# 12.0](https://docs.microsoft.com/en-us/dotnet/csharp/)** - Linguagem

### Proxy e Roteamento
- **[YARP 2.0](https://microsoft.github.io/reverse-proxy/)** - Yet Another Reverse Proxy (Microsoft)
- **Route Configuration** - Configuração declarativa de rotas

### Segurança
- **[JWT Bearer Authentication](https://jwt.io/)** - Validação de tokens
- **ASP.NET Core Authorization** - Policies e roles-based access control
- **Claims Transformation** - Extração de user ID e roles

### DevOps
- **[Docker](https://www.docker.com/)** - Containerização
- **Environment Variables** - Configuração 12-factor

---

## 📦 Pré-requisitos

Antes de começar, você vai precisar ter instalado:

- **[.NET SDK 9.0+](https://dotnet.microsoft.com/download)** - Para compilar e executar
- **[Docker](https://www.docker.com/get-started)** (opcional) - Para containerização
- **[Git](https://git-scm.com/)** - Para clonar o repositório

E ter os microsserviços backend rodando:
- User Service (porta 8081)
- Auth Service (porta 8082)
- Video Processing Service (porta 5003)
- Notification Service (porta 5004)

---

## 🔧 Instalação e Execução

### 1️⃣ Clonar o Repositório

```bash
git clone https://github.com/FIAPxHack/fiap-x-api-gateway.git
cd fiap-x-api-gateway
```

### 2️⃣ Configurar Variáveis de Ambiente

Crie um arquivo `.env` ou configure as variáveis no sistema:

```bash
# JWT Configuration
JWT_SECRET=your-256-bit-secret-key-change-in-production-min-32-chars
JWT_ISSUER=fiap-x
JWT_AUDIENCE=fiap-x-api

# Microservices URLs
USER_SERVICE_URL=http://localhost:8081
AUTH_SERVICE_URL=http://localhost:8082
VIDEO_PROCESSING_SERVICE_URL=http://localhost:5003
NOTIFICATION_SERVICE_URL=http://localhost:5004
```

### 3️⃣ Executar com .NET CLI

```bash
# Restaurar dependências
dotnet restore

# Executar em modo desenvolvimento
cd src
dotnet run

# Ou executar a solution
dotnet run --project src/Gateway.csproj
```

O Gateway estará disponível em: **http://localhost:8080**

### 4️⃣ Executar com Docker

```bash
# Build da imagem
docker build -t fiap-x-gateway .

# Executar container
docker run -d -p 8080:8080 \
  -e JWT_SECRET=your-secret \
  -e JWT_ISSUER=fiap-x \
  -e JWT_AUDIENCE=fiap-x-api \
  -e USER_SERVICE_URL=http://user-service:8081 \
  -e AUTH_SERVICE_URL=http://auth-service:8082 \
  -e VIDEO_PROCESSING_SERVICE_URL=http://video-service:5003 \
  -e NOTIFICATION_SERVICE_URL=http://notification-service:5004 \
  --name gateway \
  fiap-x-gateway
```

### 5️⃣ Verificar Saúde

```bash
# Health check
curl http://localhost:8080/healthz

# Resposta esperada: HTTP 200 OK
```

---

## 📡 Rotas e Endpoints

### Roteamento para Microsserviços

O Gateway roteia requisições baseado no prefixo `/api/{service}`:

| Prefixo               | Microsserviço           | Porta | Autenticação |
|-----------------------|-------------------------|-------|--------------|
| `/api/auth/**`        | Auth Service            | 8082  | Não (login)  |
| `/api/users/**`       | User Service            | 8081  | Sim          |
| `/api/videos/**`      | Video Processing        | 5003  | Sim          |
| `/api/notifications/**` | Notification Service  | 5004  | Sim          |

### Exemplos de Rotas

#### Autenticação (Público - sem token)
```bash
# Login
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "senha123"
}
```

#### Usuários
```bash
# Criar usuário (público)
POST http://localhost:8080/api/users
Content-Type: application/json

{
  "name": "João Silva",
  "email": "joao@example.com",
  "password": "senha123"
}

# Listar usuários (autenticado)
GET http://localhost:8080/api/users
Authorization: Bearer {token}

# Buscar por ID (autenticado)
GET http://localhost:8080/api/users/{id}
Authorization: Bearer {token}

# Atualizar usuário (autenticado - ID no body)
PUT http://localhost:8080/api/users
Authorization: Bearer {token}
Content-Type: application/json

{
  "id": "uuid-do-usuario",
  "name": "João Silva",
  "email": "joao@example.com"
}

# Deletar usuário (autenticado)
DELETE http://localhost:8080/api/users/{id}
Authorization: Bearer {token}
```

#### Vídeos (Autenticado)
```bash
# Upload de vídeo
POST http://localhost:8080/api/videos/upload
Authorization: Bearer {token}
Content-Type: multipart/form-data

# Status do processamento
GET http://localhost:8080/api/videos/{id}/status
Authorization: Bearer {token}

# Download do ZIP
GET http://localhost:8080/api/videos/download/{filename}
Authorization: Bearer {token}
```

#### Notificações (Autenticado)
```bash
# Enviar notificação
POST http://localhost:8080/api/notifications/send
Authorization: Bearer {token}

# Histórico de notificações
GET http://localhost:8080/api/notifications/user/{userId}
Authorization: Bearer {token}
```

---

## 🔐 Autenticação e Autorização

### JWT Token

O Gateway valida tokens JWT em todas as rotas protegidas:

```bash
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Claims esperadas:**
- `sub` - User ID
- `email` - Email do usuário
- `role` - Role do usuário (ADMIN, USER)

### Roles e Policies

| Role    | Código | Descrição              | Acesso                          |
|---------|--------|------------------------|---------------------------------|
| ADMIN   | 1      | Administrador          | Todas as rotas                  |
| USER    | 2      | Usuário padrão         | Rotas de usuário autenticado    |

| Policy              | Descrição                          | Roles Permitidas |
|---------------------|------------------------------------|------------------|
| ADMIN_ONLY          | Apenas administradores             | ADMIN            |
| AUTHENTICATED_USER  | Qualquer usuário autenticado       | ADMIN, USER      |

### Claims Transformation

O Gateway automaticamente:
- Extrai `userId` do claim `sub`
- Extrai `role` do claim `role`
- Valida se o usuário tem permissão para a rota

---

## 🗂️ Estrutura do Projeto

```
fiap-x-api-gateway/
├── src/
│   ├── Configurations/
│   │   ├── JwtConfiguration.cs           # Config JWT
│   │   └── RouteConfiguration.cs         # Config de serviços
│   ├── Constants/
│   │   └── RolesAndPolicies.cs          # Roles e Policies
│   ├── Extensions/
│   │   ├── AuthExtension.cs             # Configuração de autenticação
│   │   └── ReverseProxyExtension.cs     # Configuração YARP
│   ├── appsettings.json                  # Config principal
│   ├── appsettings.Development.json      # Config dev
│   ├── Program.cs                        # Entry point
│   └── Gateway.csproj                    # Projeto
├── tests/                                # Testes (a implementar)
├── Dockerfile                            # Containerização
├── .dockerignore
├── .gitignore
├── fiap-x-api-gateway.sln               # Solution
└── README.md                            # Este arquivo
```

---

## ⚙️ Variáveis de Ambiente

### Obrigatórias

```bash
# JWT
JWT_SECRET=<secret-key>                  # Chave secreta (min 32 chars)
JWT_ISSUER=fiap-x                        # Emissor do token
JWT_AUDIENCE=fiap-x-api                  # Audiência do token

# Microsserviços
USER_SERVICE_URL=http://localhost:8081
AUTH_SERVICE_URL=http://localhost:8082
VIDEO_PROCESSING_SERVICE_URL=http://localhost:5003
NOTIFICATION_SERVICE_URL=http://localhost:5004
```

### Opcionais

```bash
# Logging
ASPNETCORE_ENVIRONMENT=Development       # Development | Production
LOG_LEVEL=Information                    # Trace | Debug | Information | Warning | Error

# CORS (se necessário)
CORS_ORIGINS=http://localhost:3000,https://app.fiapx.com
```

---

## 🔗 Microsserviços Integrados

### 1. User Service (porta 8081)
- **Tecnologia**: Kotlin + Spring Boot
- **Responsável**: Vitória
- **Função**: CRUD de usuários
- **Repositório**: [fiap-x-microsservice-user](https://github.com/FIAPxHack/fiap-x-microsservice-user)

### 2. Auth Service (porta 8082)
- **Tecnologia**: Kotlin + Spring Boot
- **Responsável**: Vitória
- **Função**: Login, registro, geração de tokens JWT
- **Repositório**: [fiap-x-microsservice-auth](https://github.com/FIAPxHack/fiap-x-microsservice-auth)

### 3. Video Processing Service (porta 5003)
- **Tecnologia**: Go + FFmpeg
- **Responsável**: Marcelle
- **Função**: Upload, processamento de vídeo, geração de frames
- **Repositório**: (a definir)

### 4. Notification Service (porta 5004)
- **Tecnologia**: .NET 9.0
- **Responsável**: Elen
- **Função**: Envio de notificações por e-mail
- **Repositório**: [fiap-x-microservice-notification](https://github.com/FIAPxHack/fiap-x-microservice-notification)

---

## 🐳 Deployment

### Docker Compose (Recomendado)

Exemplo de `docker-compose.yml` completo:

```yaml
version: '3.8'

services:
  gateway:
    build: ./fiap-x-api-gateway
    ports:
      - "8080:8080"
    environment:
      JWT_SECRET: ${JWT_SECRET}
      JWT_ISSUER: fiap-x
      JWT_AUDIENCE: fiap-x-api
      USER_SERVICE_URL: http://user-service:8081
      AUTH_SERVICE_URL: http://auth-service:8082
      VIDEO_PROCESSING_SERVICE_URL: http://video-service:5003
      NOTIFICATION_SERVICE_URL: http://notification-service:5004
    depends_on:
      - user-service
      - auth-service
      - video-service
      - notification-service
    networks:
      - fiapx-network

  user-service:
    image: fiap-x-user:latest
    ports:
      - "8081:8081"
    networks:
      - fiapx-network

  auth-service:
    image: fiap-x-auth:latest
    ports:
      - "8082:8082"
    networks:
      - fiapx-network

  video-service:
    image: fiap-x-video:latest
    ports:
      - "5003:5003"
    networks:
      - fiapx-network

  notification-service:
    image: fiap-x-notification:latest
    ports:
      - "5004:5004"
    networks:
      - fiapx-network

networks:
  fiapx-network:
    driver: bridge
```

---

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma branch para sua feature (`git checkout -b feature/nova-feature`)
3. Commit suas mudanças (`git commit -m 'feat: adiciona nova feature'`)
4. Push para a branch (`git push origin feature/nova-feature`)
5. Abra um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo [LICENSE](LICENSE) para mais detalhes.

---

## 👥 Equipe

- **Marcelle** - Video Processing Service
- **Vitória** - User Service, Auth Service
- **Elen** - API Gateway, Notification Service

---

## 📞 Suporte

Para questões e suporte, abra uma [issue](https://github.com/FIAPxHack/fiap-x-api-gateway/issues) no repositório
