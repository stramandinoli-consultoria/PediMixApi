# 🎵 PediMix API

**API .NET 8.0 com Arquitetura CQRS para Sistema de Gerenciamento de Eventos Musicais**

[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)
[![MySQL](https://img.shields.io/badge/MySQL-8.0-orange.svg)](https://www.mysql.com/)
[![Railway](https://img.shields.io/badge/Railway-Database-purple.svg)](https://railway.app/)
[![Swagger](https://img.shields.io/badge/Swagger-OpenAPI-green.svg)](https://swagger.io/)

## 🎯 Sobre o Projeto

O **PediMix** é uma plataforma que conecta artistas, locais e público para eventos musicais, permitindo que o público faça pedidos de músicas em tempo real durante os shows. Esta API fornece todos os serviços backend necessários para suportar a plataforma web e mobile.

### ✨ Principais Funcionalidades

- 🎤 **Gestão de Artistas e Perfis**
- 🏢 **Cadastro de Locais e Eventos**
- 🎵 **Catálogo Completo de Músicas**
- 📝 **Sistema de Pedidos de Música**
- 🎼 **Gerenciamento de Repertórios**
- 👥 **Sistema de Usuários Multi-perfil**
- 🔍 **Busca Avançada e Filtros**

---

## 🏗️ Arquitetura

### Clean Architecture + CQRS

```
📁 PediMix.API/            # Controllers e configuração da API
📁 PediMix.Application/    # Commands, Queries, DTOs e Handlers
📁 PediMix.Infrastructure/ # Repositórios e Entity Framework
📁 PediMix.Domain/         # Entidades, Enums e regras de negócio
```

### 🛠️ Stack Tecnológico

- **Framework**: .NET Core 8.0
- **ORM**: Entity Framework Core 9.0.8
- **Database**: MySQL 8.0+ (Railway Cloud)
- **Patterns**: CQRS com MediatR 13.0.0
- **Mapping**: AutoMapper 15.0.1
- **Documentation**: Swagger/OpenAPI
- **Architecture**: Clean Architecture

---

## 🚀 Quick Start

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- MySQL Client (opcional, para dados iniciais)

### 1. Clone o repositório

```bash
git clone https://github.com/stramandinoli-consultoria/PediMixApi.git
cd PediMixApi
```

### 2. Configure o banco de dados

Copie o arquivo de configuração e preencha com suas credenciais:

```bash
cp PediMix.API/appsettings.example.json PediMix.API/appsettings.Development.json
```

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_HOST;Port=PORTA;Database=NOME_DB;User=USUARIO;Password=SUA_SENHA"
  }
}
```

### 3. Execute as Migrations

```bash
cd PediMix.Infrastructure
dotnet ef database update
```

### 4. Execute a API

```bash
cd ../PediMix.API
dotnet run
```

### 5. Acesse a Documentação

- **API**: http://localhost:5062
- **Swagger**: http://localhost:5062/swagger

### 6. Popular Dados Iniciais (Opcional)

```bash
cd database

# Windows PowerShell
.\populate-railway.ps1

# Ou via MySQL direto
mysql -h SEU_HOST -u USUARIO -p"SUA_SENHA" --port PORTA --protocol=TCP NOME_DB < seed-data.sql
```

---

## 📊 Modelo de Dados

### Entidades Principais

| Entidade        | Descrição                                            |
|-----------------|------------------------------------------------------|
| `User`          | Usuários do sistema (Audience, Singer, Venue, Admin) |
| `ArtistProfile` | Perfis de artistas com gêneros musicais              |
| `VenueProfile`  | Perfis de locais com endereços e comodidades         |
| `Song`          | Catálogo de músicas com metadados completos          |
| `Event`         | Eventos musicais com datas, locais e artistas        |
| `SongRequest`   | Pedidos de música feitos pelo público                |
| `Repertoire`    | Repertórios organizados por artistas                 |
| `Genre`         | Gêneros musicais                                     |
| `Amenity`       | Comodidades disponíveis nos locais                   |

### Relacionamentos

- Many-to-many entre Artists e Genres
- Many-to-many entre Venues e Amenities
- Many-to-many entre Repertoires e Songs
- Many-to-many entre Events e Genres
- Relacionamentos One-to-many apropriados
- Cascade deletes configurados

---

## 🔌 Endpoints da API

### 👥 Usuários (`/api/users`)

```http
GET    /api/users/{id}              # Buscar usuário por ID
GET    /api/users/email/{email}     # Buscar usuário por email
POST   /api/users                   # Criar novo usuário
PUT    /api/users/{id}              # Atualizar usuário
```

### 🎵 Músicas (`/api/songs`)

```http
GET    /api/songs/genre/{genreId}        # Listar músicas por gênero
GET    /api/songs/search?query=termo     # Buscar músicas por texto
POST   /api/songs                        # Adicionar nova música
```

### 🎪 Eventos (`/api/events`)

```http
GET    /api/events/{id}                           # Buscar evento por ID
GET    /api/events/upcoming?count=10              # Próximos eventos
GET    /api/events/date-range?startDate=&endDate= # Eventos por período
POST   /api/events                                # Criar novo evento
```

### 📝 Pedidos de Música (`/api/songrequests`)

```http
GET    /api/songrequests/event/{eventId}?pendingOnly=false  # Pedidos por evento
POST   /api/songrequests                                    # Fazer pedido de música
```

### 🎼 Repertórios (`/api/repertoires`)

```http
GET    /api/repertoires/{id}/songs                    # Repertório com músicas
GET    /api/repertoires/artist/{artistId}?activeOnly= # Repertórios do artista
```

---

## 🧪 Exemplos de Uso

### Criar um Usuário Artista

```bash
curl -X POST "http://localhost:5062/api/users" \
-H "Content-Type: application/json" \
-d '{
  "username": "joao_cantor",
  "email": "joao@email.com",
  "firstName": "João",
  "lastName": "Silva",
  "passwordHash": "hash_da_senha",
  "phoneNumber": "+5511999999999",
  "role": 2
}'
```

### Buscar Músicas de Rock

```bash
curl "http://localhost:5062/api/songs/search?query=rock"
```

### Fazer Pedido de Música

```bash
curl -X POST "http://localhost:5062/api/songrequests" \
-H "Content-Type: application/json" \
-d '{
  "eventId": "uuid-do-evento",
  "songId": "uuid-da-musica",
  "userId": "uuid-do-usuario",
  "message": "Por favor, toque essa música!"
}'
```

## 🔧 Configuração Railway

A API está configurada para usar Railway MySQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_HOST;Port=PORTA;Database=NOME_DB;User=USUARIO;Password=SUA_SENHA"
  }
}
```

## 🏗️ Padrões CQRS

### Commands (Escritas)

- `CreateUserCommand` — Criar usuário
- `UpdateUserCommand` — Atualizar usuário
- `CreateSongCommand` — Adicionar música
- `CreateEventCommand` — Criar evento
- `CreateSongRequestCommand` — Fazer pedido

### Queries (Leituras)

- `GetUserByIdQuery` — Buscar usuário por ID
- `GetUserByEmailQuery` — Buscar usuário por email
- `GetSongsByGenreQuery` — Músicas por gênero
- `SearchSongsQuery` — Buscar músicas
- `GetEventsByDateRangeQuery` — Eventos por período
- `GetUpcomingEventsQuery` — Próximos eventos

Cada Command e Query possui um Handler correspondente que implementa a lógica de negócio utilizando os padrões Repository e Unit of Work.

---

## 🗃️ Dados de Exemplo

O script `seed-data.sql` inclui:

- **10 gêneros musicais**: Rock, Pop, Sertanejo, MPB, Blues, Jazz, etc.
- **10 comodidades**: Sistema de Som, Microfones, Palco, Iluminação, etc.
- **10 músicas populares**: Bohemian Rhapsody, Hotel California, Evidências, etc.
- **1 usuário administrador** padrão

---

## 🔧 Configurações Avançadas

### CORS

O CORS está configurado para aceitar qualquer origem durante desenvolvimento. Para produção, edite `Program.cs`:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins("https://pedimix.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

### Logging

Ajuste o nível de logs em `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  }
}
```

---

## 🚀 Deploy

A API está pronta para deploy em qualquer provedor cloud:

- **Railway**: Configuração atual
- **Azure**: App Service compatível
- **AWS**: ECS/Lambda ready
- **Docker**: Dockerfile incluído

### Compilação para Produção

```bash
cd PediMix.API
dotnet publish -c Release -o ./publish
```

---

## 📝 Próximos Passos

- [ ] **Autenticação JWT** completa
- [ ] **Validações** com FluentValidation
- [ ] **Logs Estruturados** com Serilog
- [ ] **Testes** unitários e de integração
- [ ] **Upload de arquivos** (imagens, áudio)
- [ ] **Notificações em tempo real** (SignalR)
- [ ] **Cache** com Redis
- [ ] **Background Jobs** com Hangfire
- [ ] **Rate limiting**

---

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma feature branch (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -am 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

---

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.

---

<div align="center">

**🎵 Desenvolvido com ❤️ para a comunidade musical brasileira 🇧🇷**

⭐ **Se este projeto te ajudou, considere dar uma estrela!** ⭐

</div>
