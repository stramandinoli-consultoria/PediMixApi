# PEDIMIX - Guia de Integracao React com API .NET 6

Este guia mostra como integrar o frontend React com a API do PediMix de forma padrao, reutilizavel e pronta para escalar em todas as telas mapeadas no checklist.

## 1) Objetivo

- Padronizar chamadas HTTP em um unico client
- Tratar autenticacao com access token + refresh token
- Reaproveitar services/hooks por dominio
- Cobrir todas as telas mapeadas no arquivo API_METODOS_DOTNET6_CHECKLIST.md

## 2) Base URL e versao da API

No backend atual, os endpoints principais estao em `/api/v1`.

Exemplo:
- `GET /api/v1/addresses/cep/{cep}`

No frontend, sempre use uma base URL configuravel por ambiente.

Arquivo `.env` (React):

```env
VITE_API_BASE_URL=http://localhost:5062
```

## 3) Estrutura sugerida no React

```txt
src/
  api/
    httpClient.ts
    authToken.ts
    endpoints.ts
  services/
    auth.service.ts
    users.service.ts
    artists.service.ts
    venues.service.ts
    events.service.ts
    repertoires.service.ts
    songs.service.ts
    songRequests.service.ts
    admin.service.ts
    notifications.service.ts
    uploads.service.ts
    address.service.ts
  hooks/
    useAuth.ts
    useUserHome.ts
    useWeekSchedule.ts
    useLiveRequests.ts
  types/
    api.types.ts
    auth.types.ts
```

## 4) Contrato padrao de resposta

A API retorna envelope padrao:

```ts
export type ApiResponse<T> = {
  success: boolean;
  data: T;
  message: string;
  errors: string[];
  timestamp: string;
};
```

## 5) HTTP Client unico (Axios)

Instale:

```bash
npm i axios
```

Arquivo `src/api/httpClient.ts`:

```ts
import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export const http = axios.create({
  baseURL: API_BASE_URL,
  timeout: 20000,
  headers: { 'Content-Type': 'application/json' }
});

let isRefreshing = false;
let refreshQueue: Array<(token: string | null) => void> = [];

function getAccessToken() {
  return localStorage.getItem('accessToken');
}

function getRefreshToken() {
  return localStorage.getItem('refreshToken');
}

function setTokens(accessToken: string, refreshToken: string) {
  localStorage.setItem('accessToken', accessToken);
  localStorage.setItem('refreshToken', refreshToken);
}

function clearTokens() {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
}

http.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = getAccessToken();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

http.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    if (!original) throw error;

    const status = error.response?.status;
    if (status !== 401 || original._retry) throw error;

    const refreshToken = getRefreshToken();
    if (!refreshToken) {
      clearTokens();
      throw error;
    }

    if (isRefreshing) {
      const newToken = await new Promise<string | null>((resolve) => {
        refreshQueue.push(resolve);
      });
      if (!newToken) throw error;
      original.headers.Authorization = `Bearer ${newToken}`;
      return http(original);
    }

    original._retry = true;
    isRefreshing = true;

    try {
      const refreshResp = await axios.post(`${API_BASE_URL}/api/v1/auth/refresh-token`, {
        refreshToken
      });

      const payload = refreshResp.data?.data;
      const newAccessToken = payload?.accessToken;
      const newRefreshToken = payload?.refreshToken;

      if (!newAccessToken || !newRefreshToken) {
        clearTokens();
        refreshQueue.forEach((notify) => notify(null));
        refreshQueue = [];
        throw error;
      }

      setTokens(newAccessToken, newRefreshToken);
      refreshQueue.forEach((notify) => notify(newAccessToken));
      refreshQueue = [];

      original.headers.Authorization = `Bearer ${newAccessToken}`;
      return http(original);
    } catch (e) {
      clearTokens();
      refreshQueue.forEach((notify) => notify(null));
      refreshQueue = [];
      throw e;
    } finally {
      isRefreshing = false;
    }
  }
);
```

## 6) Endpoints centralizados

Arquivo `src/api/endpoints.ts`:

