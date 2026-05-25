# PediMix — Integração com APIs Musicais

Implementação do `GUIA_INTEGRACAO_MUSIC_APIS.md` respeitando a Clean Architecture existente
(Domain / Application / Infrastructure / API), mantendo MediatR + AutoMapper + EF Core + MySQL.

---

## 1. O que foi adicionado

### Domain (`PediMix.Domain`)
- `Entities/MusicEntities.cs` — `SongExternalData`, `SongLyrics`, `ExternalArtist`

### Application (`PediMix.Application`)
- `DTOs/MusicDtos.cs` — `SpotifyTrackDto`, `SpotifyArtistDto`, `LyricsDto`, `SyncedLineDto`, `YouTubeVideoDto`, `MusicSearchResultDto`
- `Interfaces/IMusicServices.cs` — `ISpotifyService`, `ILyricsService`, `IYouTubeService`, `IMusicCacheService`
- `Interfaces/IRepositories.cs` — acréscimo de `ISongExternalDataRepository`, `ISongLyricsRepository`, `IExternalArtistRepository` no `IUnitOfWork`

### Infrastructure (`PediMix.Infrastructure`)
- `Services/SpotifyService.cs` — Client Credentials Flow + cache + retry/circuit-breaker via Polly
- `Services/LyricsService.cs` — Lyrically → Vagalume fallback, cache 30 dias
- `Services/YouTubeService.cs` — YouTube Data API v3, cache 24h (economiza quota)
- `Services/MusicCacheService.cs` — cache híbrido **Redis com fallback para IMemoryCache**
- `Policies/HttpPolicies.cs` — Retry exponencial (2/4/8s) + Circuit Breaker (5 falhas → 30s)
- `Configurations/MusicEntityConfigurations.cs` — EF Core mapping (índices, FKs, tipos MySQL)
- `Repositories/MusicRepositories.cs` — repositórios concretos das novas entidades
- `Repositories/SpecificRepositories.cs` — UnitOfWork expondo os novos repositórios
- `Data/PediMixDbContext.cs` — DbSets + ApplyConfiguration das novas entidades

### API (`PediMix.API`)
- `Controllers/MusicV1Controller.cs` — 3 controllers:
  - `MusicV1Controller` em `/api/v1/music`
  - `ArtistSearchV1Controller` em `/api/v1/artist-search`
  - `YouTubeV1Controller` em `/api/v1/youtube`
- `Program.cs` — registro de Redis opcional + IMemoryCache + HttpClients tipados com Polly
- `appsettings.json` — seções `Spotify`, `YouTube`, `Vagalume`, `Redis`

### Frontend (`PediMix/frontend-web`)
- `src/api/endpoints.ts` — `music`, `artistSearch`, `youtube`
- `src/services/music.service.ts` — wrapper TS tipado para todos os endpoints novos

---

## 2. NuGet packages adicionados

Em `PediMix.Infrastructure.csproj`:

```xml
<PackageReference Include="StackExchange.Redis" Version="2.7.33" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="6.0.36" />
<PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="6.0.2" />
<PackageReference Include="Polly" Version="7.2.4" />
<PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
<PackageReference Include="Microsoft.Extensions.Http.Polly" Version="6.0.36" />
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="6.0.4" />
<PackageReference Include="Microsoft.Extensions.Configuration.Abstractions" Version="6.0.0" />
<PackageReference Include="Microsoft.Extensions.Http" Version="6.0.1" />
```

Rode `dotnet restore` na raiz do `PediMixApi`.

---

## 3. Migration (você roda manualmente)

As entidades, configurations e DbContext já estão prontos. Gere a migration com:

```bash
cd PediMixApi
dotnet ef migrations add AddMusicIntegrationEntities --project PediMix.Infrastructure --startup-project PediMix.API
dotnet ef database update --project PediMix.Infrastructure --startup-project PediMix.API
```

Se o EF Core CLI não estiver instalado:
```bash
dotnet tool install --global dotnet-ef --version 6.0.21
```

A migration vai criar 3 tabelas:
- `SongExternalData` (1:1 com `Song` — `SongId` é UNIQUE)
- `SongLyrics` (N:1 com `Song`)
- `ExternalArtists` (independente, `SpotifyId` UNIQUE)

---

## 4. Variáveis de ambiente (Railway)

Adicione no painel do Railway:

```env
# Spotify Web API (https://developer.spotify.com/dashboard)
Spotify__ClientId=SEU_CLIENT_ID
Spotify__ClientSecret=SEU_CLIENT_SECRET

# YouTube Data API v3 (https://console.cloud.google.com)
YouTube__ApiKey=SUA_API_KEY

# Vagalume (https://api.vagalume.com.br) — opcional, só fallback BR
Vagalume__ApiKey=SUA_API_KEY

# Redis — opcional. Se ausente, cai automaticamente em IMemoryCache.
Redis__ConnectionString=redis://default:senha@host:porta
# alternativa: REDIS_URL ou REDIS_CONNECTION_STRING
```

> **Importante:** Railway oferece Redis como addon. Adicione o plugin "Redis" no projeto;
> a connection string ficará disponível em `REDIS_URL`. O `Program.cs` já lê essa env
> automaticamente, então basta provisionar o addon.

