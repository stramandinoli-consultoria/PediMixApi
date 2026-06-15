# 🎵 PediMix API

**API .NET 6.0 com Arquitetura CQRS para Sistema de Gerenciamento de Eventos Musicais**

[![.NET](https://img.shields.io/badge/.NET-6.0-blue.svg)](https://dotnet.microsoft.com/)
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

## 🏗️ Arquitetura

### Clean Architecture + CQRS

```
📁 PediMix.API/          # Controllers e configuração da API
📁 PediMix.Application/  # Commands, Queries, DTOs e Handlers  
📁 PediMix.Infrastructure/ # Repositórios e Entity Framework
📁 PediMix.Domain/       # Entidades, Enums e regras de negócio
```

### 🛠️ Stack Tecnológico

- **Framework**: .NET Core 8.0
- **ORM**: Entity Framework Core 9.0.8
- **Database**: MySQL 8.0+ (Railway Cloud)
- **Patterns**: CQRS com MediatR 13.0.0
- **Mapping**: AutoMapper 15.0.1
- **Documentation**: Swagger/OpenAPI
- **Architecture**: Clean Architecture

## 🚀 Quick Start

### Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- MySQL Client (opcional, para dados iniciais)

### 1. Clone e Configure

```bash
git clone https://github.com/newskyrender/PediMixApi.git
cd PediMixApi
```

### 2. Execute as Migrations

```bash
cd PediMix.Infrastructure
dotnet ef database update
```

### 3. Execute a API

```bash
cd ../PediMix.API
dotnet run
```

### 4. Acesse a Documentação

- **API**: http://localhost:5062
- **Swagger**: http://localhost:5062/swagger

### 5. Popular Dados Iniciais (Opcional)

```bash
cd database
# Windows PowerShell
.\populate-railway.ps1

# Ou comando direto
```

## 📊 Modelo de Dados

### Entidades Principais

| Entidade | Descrição |
|----------|-----------|
| `User` | Usuários do sistema (Audience, Singer, Venue, Admin) |
| `ArtistProfile` | Perfis de artistas com gêneros musicais |
| `VenueProfile` | Perfis de locais com endereços e comodidades |
| `Song` | Catálogo de músicas com metadados completos |
| `Event` | Eventos musicais com datas, locais e artistas |
| `SongRequest` | Pedidos de música feitos pelo público |
| `Repertoire` | Repertórios organizados por artistas |
| `Genre` | Gêneros musicais |
| `Amenity` | Comodidades disponíveis nos locais |

### Relacionamentos

- Many-to-many entre Artists e Genres
- Many-to-many entre Venues e Amenities  
- Many-to-many entre Repertoires e Songs
- Many-to-many entre Events e Genres
- Relacionamentos One-to-many apropriados
- Cascade deletes configurados

## 🔌 API Endpoints

### 👥 Usuários (`/api/users`)

```http
GET    /api/users/{id}              # Buscar usuário por ID
GET    /api/users/email/{email}     # Buscar usuário por email  
POST   /api/users                   # Criar novo usuário
PUT    /api/users/{id}              # Atualizar usuário
```

### 🎵 Músicas (`/api/songs`)

```http
GET    /api/songs/genre/{genreId}   # Listar músicas por gênero
GET    /api/songs/search?query=     # Buscar músicas por texto
POST   /api/songs                   # Adicionar nova música
```

### 🎪 Eventos (`/api/events`)

```http
GET    /api/events/{id}             # Buscar evento por ID
GET    /api/events/upcoming?count=  # Próximos eventos
GET    /api/events/date-range       # Eventos por período
POST   /api/events                  # Criar novo evento
```

### 📝 Pedidos de Música (`/api/songrequests`)

```http
GET    /api/songrequests/event/{eventId}  # Pedidos por evento
POST   /api/songrequests                  # Fazer pedido de música
```

### 🎼 Repertórios (`/api/repertoires`)

```http
GET    /api/repertoires/{id}/songs        # Repertório com músicas
GET    /api/repertoires/artist/{artistId} # Repertórios do artista
```

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
    "DefaultConnection": "Server=mainline.proxy.rlwy.net;Port=49986;Database=railway;User=root;Password=jYFqsjMdBZJGWfEMrukyftRgcEYazGKq;SslMode=Required;"
  }
}
```

## 📋 Padrões CQRS

### Commands (Escritas)

- `CreateUserCommand` - Criar usuário
- `UpdateUserCommand` - Atualizar usuário  
- `CreateSongCommand` - Adicionar música
- `CreateEventCommand` - Criar evento
- `CreateSongRequestCommand` - Fazer pedido

### Queries (Leituras)

- `GetUserByIdQuery` - Buscar usuário por ID
- `GetUserByEmailQuery` - Buscar usuário por email
- `SearchSongsQuery` - Buscar músicas
- `GetEventsByDateRangeQuery` - Eventos por período
- `GetUpcomingEventsQuery` - Próximos eventos

## 🗃️ Dados de Exemplo

O script `seed-data.sql` inclui:

- **10 gêneros musicais**: Rock, Pop, Sertanejo, MPB, Blues, Jazz, etc.
- **10 comodidades**: Sistema de Som, Microfones, Palco, Iluminação, etc.
- **10 músicas populares**: Bohemian Rhapsody, Hotel California, Evidências, etc.
- **1 usuário administrador** padrão

## 🧪 Testar a API

Use o Swagger UI em `http://localhost:5062/swagger` ou:

