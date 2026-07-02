# ES.ConexaoSolidaria.Usuarios

Microsserviço de **Usuários e Autenticação** da plataforma **Conexão Solidária**, desenvolvido para a ONG Esperança Solidária como parte do Hackathon POSTECH/FIAP.

Responsável por:
- Cadastro, consulta, atualização e remoção (LGPD) de usuários;
- Autenticação com **JWT**, login/logout e blacklist de tokens (Redis);
- Controle de acesso por perfil — **RBAC** (`GESTOR_ONG` e `DOADOR`);
- Publicação de eventos de domínio (ex.: `UserCreatedEvent`) via **RabbitMQ** (local) ou **SQS** (ambiente `LAB`/AWS);
- Auditoria das operações em **DynamoDB**;
- Métricas expostas via **Prometheus** e visualizadas em **Grafana**.

---

## Sumário
- [Arquitetura](#arquitetura)
- [Stack Tecnológica](#stack-tecnológica)
- [Perfis e Regras de Acesso](#perfis-e-regras-de-acesso)
- [Endpoints](#endpoints)
- [Como Rodar Localmente](#como-rodar-localmente)
- [Variáveis de Ambiente](#variáveis-de-ambiente)
- [Observabilidade](#observabilidade)
- [Eventos de Domínio](#eventos-de-dominio)
- [Testes](#testes)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Github Actions](#github-actions)


---

## Arquitetura

Este serviço segue **Clean Architecture** dividido em quatro projetos (`Api`, `Application`, `Domain`, `Infrastructure`) e implementa CQRS via handlers de Use Case (`IUseCaseHandler<TCommand, TResult>`).

Ao criar um usuário, a API **não** notifica outros serviços diretamente: ela publica um `UserCreatedEvent` em um broker de mensageria, permitindo que outros microsserviços da plataforma (ex.: Campanhas/Doações) reajam de forma assíncrona e desacoplada.

> O diagrama completo da arquitetura da plataforma (todos os microsserviços, bancos de dados, broker e observabilidade) está no repositório de infraestrutura — [https://github.com/gmerendi/ES.ConexaoSolidaria.Infra]

## Stack Tecnológica

- **.NET 8** (ASP.NET Core Web API)
- **Entity Framework Core** + **PostgreSQL** (dados relacionais dos usuários)
- **DynamoDB Local** (log de auditoria)
- **Redis** (cache de sessão e blacklist de tokens JWT)
- **RabbitMQ** (via **MassTransit**) em ambiente local, com fallback para **Amazon SQS** em ambiente `LAB`
- **JWT Bearer Authentication**
- **BCrypt**/hash de senha e criptografia AES para dados sensíveis (CPF anonimizado nos eventos)
- **Prometheus** + **Grafana** (métricas e dashboards)
- **Docker** / **Docker Compose**
- **xUnit** (testes)

## Perfis e Regras de Acesso

| Role | Descrição |
|---|---|
| `GESTOR_ONG` | Acesso administrativo: pode suspender/ativar usuários e alterar perfis |
| `DOADOR` | Perfil padrão de todo novo cadastro; acesso ao próprio perfil |

- Todo novo usuário é criado com o perfil **DOADOR** por padrão — não é possível se autocadastrar como `GESTOR_ONG`.
- CPF, e-mail e senha possuem validação de formato e regras de negócio (unicidade de e-mail/CPF, senha forte, etc.) aplicadas no domínio.
- Remoção de usuário segue a **LGPD**: os dados pessoais são excluídos fisicamente do banco, mas doações associadas permanecem preservadas para fins de auditoria/transparência.

## Endpoints

### Auth — `api/v1/auth`
| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/login` | Público | Autentica o usuário e retorna o token JWT |
| `POST` | `/logout` | `GESTOR_ONG`, `DOADOR` | Invalida o token atual (blacklist no Redis) |
| `PUT` | `/reset-password` | `GESTOR_ONG`, `DOADOR` | Reseta a senha do usuário logado |

### Usuário — `api/v1/usuario`
| Método | Rota | Acesso | Descrição |
|---|---|---|---|
| `POST` | `/` | Público | Cadastra um novo usuário (sempre como `DOADOR`) |
| `GET` | `/` | `GESTOR_ONG`, `DOADOR` | Consulta um usuário por e-mail (doador só vê o próprio perfil) |
| `DELETE` | `/` | `GESTOR_ONG`, `DOADOR` | Remove os dados pessoais do usuário (LGPD) |
| `PUT` | `/suspender` | `GESTOR_ONG` | Suspende um usuário |
| `PUT` | `/ativar` | `GESTOR_ONG` | Reativa um usuário |
| `PUT` | `/alterar-para-gestor` | `GESTOR_ONG` | Promove um usuário para `GESTOR_ONG` |
| `PUT` | `/alterar-para-doador` | `GESTOR_ONG` | Rebaixa um usuário para `DOADOR` |
| `PUT` | `/alterar` | `GESTOR_ONG`, `DOADOR` | Atualiza nome completo e CPF do usuário logado |

### Observabilidade
| Rota | Descrição |
|---|---|
| `/health/ready` | Readiness probe |
| `/health/live` | Liveness probe |
| `/metrics` | Métricas no formato Prometheus |

A documentação interativa (Swagger) fica disponível em `/swagger` quando a API está rodando.

## Como Rodar Localmente

### Pré-requisitos
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Docker Desktop 4.79.0

### Subindo tudo com Docker Compose (recomendado)

O `docker-compose.yml` já sobe toda a infraestrutura necessária: PostgreSQL, Redis, DynamoDB Local, RabbitMQ, a própria API, Prometheus e Grafana.

```bash
# 1. Clone o repositório
git clone https://github.com/gmerendi/ES.ConexaoSolidaria.Usuarios.git
cd ES.ConexaoSolidaria.Usuarios

# 2. Suba todos os serviços
docker compose up -d --build

# 3. Acompanhe os logs da API (opcional)
docker compose logs -f cs.usuarios.api
```

Serviços expostos:

| Serviço | URL/Porta |
|---|---|
| API de Usuários | http://localhost:5001 (Swagger em `/swagger`) |
| PostgreSQL | localhost:5433 |
| Redis | localhost:6379 |
| DynamoDB Local | localhost:8000 |
| RabbitMQ (AMQP / Management UI) | localhost:5672 / http://localhost:15672 (`fiap` / `fiap123`) |
| Prometheus | http://localhost:9090 |
| Grafana | http://localhost:3000 (`admin` / `fiap123`) |

As migrations do banco são aplicadas automaticamente na subida da aplicação.

## Variáveis de Ambiente

Todas já vêm configuradas no `docker-compose.yml` para o ambiente local. Principais chaves:

| Variável | Descrição |
|---|---|
| `ConnectionStrings__Database` / `ConnectionStrings__ConnectionString` | Conexão com o PostgreSQL |
| `ConnectionStrings__AuditLog` | Endpoint do DynamoDB (auditoria) |
| `ConnectionStrings__Redis` | Conexão com o Redis |
| `RabbitMq__Host` / `RabbitMq__Username` / `RabbitMq__Password` | Conexão com o RabbitMQ |
| `USER_CREATED_QUEUE` | Nome da fila/evento de usuário criado |
| `Jwt__SecretKey` / `Jwt__Issuer` / `Jwt__Audience` / `Jwt__ExpirationHours` | Configuração do token JWT |
| `AES__KEY` | Chave usada para criptografia de dados sensíveis |
| `Application__Type` | `LOCAL` (usa RabbitMQ) ou `LAB` (usa SQS/AWS) |
| `Admin__Email` / `Admin__Password` | Credenciais do usuário administrador seed |

> **Atenção:** os valores no `docker-compose.yml` são apenas para desenvolvimento local. Nunca reutilize essas chaves em produção.

## Eventos de Dominio

- UserCreatedEvent - Publicado quando um usuário é criado.

## Observabilidade

- A API expõe métricas em `/metrics` (Prometheus).
- O Prometheus (`observability/prometheus`) já está pré-configurado para fazer scrape da API automaticamente.
- O Grafana (`observability/grafana`) sobe com o datasource do Prometheus provisionado e dashboards prontos em `observability/grafana/provisioning/dashboards`.
- Health checks disponíveis em `/health/ready` e `/health/live`.

## Testes

```bash
dotnet test
```

Os testes de unidade (xUnit) estão em `tests/Usuarios.Test` e cobrem as regras de domínio e casos de uso.

## Estrutura do Projeto

```
ES.ConexaoSolidaria.Usuarios/
├── src/
│   ├── Usuarios.Api/              # Controllers, Program.cs, Middlewares, Swagger, Health Checks
│   ├── Usuarios.Application/      # Use Cases (Commands/Queries/Handlers) por Feature
│   ├── Usuarios.Domain/           # Entidades, Value Objects, Eventos de Domínio, regras de negócio
│   └── Usuarios.Infrastructure/   # EF Core, Repositórios, Messaging, Cache, Auditoria, Métricas
├── tests/
│   └── Usuarios.Test/             # Testes de unidade (xUnit)
├── observability/
│   ├── prometheus/                # Configuração de scrape do Prometheus
│   └── grafana/                   # Datasource e dashboards provisionados
├── docker-compose.yml             # Orquestração local (API + infra + observabilidade)
└── ES.ConexaoSolidaria.Usuarios.slnx
```

Projeto desenvolvido para o Hackathon **POSTECH** — grupo 1.

## Github Actions

O repositório contém um pipeline GitHub Actions, acionado a cada push na branch principal. O pipeline compila o código (.NET build), executa os testes e gera a imagem Docker.