```ts
export const API = {
  auth: {
    register: '/api/v1/auth/register',
    login: '/api/v1/auth/login',
    refresh: '/api/v1/auth/refresh-token',
    logout: '/api/v1/auth/logout',
    logoutAll: '/api/v1/auth/logout-all',
    me: '/api/v1/auth/me'
  },
  users: {
    me: '/api/v1/users/me',
    completeProfile: '/api/v1/users/me/complete-profile',
    profileCompletion: '/api/v1/users/me/profile-completion',
    preferences: '/api/v1/users/me/preferences',
    favorites: '/api/v1/users/me/favorites',
    address: '/api/v1/users/me/address'
  },
  addresses: {
    cep: (cep: string) => `/api/v1/addresses/cep/${cep}`
  },
  artists: {
    list: '/api/v1/artists',
    byId: (id: string) => `/api/v1/artists/${id}`,
    dashboard: '/api/v1/artists/me/dashboard',
    weeklyStats: '/api/v1/artists/me/weekly-stats',
    feedbacks: '/api/v1/artists/me/feedbacks',
    repertoire: (id: string) => `/api/v1/artists/${id}/repertoire`
  },
  venues: {
    list: '/api/v1/venues',
    nearby: '/api/v1/venues/nearby',
    profile: '/api/v1/venues/me/profile'
  },
  events: {
    list: '/api/v1/events',
    byId: (id: string) => `/api/v1/events/${id}`,
    week: '/api/v1/events/week',
    reviews: (id: string) => `/api/v1/events/${id}/reviews`,
    status: (id: string) => `/api/v1/events/${id}/status`
  },
  repertoires: {
    list: '/api/v1/repertoires',
    byId: (id: string) => `/api/v1/repertoires/${id}`,
    songs: (id: string) => `/api/v1/repertoires/${id}/songs`
  },
  songs: {
    list: '/api/v1/songs',
    byId: (id: string) => `/api/v1/songs/${id}`
  },
  songRequests: {
    create: '/api/v1/song-requests',
    live: (artistId: string) => `/api/v1/song-requests/live/${artistId}`,
    byEvent: (eventId: string) => `/api/v1/song-requests/event/${eventId}`,
    accept: (id: string) => `/api/v1/song-requests/${id}/accept`,
    decline: (id: string) => `/api/v1/song-requests/${id}/decline`,
    play: (id: string) => `/api/v1/song-requests/${id}/play`
  },
  admin: {
    users: '/api/v1/admin/users',
    reports: '/api/v1/admin/reports'
  },
  notifications: {
    me: '/api/v1/notifications/me',
    readAll: '/api/v1/notifications/read-all'
  }
};
```

## 7) Exemplo de service por dominio

Arquivo `src/services/address.service.ts`:

```ts
import { http } from '../api/httpClient';
import { API } from '../api/endpoints';
import { ApiResponse } from '../types/api.types';

export type ViaCepDto = {
  cep: string;
  street: string;
  complement: string;
  neighborhood: string;
  city: string;
  state: string;
  ibge: string;
  gia: string;
  ddd: string;
  siafi: string;
};

export async function getAddressByCep(cep: string) {
  const { data } = await http.get<ApiResponse<ViaCepDto>>(API.addresses.cep(cep));
  return data;
}
```

Uso na tela (ex.: CompleteProfile):

```ts
const onBlurCep = async (cep: string) => {
  const resp = await getAddressByCep(cep);
  if (!resp.success || !resp.data) return;

  setValue('street', resp.data.street || '');
  setValue('neighborhood', resp.data.neighborhood || '');
  setValue('city', resp.data.city || '');
  setValue('state', resp.data.state || '');
};
```

## 8) Mapeamento tela -> chamadas React

### Publicas

- Home
  - `events.service.list()`
  - `artists.service.list({ trending: true })`
- Explore
  - `events.service.list(filters)`
- EventDetails
  - `events.service.getById(id)`
  - `events.service.getReviews(id)`

### Auth

- Login/Cadastro
  - `auth.service.login(payload)`
  - `auth.service.register(payload)`
  - `auth.service.refreshToken()`
  - `auth.service.logout()`
  - `auth.service.me()`

### Usuario

- UserHome
  - `users.service.getMe()`
  - `venues.service.nearby({ lat, lng, radius })`
  - `artists.service.list({ featured: true })`
  - `users.service.getFavorites(type)`
  - `users.service.addFavorite(payload)`
  - `users.service.removeFavorite(favoriteId)`