```bash
# Verificar endpoints
curl http://localhost:5062/api/songs/search?query=rock
curl http://localhost:5062/api/events/upcoming?count=5
```

## 🚀 Deploy

### Estrutura para Deploy

A API está pronta para deploy em qualquer provedor cloud:

- **Railway**: Configuração atual
- **Azure**: App Service compatível  
- **AWS**: ECS/Lambda ready
- **Docker**: Dockerfile incluído

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma feature branch (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -am 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

## 🛣️ Próximos Passos

- [ ] **Autenticação JWT** completa
- [ ] **Upload de arquivos** (imagens, áudio)
- [ ] **Notificações em tempo real** (SignalR)
- [ ] **Sistema de cache** (Redis)
- [ ] **Testes de integração**
- [ ] **Rate limiting**

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.

---

<div align="center">

**🎵 Desenvolvido com ❤️ para a comunidade musical brasileira 🇧🇷**

⭐ **Se este projeto te ajudou, considere dar uma estrela!** ⭐

</div>
```

## ⚙️ Configuração

### 1. Configurar Banco de Dados

O projeto está configurado para usar o **Railway MySQL**. A string de conexão já está configurada no `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SEU_HOST;Port=PORTA;Database=NOME_DB;User=USUARIO;Password=SUA_SENHA"
  }
}
```

### 2. Executar Migrations

As migrations já foram aplicadas no banco Railway. Para executar novamente se necessário:

```bash
cd backend-api/PediMix.Infrastructure
dotnet ef database update
```

### 3. Popular Dados Iniciais (Opcional)

Execute o script SQL para inserir dados básicos no Railway:

**Opção 1 - Script PowerShell (Windows):**
```powershell
cd database
.\populate-railway.ps1
```

**Opção 2 - Comando direto:**
```bash
mysql -h SEU_HOST -u root -p"SUA_SENHA" --port PORTA --protocol=TCP railway < database/seed-data.sql
```

## 🚀 Como Executar

### Desenvolvimento

1. Navegue até a pasta da API:
```bash
cd backend-api/PediMix.API
```

2. Execute o projeto:
```bash
dotnet run
```

3. Acesse a documentação Swagger:
```
https://localhost:7139/swagger
```

### Compilação para Produção

```bash
cd backend-api/PediMix.API
dotnet publish -c Release -o ./publish
```

## 📊 Modelo de Dados

### Principais Entidades

- **User**: Usuários do sistema (Audience, Singer, Venue, Admin)
- **ArtistProfile**: Perfil de artistas com gêneros musicais
- **VenueProfile**: Perfil de locais com endereço e comodidades
- **Song**: Músicas com metadados (gênero, duração, dificuldade, etc.)
- **Repertoire**: Repertórios de artistas com suas músicas
- **Event**: Eventos musicais com datas, locais e artistas
- **SongRequest**: Pedidos de música para eventos
- **Genre**: Gêneros musicais
- **Amenity**: Comodidades dos locais

### Relacionamentos

- Relacionamentos many-to-many entre entidades usando tabelas intermediárias
- Cascade delete configurado apropriadamente
- Índices otimizados para consultas frequentes

## 🔌 Endpoints da API

### Usuários
- `GET /api/users/{id}` - Buscar usuário por ID
- `GET /api/users/email/{email}` - Buscar usuário por email
- `POST /api/users` - Criar novo usuário
- `PUT /api/users/{id}` - Atualizar usuário

### Músicas
- `GET /api/songs/genre/{genreId}` - Listar músicas por gênero
- `GET /api/songs/search?query=termo` - Buscar músicas
- `POST /api/songs` - Adicionar nova música

### Eventos
- `GET /api/events/{id}` - Buscar evento por ID
- `GET /api/events/upcoming?count=10` - Próximos eventos
- `GET /api/events/date-range?startDate=&endDate=` - Eventos por período
- `POST /api/events` - Criar novo evento

### Pedidos de Música
- `GET /api/songrequests/event/{eventId}?pendingOnly=false` - Pedidos por evento
- `POST /api/songrequests` - Fazer pedido de música

### Repertórios
- `GET /api/repertoires/{id}/songs` - Repertório com músicas
- `GET /api/repertoires/artist/{artistId}?activeOnly=false` - Repertórios do artista

## 🧪 Testando a API

### Exemplo de Criação de Usuário

```bash
curl -X POST "https://localhost:7139/api/users" \
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