Para desenvolvimento local, edite `appsettings.json` ou use `dotnet user-secrets`.

---

## 5. Endpoints novos

| Método | Rota                                          | Descrição                                 |
|--------|-----------------------------------------------|-------------------------------------------|
| GET    | `/api/v1/music/search?query=`                 | Busca tracks + artistas (Spotify)         |
| GET    | `/api/v1/music/tracks?query=&limit=20`        | Só tracks                                 |
| GET    | `/api/v1/music/lyrics?artist=&title=`         | Letra (Lyrically → Vagalume)              |
| GET    | `/api/v1/artist-search/search?query=&limit=`  | Artistas no Spotify                       |
| GET    | `/api/v1/artist-search/{spotifyId}/top-tracks`| Top tracks do artista                     |
| GET    | `/api/v1/youtube/search?query=&type=clip`     | Vídeos (`type`: `clip`, `lyric`, `live`)  |

Todos seguem o padrão `ApiResponse<T>` existente. Veja em Swagger (`/swagger`).

---

## 6. Estratégia de cache (TTL)

| Tipo            | Chave                                  | TTL      |
|-----------------|----------------------------------------|----------|
| Busca tracks    | `spotify:tracks:{query}:{limit}`       | 1 dia    |
| Busca artistas  | `spotify:artists:{query}:{limit}`      | 7 dias   |
| Top tracks      | `spotify:artist:toptracks:{spotifyId}` | 7 dias   |
| Letras          | `lyrics:{artist}:{title}`              | 30 dias  |
| YouTube search  | `youtube:{type}:{query}:{max}`         | 1 dia    |

Estratégia: **cache-aside** (try cache → miss → busca API → grava cache).
Falha silenciosa: se Redis cair, o `MusicCacheService` marca como indisponível por 30s
e usa IMemoryCache. A API nunca quebra por causa do cache.

---

## 7. Resiliência (Polly)

Os 3 HttpClients (`SpotifyService`, `LyricsService`, `YouTubeService`) têm:
- **Retry**: 3 tentativas com backoff exponencial (2s, 4s, 8s), trata 5xx, 408 e 429
- **Circuit Breaker**: abre após 5 falhas consecutivas, fica 30s aberto
- **Timeout**: 15s por requisição (configurado no `AddHttpClient`)

---

## 8. Frontend — como usar

```ts
import { musicService } from '../services/music.service';

// Busca geral
const result = await musicService.searchMusic('Jorge Henrique');

// Só tracks
const tracks = await musicService.searchTracks('saudade', 20);

// Letra
const lyrics = await musicService.getLyrics('Jorge Henrique', 'Amor de Verão');
if (lyrics) {
  console.log(lyrics.content, lyrics.source, lyrics.fromCache);
}

// Top tracks de artista
const top = await musicService.getArtistTopTracks(spotifyArtistId);

// YouTube
const videos = await musicService.searchYouTubeVideos(
  'Jorge Henrique Amor de Verão',
  'clip',
  3,
);
```

---

## 9. Plano de migração futura para PostgreSQL

Já que o projeto vai migrar para Postgres no futuro, anota:

1. **Trocar package**: remover `Pomelo.EntityFrameworkCore.MySql`, instalar `Npgsql.EntityFrameworkCore.PostgreSQL 6.0.x`.
2. **Trocar `UseMySql` no `Program.cs`** por `UseNpgsql`.
3. **Connection string Railway**: ler de `DATABASE_URL` (Railway expõe Postgres assim).
   Precisa parsear `postgres://user:pass@host:port/db` para o formato Npgsql (`Host=...;Port=...;...`).
4. **`longtext` → `text`**: a `MusicEntityConfigurations.cs` (e demais existentes) usa
   `HasColumnType("longtext")`. Trocar por `"text"` no Postgres.
5. **Migrations**: rodar `dotnet ef migrations add InitialPostgresMigrate` em outro banco
   ou apagar migrations atuais e recriar do zero (recomendado em dev).
6. **Índices full-text**: aproveitar para adicionar `GIN` indexes em `Song.Title`, `Song.Artist`,
   `ExternalArtist.Name` para acelerar buscas.

Posso fazer esse trabalho em uma iteração separada quando você decidir migrar.

---

## 10. Próximos passos sugeridos

1. Configurar credenciais Spotify/YouTube/Vagalume no Railway.
2. (Opcional) Adicionar addon Redis no Railway.
3. Rodar a migration (`dotnet ef database update`).
4. Testar em `/swagger`:
   - `GET /api/v1/music/search?query=jorge%20henrique`
   - `GET /api/v1/music/lyrics?artist=Legi%C3%A3o%20Urbana&title=Tempo%20Perdido`
5. Construir as telas no React usando o `musicService` (busca de músicas para repertório,
   tela de letra/karaokê, perfil de artista externo, etc.).
6. Quando integrar IA (cifras, BPM, tom): adicionar `IMusicAiService` no
   `IMusicServices.cs` e novo service em Infrastructure (ACRCloud, AudD, ML.NET).
