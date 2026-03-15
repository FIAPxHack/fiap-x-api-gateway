# FIAP X - API Gateway

[![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![C#](https://img.shields.io/badge/C%23-12.0-239120?logo=csharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![YARP](https://img.shields.io/badge/YARP-2.0-5C2D91?logo=microsoft&logoColor=white)](https://microsoft.github.io/reverse-proxy/)
[![Coverage](https://img.shields.io/badge/Coverage-100%25-brightgreen)](./CoverageReport/index.html)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> **Gateway centralizado para orquestração, autenticação e roteamento de microsserviços da plataforma FIAP X**  
> Desenvolvido com ASP.NET Core 9.0 e YARP (Yet Another Reverse Proxy)

---

## 📋 Sumário

- [Visão Geral](#-visão-geral)
- [Arquitetura e Padrões](#-arquitetura-e-padrões)
- [Stack Tecnológico](#-stack-tecnológico)
- [Funcionalidades](#-funcionalidades)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Configuração e Execução](#-configuração-e-execução)
- [Rotas e Endpoints](#-rotas-e-endpoints)
- [Segurança](#-segurança)
- [Testes e Qualidade](#-testes-e-qualidade)
- [Deployment](#-deployment)
- [Decisões Arquiteturais](#-decisões-arquiteturais)

---

## 🎯 Visão Geral

O **FIAP X API Gateway** é o componente central de entrada da arquitetura de microsserviços da plataforma FIAP X. Implementa o padrão **API Gateway Pattern**, atuando como ponto único de acesso (single entry point) para todos os serviços backend, consolidando responsabilidades transversais como autenticação, autorização, roteamento e observabilidade.

### Contexto do Sistema

A plataforma FIAP X é um sistema de gerenciamento e processamento de vídeos educacionais que permite upload, processamento assíncrono, geração de frames e notificações aos usuários. O API Gateway integra quatro microsserviços independentes:

- **User Service** - Gerenciamento de usuários
- **Auth Service** - Autenticação e geração de tokens JWT
- **Video Manager Service** - Orquestração de upload e processamento de vídeos
- **Notification Service** - Envio assíncrono de notificações

### Objetivos do Componente

1. **Centralização de Segurança**: Validação JWT unificada, evitando duplicação de lógica nos microsserviços
2. **Desacoplamento**: Clientes não conhecem detalhes internos (URLs, portas) dos microsserviços
3. **Simplificação de Integração**: Interface uniforme para consumidores (web, mobile, APIs externas)
4. **Governança**: Ponto centralizado para aplicação de políticas de segurança, rate limiting e monitoramento
5. **Elasticidade**: Facilita balanceamento de carga e redirecionamento inteligente

---

## 🏗️ Arquitetura e Padrões

### Padrões Arquiteturais Aplicados

#### 1. API Gateway Pattern
O padrão fundamental que estrutura todo o componente, provendo uma fachada unificada para a arquitetura de microsserviços.

**Benefícios:**
- Reduz acoplamento entre clientes e serviços backend
- Facilita versionamento e evolução independente de APIs
- Ponto centralizado para aplicação de cross-cutting concerns

#### 2. Reverse Proxy Pattern
Implementado através do YARP (Yet Another Reverse Proxy) da Microsoft.

**Características:**
- Encaminhamento transparente de requisições/respostas
- Preservação de headers e contexto
- Suporte a transformações de caminho (path rewriting)

#### 3. Token Validation Pattern
Validação centralizada de tokens JWT antes do encaminhamento.

**Fluxo:**
```
Cliente → JWT no Header → Gateway valida → Microsserviço (sem validação JWT)
```

**Vantagens:**
- Microsserviços não precisam validar tokens (separação de responsabilidades)
- Atualização de configurações JWT em único local
- Claims transformadas e disponibilizadas para serviços downstream

### Diagrama de Arquitetura

```
┌────────────────────────────────────────────────────────────────┐
│                         Camada Cliente                         │
│                   (Web, Mobile, APIs Externas)                 │
└───────────────────────────────┬────────────────────────────────┘
                                │ HTTPS
                                ▼
┌────────────────────────────────────────────────────────────────┐
│                      API Gateway :8080                         │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │  Pipeline de Middleware                                  │ │
│  │  1. CORS Handler                                         │ │
│  │  2. Authentication (JWT Bearer)                          │ │
│  │  3. Authorization (Policy-based)                         │ │
│  │  4. Claims Transformation                                │ │
│  │  5. YARP Reverse Proxy                                   │ │
│  └──────────────────────────────────────────────────────────┘ │
└───┬───────────┬───────────┬───────────┬────────────────────────┘
    │           │           │           │
    │ /users    │ /auth     │ /videos   │ /notifications
    ▼           ▼           ▼           ▼
┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────────┐
│  User   │ │  Auth   │ │  Video  │ │Notification │
│ Service │ │ Service │ │ Manager │ │  Service    │
│  :8081  │ │  :8082  │ │  :5002  │ │    :5001    │
│ (Kotlin)│ │ (Kotlin)│ │  (.NET) │ │    (.NET)   │
└─────────┘ └─────────┘ └─────────┘ └─────────────┘
```

### Fluxo de Processamento de Requisição

```mermaid
sequenceDiagram
    participant C as Cliente
    participant G as Gateway
    participant A as Auth Service
    participant M as Microsserviço

    C->>G: POST /api/auth/login
    G->>A: Forward request
    A->>G: JWT Token
    G->>C: JWT Token
    
    Note over C: Cliente armazena token
    
    C->>G: GET /api/users (+ JWT)
    G->>G: Valida JWT
    G->>G: Verifica autorização
    G->>G: Transforma claims
    G->>M: Forward (sem validar JWT novamente)
    M->>G: Response
    G->>C: Response
```

---

## 🚀 Stack Tecnológico

### Tecnologias Core

| Tecnologia | Versão | Justificativa |
|------------|--------|---------------|
| **.NET** | 9.0 | Runtime de alta performance, suporte a containers nativos, eco-sistema maduro |
| **ASP.NET Core** | 9.0 | Framework web moderno, middleware pipeline flexível, performance otimizada |
| **C#** | 12.0 | Linguagem type-safe, suporte a programação assíncrona, record types |
| **YARP** | 2.0 | Proxy reverso oficial Microsoft, extensível, alta performance, configuração declarativa |

### Bibliotecas e Frameworks

**Segurança:**
- `Microsoft.AspNetCore.Authentication.JwtBearer` - Validação de tokens JWT
- `System.IdentityModel.Tokens.Jwt` - Manipulação de tokens
- `Microsoft.IdentityModel.Tokens` - Validação criptográfica

**Testes:**
- `xUnit` - Framework de testes
- `FluentAssertions` - Assertions expressivas
- `coverlet.collector` - Cobertura de código
- `Microsoft.AspNetCore.Mvc.Testing` - Testes de integração

**DevOps:**
- Docker - Containerização multi-stage
- Environment Variables - Configuração 12-factor app

---

## ✨ Funcionalidades

### Funcionalidades Implementadas

#### 🔐 Autenticação e Autorização
- ✅ Validação de tokens JWT com algoritmo HS256
- ✅ Verificação de issuer, audience e lifetime
- ✅ Claims transformation (extração de userId e role)
- ✅ Policies de autorização baseadas em roles
- ✅ Suporte a rotas públicas (login, registro)

#### 🔀 Roteamento Inteligente
- ✅ Mapeamento baseado em prefixos de rota (`/api/{service}`)
- ✅ Path rewriting (remoção de prefixo `/api`)
- ✅ Encaminhamento de headers e query strings
- ✅ Suporte a todos os métodos HTTP (GET, POST, PUT, DELETE)

#### 🌐 CORS e Integração
- ✅ Configuração CORS para aplicações frontend
- ✅ AllowAnyOrigin/Method/Header (configurável para produção)
- ✅ Suporte a requisições OPTIONS (preflight)

#### 🏥 Observabilidade
- ✅ Health check endpoint (`/healthz`)
- ✅ Logs de autenticação (sucesso/falha)
- ✅ Configuração de níveis de log por ambiente

#### ⚙️ Configuração Dinâmica
- ✅ Carregamento de configurações via variáveis de ambiente
- ✅ Validação de configurações obrigatórias na inicialização
- ✅ Suporte a múltiplos ambientes (Development, Production)

---

## 🗂️ Estrutura do Projeto

```
fiap-x-api-gateway/
│
├── src/                                    # Código-fonte principal
│   ├── Configurations/                     # Classes de configuração
│   │   ├── JwtConfiguration.cs            # Configuração JWT (Secret, Issuer, Audience)
│   │   └── RouteConfiguration.cs          # Endpoints dos microsserviços
│   │
│   ├── Constants/                          # Constantes do sistema
│   │   └── RolesAndPolicies.cs            # Definição de roles e policies
│   │
│   ├── Extensions/                         # Extension methods para configuração
│   │   ├── AuthExtension.cs               # Configuração de autenticação JWT
│   │   ├── ClaimTransformationExtension.cs # Transformação de claims
│   │   └── ReverseProxyExtension.cs       # Configuração YARP (rotas e clusters)
│   │
│   ├── Program.cs                          # Entry point e pipeline de middleware
│   ├── appsettings.json                    # Configurações base
│   ├── appsettings.Development.json        # Configurações de desenvolvimento
│   └── Gateway.csproj                      # Arquivo de projeto .NET
│
├── tests/                                  # Testes automatizados
│   ├── Configurations/                     # Testes de configurações
│   │   ├── JwtConfigurationTests.cs
│   │   └── RouteConfigurationTests.cs
│   │
│   ├── Extensions/                         # Testes de extensions
│   │   ├── AuthExtensionTests.cs
│   │   ├── ClaimTransformationExtensionTests.cs
│   │   └── ReverseProxyExtensionTests.cs
│   │
│   ├── Integration/                        # Testes de integração
│   │   └── ProgramIntegrationTests.cs
│   │
│   ├── Helpers/                            # Helpers de teste
│   │   └── TestConfigurationHelper.cs
│   │
│   └── Gateway.Tests.csproj                # Projeto de testes
│
├── CoverageReport/                         # Relatórios de cobertura (gerados)
│   ├── index.html                          # Relatório HTML interativo
│   └── Summary.txt                         # Sumário de cobertura
│
├── Dockerfile                              # Multi-stage build Docker
├── coverlet.runsettings                    # Configuração de cobertura
├── .dockerignore                           # Exclusões para build Docker
├── .gitignore                              # Exclusões para Git
├── fiap-x-api-gateway.sln                 # Solution file
├── LICENSE                                 # Licença MIT
└── README.md                              # Este documento
```

### Organização por Responsabilidades

**Configurations**: Classes POCO para binding de configurações do `appsettings.json`  
**Constants**: Valores constantes (roles, policies, nomes de claims)  
**Extensions**: Métodos de extensão para configuração de serviços (DI)  
**Tests**: Cobertura de 100% de linhas e 98% de branches

---

## ⚙️ Configuração e Execução

### Pré-requisitos

- **[.NET SDK 9.0+](https://dotnet.microsoft.com/download)** - Compilação e execução
- **[Docker Desktop](https://www.docker.com/products/docker-desktop)** (opcional) - Para execução containerizada
- **[Git](https://git-scm.com/)** - Controle de versão

### Variáveis de Ambiente

#### Obrigatórias

```bash
# Configuração JWT
JWT_SECRET=your-256-bit-secret-key-min-32-characters-change-in-production
JWT_ISSUER=fiap-x
JWT_AUDIENCE=fiap-x-api

# URLs dos Microsserviços
USER_SERVICE_URL=http://localhost:8081
AUTH_SERVICE_URL=http://localhost:8082
VIDEO_PROCESSING_SERVICE_URL=http://localhost:5002
NOTIFICATION_SERVICE_URL=http://localhost:5001
```

#### Opcionais

```bash
ASPNETCORE_ENVIRONMENT=Development          # Development | Production
ASPNETCORE_URLS=http://+:8080              # URL de bind
```

### Execução Local

#### Usando .NET CLI

```bash
# 1. Clonar repositório
git clone <repository-url>
cd fiap-x-api-gateway

# 2. Restaurar dependências
dotnet restore

# 3. Configurar variáveis de ambiente (Linux/Mac)
export JWT_SECRET="your-secret-key-with-minimum-32-characters"
export JWT_ISSUER="fiap-x"
export JWT_AUDIENCE="fiap-x-api"
export USER_SERVICE_URL="http://localhost:8081"
export AUTH_SERVICE_URL="http://localhost:8082"
export VIDEO_PROCESSING_SERVICE_URL="http://localhost:5002"
export NOTIFICATION_SERVICE_URL="http://localhost:5001"

# 4. Executar aplicação
cd src
dotnet run

# Gateway disponível em: http://localhost:8080
```

#### Usando Docker

```bash
# Build da imagem
docker build -t fiap-x-gateway:latest .

# Executar container
docker run -d \
  --name fiapx-gateway \
  -p 8080:8080 \
  -e JWT_SECRET="your-secret-key" \
  -e JWT_ISSUER="fiap-x" \
  -e JWT_AUDIENCE="fiap-x-api" \
  -e USER_SERVICE_URL="http://user-service:8081" \
  -e AUTH_SERVICE_URL="http://auth-service:8082" \
  -e VIDEO_PROCESSING_SERVICE_URL="http://video-service:5002" \
  -e NOTIFICATION_SERVICE_URL="http://notification-service:5001" \
  fiap-x-gateway:latest

# Verificar logs
docker logs -f fiapx-gateway

# Health check
curl http://localhost:8080/healthz
```

### Execução de Testes

```bash
# Executar todos os testes
dotnet test

# Executar testes com cobertura
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

# Gerar relatório HTML de cobertura
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator \
  -reports:"**/coverage.cobertura.xml" \
  -targetdir:"CoverageReport" \
  -reporttypes:Html

# Abrir relatório
open CoverageReport/index.html  # Mac
xdg-open CoverageReport/index.html  # Linux
```

---

## 📡 Rotas e Endpoints

### Mapeamento de Rotas

O Gateway implementa roteamento baseado em prefixos de caminho, removendo o prefixo `/api` antes de encaminhar aos microsserviços.

| Prefixo Gateway | Microsserviço Destino | Porta | Autenticação | Exemplo |
|-----------------|----------------------|-------|--------------|---------|
| `/api/auth/**` | Auth Service | 8082 | ❌ Não | `POST /api/auth/login` → `POST /auth/login` |
| `/api/users/**` | User Service | 8081 | ✅ Sim | `GET /api/users` → `GET /users` |
| `/api/videos/**` | Video Manager | 5002 | ✅ Sim | `POST /api/videos/upload` → `POST /videos/upload` |
| `/api/notifications/**` | Notification Service | 5001 | ✅ Sim | `GET /api/notifications/user/{id}` → `GET /notifications/user/{id}` |

### Exemplos de Uso

#### 1. Autenticação (Público)

```http
POST http://localhost:8080/api/auth/login
Content-Type: application/json

{
  "email": "usuario@fiap.com",
  "password": "SenhaSegura123!"
}

# Response: 200 OK
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresIn": 3600
}
```

#### 2. Listar Usuários (Autenticado)

```http
GET http://localhost:8080/api/users
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# Response: 200 OK
[
  {
    "id": "uuid-123",
    "name": "João Silva",
    "email": "joao@fiap.com"
  }
]
```

#### 3. Upload de Vídeo (Autenticado)

```http
POST http://localhost:8080/api/videos/upload
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
Content-Type: multipart/form-data

# Form data:
# file: video.mp4

# Response: 202 Accepted
{
  "videoId": "uuid-456",
  "status": "processing"
}
```

#### 4. Health Check (Público)

```http
GET http://localhost:8080/healthz

# Response: 200 OK
{
  "status": "ok",
  "timestamp": "2026-03-15T10:30:00Z"
}
```

---

## 🔐 Segurança

### Implementação JWT

#### Validação de Token

O Gateway valida cada token JWT recebido no header `Authorization: Bearer <token>` contra os seguintes critérios:

**Validações Aplicadas:**
- ✅ **Signature Validation**: Verifica assinatura com chave simétrica (HS256)
- ✅ **Issuer Validation**: Garante que o token foi emitido pelo Auth Service esperado
- ✅ **Audience Validation**: Verifica se o token é destinado ao FIAP X
- ✅ **Lifetime Validation**: Valida se o token não expirou (claim `exp`)
- ✅ **Clock Skew**: Zero tolerance para tolerância de relógio

#### Configuração JWT

```csharp
TokenValidationParameters = new()
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtConfig.Secret)
    ),
    ValidateIssuer = true,
    ValidIssuer = jwtConfig.Issuer,
    ValidateAudience = true,
    ValidAudience = jwtConfig.Audience,
    ValidateLifetime = true,
    ClockSkew = TimeSpan.Zero,
    RoleClaimType = "role",
    NameClaimType = "name"
}
```

### Claims Transformation

O Gateway extrai e transforma claims JWT para facilitar autorização:

**Claims Extraídas:**
```json
{
  "sub": "user-uuid-123",          // User ID
  "email": "usuario@fiap.com",     // Email
  "role": "USER",                  // Role (ADMIN | USER)
  "name": "João Silva"             // Nome do usuário
}
```

### Políticas de Autorização

| Policy | Roles Permitidas | Aplicação |
|--------|------------------|-----------|
| `AUTHENTICATED_USER` | `ADMIN`, `USER` | Todas as rotas protegidas |

**Exemplo de aplicação:**
```csharp
RouteConfig {
    RouteId = "users-get",
    AuthorizationPolicy = Policies.AUTHENTICATED_USER,
    // ...
}
```

### Segurança em Produção

**Recomendações:**
- ✅ Usar HTTPS/TLS para todas as comunicações
- ✅ Configurar CORS para origens específicas (remover `AllowAnyOrigin`)
- ✅ Implementar rate limiting para prevenir DDoS
- ✅ Rodar secrets em serviços gerenciados (AWS Secrets Manager, Azure Key Vault)
- ✅ Configurar JWT_SECRET com mínimo 256 bits de entropia
- ✅ Implementar token refresh para melhorar segurança
- ✅ Adicionar logging de eventos de segurança

---

## 🧪 Testes e Qualidade

### Cobertura de Testes

**Métricas de Cobertura:**
```
Line Coverage:   100% (314/314 linhas)
Branch Coverage:  98% (51/52 branches)
Method Coverage: 100% (17/17 métodos)
```

### Estratégia de Testes

#### Testes Unitários

**Configurações** (`JwtConfigurationTests`, `RouteConfigurationTests`):
- Validação de configurações obrigatórias
- Validação de formatos (URLs, secrets)
- Testes de edge cases (strings vazias, nulls)

**Extensions** (`AuthExtensionTests`, `ReverseProxyExtensionTests`):
- Configuração correta de serviços DI
- Validação de parâmetros de autenticação
- Montagem correta de rotas YARP

#### Testes de Integração

**`ProgramIntegrationTests`**:
- Inicialização completa da aplicação
- Teste end-to-end de rotas protegidas
- Validação de pipeline de middleware
- Teste de health check

### Execução de Testes

```bash
# Executar todos os testes
dotnet test --logger "console;verbosity=detailed"

# Executar testes com cobertura
dotnet test \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  --results-directory ./TestResults

# Ver relatório de cobertura
cat ./CoverageReport/Summary.txt
```

### Qualidade de Código

**Práticas Aplicadas:**
- ✅ SOLID Principles (especialmente Single Responsibility)
- ✅ Dependency Injection para desacoplamento
- ✅ Extension Methods para configuração fluente
- ✅ Validation-at-borders (validação de configurações na startup)
- ✅ Fail-fast approach (aplicação não inicia com configurações inválidas)
- ✅ Immutability com record types (onde aplicável)

---

## 🐳 Deployment

### Dockerfile Multi-Stage

O projeto utiliza build multi-stage para otimização de imagem:

```dockerfile
# Stage 1: Base runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

# Stage 2: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Gateway.csproj", "src/"]
RUN dotnet restore "src/Gateway.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "Gateway.csproj" -c Release -o /app/build

# Stage 3: Publish
FROM build AS publish
RUN dotnet publish "Gateway.csproj" -c Release -o /app/publish

# Stage 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]
```

**Benefícios:**
- Imagem final menor (~210 MB) - apenas runtime ASP.NET
- Build cache otimizado
- Separação de dependências de build e runtime

### Docker Compose - Sistema Completo

O sistema completo pode ser orquestrado via Docker Compose:

```yaml
version: '3.8'

services:
  # Banco de dados
  postgres:
    image: postgres:16-alpine
    environment:
      POSTGRES_DB: videomanager_db
      POSTGRES_USER: postgres
      POSTGRES_PASSWORD: postgres123
    ports:
      - "5432:5432"
    networks:
      - fiapx-network

  # Armazenamento de objetos
  minio:
    image: minio/minio:latest
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin123
    command: server /data --console-address ":9001"
    ports:
      - "9000:9000"
      - "9001:9001"
    networks:
      - fiapx-network

  # Microsserviços
  notification-service:
    build: ./fiap-x-microservice-notification
    ports:
      - "5001:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      SMTP__Host: ${SMTP_HOST}
      SMTP__Port: ${SMTP_PORT}
    networks:
      - fiapx-network

  video-manager-service:
    build: ./fiap-x-microsservice-video-manager
    ports:
      - "5002:8080"
    depends_on:
      - postgres
      - minio
      - notification-service
    environment:
      ConnectionStrings__DefaultConnection: "Host=postgres;Database=videomanager_db;..."
      Services__NotificationService__Url: http://notification-service:8080
    networks:
      - fiapx-network

  # API Gateway
  api-gateway:
    build: ./fiap-x-api-gateway
    ports:
      - "8080:8080"
    depends_on:
      - notification-service
      - video-manager-service
    environment:
      JWT_SECRET: ${JWT_SECRET}
      JWT_ISSUER: fiap-x
      JWT_AUDIENCE: fiap-x-api
      USER_SERVICE_URL: http://user-service:8081
      AUTH_SERVICE_URL: http://auth-service:8082
      VIDEO_PROCESSING_SERVICE_URL: http://video-manager-service:8080
      NOTIFICATION_SERVICE_URL: http://notification-service:8080
    networks:
      - fiapx-network

networks:
  fiapx-network:
    driver: bridge
```

### Deployment em Cloud

**Recomendações para AWS:**
- **ECS/Fargate**: Deploy de containers sem gerenciar servidores
- **ALB**: Load balancing com health checks
- **Secrets Manager**: Gerenciamento seguro de JWT_SECRET
- **CloudWatch**: Logs e métricas centralizadas

**Recomendações para Azure:**
- **Azure Container Instances/App Service**: Containers gerenciados
- **Application Gateway**: Load balancing e WAF
- **Key Vault**: Gerenciamento de secrets
- **Application Insights**: APM e observabilidade

---

## 💡 Decisões Arquiteturais

### 1. Escolha do YARP sobre Ocelot

**Motivação:**
- YARP é mantido oficialmente pela Microsoft (.NET team)
- Performance superior (baseado em `HttpClient` otimizado)
- Configuração declarativa mais flexível
- Melhor suporte a .NET 9.0 e features modernas
- Documentação e comunidade ativas

**Trade-offs:**
- YARP é mais low-level, exigindo mais configuração manual
- Ocelot tem features prontas (rate limiting, caching) que YARP requer implementação

**Decisão:** YARP foi escolhido pela performance, suporte oficial e alinhamento com stack .NET moderna.

### 2. Validação JWT Centralizada

**Motivação:**
- Evitar duplicação de lógica de validação JWT em cada microsserviço
- Simplificar microsserviços (não precisam conhecer JWT_SECRET)
- Facilitar rotação de chaves (apenas Gateway precisa ser atualizado)

**Implementação:**
- Gateway valida JWT e adiciona claims ao contexto
- Microsserviços confiam no Gateway (não re-validam token)
- Conexões entre Gateway e microsserviços em rede privada

**Risco Mitigado:**
- Se Gateway for comprometido, atacante pode chamar microsserviços diretamente
- **Mitigação**: Network policies isolam microsserviços (apenas Gateway tem acesso)

### 3. Configuração via Variáveis de Ambiente

**Motivação:**
- Seguir 12-Factor App (Config separada de código)
- Facilitar deploy em múltiplos ambientes (dev, staging, prod)
- Compatibilidade com orquestradores (Kubernetes, ECS)

**Implementação:**
```csharp
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET") 
                     ?? builder.Configuration["Jwt:Secret"],
    // ...
});
```

### 4. Claims Transformation

**Motivação:**
- Normalizar claims JWT para formato esperado por microsserviços
- Facilitar autorização baseada em roles
- Adicionar contexto adicional (userId extraído de `sub`)

**Implementação:**
```csharp
OnTokenValidated = context =>
{
    context.TransformClaims();  // Extension method customizada
    return Task.CompletedTask;
}
```

**Benefício:**
- Microsserviços recebem claims já processadas em headers HTTP
- Não precisam conhecer estrutura interna de JWT

### 5. Path Rewriting

**Motivação:**
- Clientes não precisam conhecer estrutura interna de URLs dos microsserviços
- Permite versionamento de APIs (`/api/v1/users`, `/api/v2/users`)
- Facilita migração de microsserviços (mudar porta/host sem afetar clientes)

**Implementação:**
```csharp
Transforms = new[]
{
    new Dictionary<string, string> { ["PathRemovePrefix"] = "/api" }
}
```

**Exemplo:**
```
Cliente:         GET /api/users/123
Gateway remove:  GET /users/123  (encaminhado ao User Service)
```

### 6. Fail-Fast na Inicialização

**Motivação:**
- Detectar configurações inválidas antes de aceitar tráfego
- Evitar comportamento imprevisível em runtime

**Implementação:**
```csharp
jwtConfig.Validate();  // Lança exceção se configuração inválida
servicesConfig.UserService.Validate("UserService");
```

**Benefício:**
- Aplicação não inicia se JWT_SECRET estiver vazio
- Erros detectados em tempo de deployment, não em produção

---

## 📚 Referências Técnicas

### Documentação Oficial
- [ASP.NET Core Documentation](https://docs.microsoft.com/en-us/aspnet/core/)
- [YARP Documentation](https://microsoft.github.io/reverse-proxy/)
- [JWT RFC 7519](https://datatracker.ietf.org/doc/html/rfc7519)

### Padrões e Práticas
- [API Gateway Pattern - Microsoft Docs](https://docs.microsoft.com/en-us/azure/architecture/microservices/design/gateway)
- [12-Factor App](https://12factor.net/)
- [.NET Microservices: Architecture for Containerized Applications](https://docs.microsoft.com/en-us/dotnet/architecture/microservices/)

---

## 📄 Licença

Este projeto está licenciado sob a **MIT License**. Consulte o arquivo [LICENSE](LICENSE) para detalhes.

---

## 👥 Equipe de Desenvolvimento

**Pós-Graduação em Arquitetura de Software - FIAP**

| Nome | Responsabilidade | Tecnologia |
|------|------------------|------------|
| **Elen** | API Gateway, Notification Service | .NET 9.0, C# |
| **Vitória** | User Service, Auth Service | Kotlin, Spring Boot |
| **Marcelle** | Video Processing, Video Manager | .NET 9.0, C# |

---

## 📞 Contato e Suporte

Para dúvidas, sugestões ou reportar problemas:

- **Issues**: Utilize o sistema de [Issues](https://github.com/FIAPxHack/fiap-x-api-gateway/issues) do GitHub
- **Pull Requests**: Contribuições são bem-vindas via Pull Requests
- **Documentação**: Consulte este README e os comentários no código-fonte

---

**Desenvolvido com ❤️ para FIAP - Pós-Graduação em Arquitetura de Software**