### Exemplo de Busca de Músicas

```bash
curl -X GET "https://localhost:7139/api/songs/search?query=rock"
```

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

## 🏗️ Estrutura CQRS

### Commands (Escritas)
- `CreateUserCommand`
- `UpdateUserCommand`
- `CreateSongCommand`
- `CreateEventCommand`
- `CreateSongRequestCommand`

### Queries (Leituras)
- `GetUserByIdQuery`
- `GetUserByEmailQuery`
- `GetSongsByGenreQuery`
- `SearchSongsQuery`
- `GetEventsByDateRangeQuery`
- `GetUpcomingEventsQuery`

### Handlers

Cada Command e Query possui um Handler correspondente que implementa a lógica de negócio utilizando o padrão Repository e Unit of Work.

## 📝 Próximos Passos

1. **Autenticação JWT**: Implementar sistema de login e autorização
2. **Validações**: Adicionar FluentValidation para validar requests
3. **Logs Estruturados**: Implementar Serilog
4. **Testes**: Criar testes unitários e de integração
5. **Cache**: Implementar Redis para cache de consultas frequentes
6. **Background Jobs**: Hangfire para tarefas em background
7. **File Upload**: Sistema para upload de imagens e áudios

## 🤝 Contribuição

1. Fork o projeto
2. Crie uma feature branch (`git checkout -b feature/nova-funcionalidade`)
3. Commit suas mudanças (`git commit -am 'Adiciona nova funcionalidade'`)
4. Push para a branch (`git push origin feature/nova-funcionalidade`)
5. Abra um Pull Request

## 📄 Licença

Este projeto está sob a licença MIT. Veja o arquivo LICENSE para mais detalhes.

---

**Desenvolvido com ❤️ para a comunidade musical brasileira** 🎵🇧🇷