- SearchPage
  - `venues.service.list({ query })`
  - `artists.service.list({ query })`
- WeekSchedule
  - `events.service.week()`
  - `artists.service.getRepertoire(artistId)`
  - `songRequests.service.create(payload)`
- CompleteProfile
  - `users.service.completeProfile(payload)`
  - `users.service.profileCompletion()`
  - `address.service.getAddressByCep(cep)`

### Artista

- ArtistHome
  - `artists.service.dashboard()`
  - `artists.service.weeklyStats()`
  - `artists.service.feedbacks()`
  - `events.service.patchStatus(eventId, payload)`
- RepertoireManager
  - `repertoires.service.list()`
  - `repertoires.service.create()`
  - `repertoires.service.update(id)`
  - `repertoires.service.remove(id)`
  - `repertoires.service.addSong(repertoireId, payload)`
  - `repertoires.service.removeSong(repertoireId, songId)`
  - `songs.service.list({ query })`
- LiveRequests
  - `songRequests.service.live(artistId)`
  - `songRequests.service.accept(id)`
  - `songRequests.service.decline(id)`
  - `songRequests.service.play(id)`

### Estabelecimento

- EstablishmentHome/Profile/Menu/Schedule
  - `venues.service.getMyProfile()`
  - `venues.service.updateMyProfile(payload)`
  - `venues.service.getMenu(venueId)`
  - `venues.service.updateMenu(venueId, payload)`
  - `events.service.list({ venueId })`
  - `events.service.create(payload)`
  - `events.service.update(eventId, payload)`

### Admin

- Moderacao/Usuarios/Relatorios
  - `admin.service.listUsers(filters)`
  - `admin.service.patchUserStatus(userId, payload)`
  - `admin.service.listModerationComments()`
  - `admin.service.approveComment(commentId)`
  - `admin.service.rejectComment(commentId)`
  - `admin.service.reports(filters)`

## 9) Exemplo de service de Auth

Arquivo `src/services/auth.service.ts`:

```ts
import { http } from '../api/httpClient';
import { API } from '../api/endpoints';

export async function login(payload: { email: string; password: string }) {
  const { data } = await http.post(API.auth.login, payload);
  const auth = data?.data;

  if (auth?.accessToken) localStorage.setItem('accessToken', auth.accessToken);
  if (auth?.refreshToken) localStorage.setItem('refreshToken', auth.refreshToken);

  return data;
}

export async function logout() {
  const refreshToken = localStorage.getItem('refreshToken');
  await http.post(API.auth.logout, { refreshToken });
  localStorage.removeItem('accessToken');
  localStorage.removeItem('refreshToken');
}
```

## 10) Realtime (LiveRequests)

Para a tela LiveRequests, manter dois canais ativos:

- Carga inicial via REST (`GET /api/v1/song-requests/live/{artistId}`)
- Atualizacao em tempo real via SignalR/WebSocket

Eventos esperados:
- `songRequestCreated`
- `songRequestUpdated`
- `songRequestVoted`
- `showStatusChanged`
- `nowPlayingChanged`

## 11) Boas praticas para incluir em todas as telas

- Sempre chamar endpoints via `services` (nunca direto no componente)
- Tratar `success=false` com mensagem de negocio
- Tratar `401` globalmente no interceptor
- Normalizar erros para um formato unico no frontend
- Usar React Query para cache, loading e invalidacao
- Criar chaves de cache por dominio (`['events', filters]`, `['user','me']`)

## 12) Checklist rapido de rollout no frontend

1. Criar `httpClient.ts` com interceptor de refresh token.
2. Centralizar rotas em `endpoints.ts` com prefixo `/api/v1`.
3. Criar services por dominio (auth, users, events, etc.).
4. Trocar chamadas mock das telas pelos services.
5. Ligar tela CompleteProfile no endpoint de CEP.
6. Ligar tela LiveRequests em REST + SignalR.
7. Revisar guardas de rota por perfil (AUDIENCE, SINGER, VENUE, ADMIN).
8. Validar expiracao de sessao de 2h com logout automatico.

---

Se quiser, no proximo passo eu posso gerar os arquivos base de `src/api`, `src/services` e `src/hooks` ja prontos para colar no seu frontend.
