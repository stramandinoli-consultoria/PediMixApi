# PEDIMIX - Checklist de Metodos da API (.NET 6)
## Todos os campos JSON por endpoint, mapeados por tela

> **Base URL:** `http://localhost:5062`  
> **Prefixo:** `/api/v1/`  
> **Autorizacao:** `Authorization: Bearer {accessToken}`  
> **Resposta padrao:** `{ success, data, message, errors[], timestamp }`

---

## ENUMS

### UserRole
| Valor | Papel |
|-------|-------|
| 1 | AUDIENCE (usuario comum) |
| 2 | SINGER (artista) |
| 3 | VENUE (estabelecimento) |
| 4 | ADMIN |

### Gender
| Valor | Genero |
|-------|--------|
| 1 | Masculino |
| 2 | Feminino |
| 3 | Nao Binario |
| 4 | Prefiro nao informar |

### EventStatus
| Valor | Status |
|-------|--------|
| 1 | Draft |
| 2 | Published |
| 3 | Cancelled |
| 4 | Ongoing |
| 5 | Finished |
| 6 | Postponed |
| 7 | Outro |

### SongRequestStatus
| Valor | Status |
|-------|--------|
| 1 | Pending |
| 2 | Accepted |
| 3 | Declined |
| 4 | Played |

### EventCategory / EventVisibility / SongDifficulty / VenueType
- EventCategory: 1 a 10
- EventVisibility: 1=Public, 2=Private, 3=FriendsOnly
- SongDifficulty: 1=Easy, 2=Medium, 3=Hard
- VenueType: 1 a 9

---

## 1) AUTENTICACAO E SESSAO

### POST /api/v1/auth/register
**Tela:** Login/Cadastro

**Request Body:**
```json
{
  "email": "string",
  "password": "string",
  "confirmPassword": "string",
  "username": "string",
  "firstName": "string",
  "lastName": "string",
  "role": "AUDIENCE",
  "acceptTerms": true,
  "acceptPrivacy": true
}
```
> role: "AUDIENCE" | "SINGER" | "VENUE" (string, nao inteiro)  
> username: enviado como email do usuario  
> firstName: primeiro nome, lastName: restante do nome completo  
> A API deve aceitar **firstName/lastName** (nao nome) e **role como string**

**Response 200:**
```json
{
  "success": true,
  "data": {
    "user": {
      "id": "uuid",
      "email": "string",
      "username": "string",
      "firstName": "string",
      "lastName": "string",
      "avatar": "string",
      "bio": "string",
      "phoneNumber": "string",
      "dateOfBirth": "datetime",
      "gender": 1,
      "role": 1,
      "isEmailVerified": false,
      "isPhoneVerified": false,
      "isActive": true,
      "lastLoginAt": "datetime",
      "address": {
        "street": "string",
        "number": "string",
        "complement": "string",
        "neighborhood": "string",
        "city": "string",
        "state": "string",
        "zipCode": "string",
        "country": "string"
      },
      "preferences": null,
      "artistProfile": null,
      "venueProfile": null
    },
    "accessToken": "string",
    "refreshToken": "string",
    "expiresIn": 900,
    "refreshExpiresIn": 7200
  },
  "message": null,
  "errors": [],
  "timestamp": "datetime"
}
```

---

### POST /api/v1/auth/login
**Tela:** Login/Cadastro

**Request Body:**
```json
{
  "email": "string",
  "password": "string"
}
```

**Response 200:** (mesmo formato de register acima)

---

### POST /api/v1/auth/refresh-token
**Uso:** Interceptor HTTP automatico

**Request Body:**
```json
{
  "refreshToken": "string"
}
```

**Response 200:** (mesmo formato de login)

---

### POST /api/v1/auth/logout
**Tela:** Qualquer tela com logout

**Request Body:**
```json
{
  "refreshToken": "string"
}
```

**Response 200:**
```json
{
  "success": true,
  "data": null,
  "message": "Logout realizado",
  "errors": [],
  "timestamp": "datetime"
}
```

---

### POST /api/v1/auth/logout-all
Sem body. Invalida todos os tokens do usuario.

---

### GET /api/v1/auth/me
**Uso:** Restaurar sessao ao abrir o app

**Response 200 - UserDtoApiResponse:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "email": "string",
    "username": "string",
    "firstName": "string",
    "lastName": "string",
    "avatar": "string",
    "bio": "string",
    "phoneNumber": "string",
    "dateOfBirth": "datetime",
    "gender": 1,
    "role": 1,
    "isEmailVerified": false,
    "isPhoneVerified": false,
    "isActive": true,
    "lastLoginAt": "datetime",
    "address": {
      "street": "string",
      "number": "string",
      "complement": "string",
      "neighborhood": "string",
      "city": "string",
      "state": "string",
      "zipCode": "string",
      "country": "string"
    },
    "preferences": {
      "theme": "string",
      "language": "string",
      "emailNotifications": true,
      "pushNotifications": true,
      "smsNotifications": false,
      "eventReminders": true,
      "newFollowers": true,
      "eventUpdates": true,
      "profileVisible": true,
      "showLocation": true,
      "allowDirectMessages": true
    },
    "artistProfile": null,
    "venueProfile": null
  }
}
```

---

### POST /api/v1/auth/forgot-password
```json
{ "email": "string" }
```

### POST /api/v1/auth/reset-password
```json
{ "email": "string", "token": "string", "newPassword": "string" }
```

### POST /api/v1/auth/verify-email
```json
{ "email": "string", "token": "string" }
```

### POST /api/v1/auth/resend-verification
```json
{ "email": "string" }
```

---

## 2) USUARIO E PERFIL

### GET /api/v1/users/me
**Tela:** UserHome e outras

Sem body. Response: UserDtoApiResponse (mesmo formato de auth/me acima)

---

### PUT /api/v1/users/me  /  PATCH /api/v1/users/me
**Tela:** Editar perfil

**Request Body - UpdateUserCommand:**
```json
{
  "id": "uuid",
  "username": "string",
  "email": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string"
}
```

---

### PUT /api/v1/users/me/complete-profile
**Tela:** CompleteProfile - Finalizar Cadastro (3 etapas: Pessoal, Endereco, Contato)

> O frontend envia TUDO em um unico PUT ao final das 3 etapas (nao usa endpoint separado de address).

**Request Body - CompleteProfileRequest:**
```json
{
  "cpf": "string",
  "birthDate": "2000-01-01T00:00:00Z",
  "gender": "masculino",
  "address": {
    "cep": "string",
    "street": "string",
    "number": "string",
    "complement": "string",
    "neighborhood": "string",
    "city": "string",
    "state": "string"
  },
  "contact": {
    "phone": "string",
    "whatsapp": "string",
    "emergencyContact": "string",
    "emergencyPhone": "string"
  }
}
```
> gender: "masculino" | "feminino" | "outro" | "prefiro-nao-informar" (string, nao inteiro)  
> O campo **bio** NAO existe no formulario do frontend  
> O endereco usa **cep** (nao zipCode), sem campo country  
> Contato usa objeto **contact** com phone, whatsapp, emergencyContact, emergencyPhone  

---

### PUT /api/v1/users/me/address
**Uso:** Endpoint separado (nao usado pelo CompleteProfile — o frontend envia address dentro do complete-profile acima)

**Request Body - AddressDto (para uso futuro em edicao de perfil):**
```json
{
  "cep": "string",
  "street": "string",
  "number": "string",
  "complement": "string",
  "neighborhood": "string",
  "city": "string",
  "state": "string"
}
```
> Usar **cep** (nao zipCode). Sem campo country.

---

### POST /api/v1/users/me/avatar
**Tela:** Foto de perfil

**Request Body - AvatarRequest:**
```json
{
  "avatarUrl": "string"
}
```

### DELETE /api/v1/users/me/avatar
Sem body.

---

### GET /api/v1/users/me/profile-completion
**Tela:** UserHome - banner "Finalizar Cadastro"

Retorna percentual/status de completude (estrutura definida pela API).

---

### GET /api/v1/users/me/preferences / PUT /api/v1/users/me/preferences
**Tela:** Configuracoes

**Body do PUT - UserPreferencesDto:**
```json
{
  "theme": "dark",
  "language": "pt-BR",
  "emailNotifications": true,
  "pushNotifications": true,
  "smsNotifications": false,
  "eventReminders": true,
  "newFollowers": true,
  "eventUpdates": true,
  "profileVisible": true,
  "showLocation": true,
  "allowDirectMessages": true
}
```

---

### GET /api/v1/users/me/favorites?type=
**Tela:** UserHome - favoritos

**Query:** type = "artist" | "venue" | "event"

**Response 200:**
```json
{
  "success": true,
  "data": [
    { "id": "uuid", "type": "artist", "refId": "uuid", "createdAt": "datetime" }
  ]
}
```

---

### POST /api/v1/users/me/favorites
**Request Body - AddFavoriteRequest:**
```json
{
  "type": "artist",
  "refId": "uuid-do-artista-ou-venue"
}
```

---

### DELETE /api/v1/users/me/favorites/{favoriteId}
Sem body. Usar o `id` do objeto favorito (nao o `refId`).

---

## 3) CEP / ENDERECO

### GET /api/v1/addresses/cep/{cep}
**Tela:** CompleteProfile - preencher endereco automaticamente

**Path Param:** cep como string sem pontuacao (ex: `01310100`)

**Response 200:**
```json
{
  "success": true,
  "data": {
    "cep": "01310-100",
    "logradouro": "Avenida Paulista",
    "complemento": "",
    "bairro": "Bela Vista",
    "localidade": "Sao Paulo",
    "uf": "SP",
    "ibge": "string",
    "gia": "string",
    "ddd": "11",
    "siafi": "string",
    "erro": false
  }
}
```

> Mapeamento: logradouro -> street, bairro -> neighborhood, localidade -> city, uf -> state, cep -> zipCode

---

## 4) ARTISTAS

### GET /api/v1/artists?query=
**Tela:** SearchPage, UserHome

**Query Params:** query (string, opcional)

**Response 200:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "userId": "uuid",
      "stageName": "string",
      "description": "string",
      "isVerified": false,
      "followers": 0,
      "totalEvents": 0,
      "rating": 4.5,
      "instagramUrl": "string",
      "youtubeUrl": "string",
      "spotifyUrl": "string",
      "soundcloudUrl": "string",
      "tiktokUrl": "string",
      "genres": [{ "id": "uuid", "name": "Rock", "color": "#ff0000", "description": "" }],
      "repertoires": []
    }
  ]
}
```

---

### GET /api/v1/artists/{id}
Retorna ArtistProfileApiResponse com ArtistProfile completo.

### GET /api/v1/artists/{id}/events
Retorna EventIEnumerableApiResponse.

### GET /api/v1/artists/{id}/repertoire
Retorna RepertoireIEnumerableApiResponse com songs (ver schema de repertoires abaixo).

### GET /api/v1/artists/{id}/ratings
Retorna ObjectApiResponse.

### POST /api/v1/artists/{id}/follow
Sem body.

### DELETE /api/v1/artists/{id}/follow
Sem body.

### GET /api/v1/artists/me/dashboard
**Tela:** ArtistHome

Retorna ObjectApiResponse (estrutura definida pela API).

### PUT /api/v1/artists/me/profile
**Request Body (objeto livre):**
```json
{
  "stageName": "string",
  "description": "string",
  "instagramUrl": "string",
  "youtubeUrl": "string",
  "spotifyUrl": "string",
  "soundcloudUrl": "string",
  "tiktokUrl": "string"
}
```

### Artist Complete Profile (campos detalhados da tela nova)
**Tela:** /artist/complete-profile

### PUT /api/v1/artists/me/complete-profile
**Tela:** /artist/complete-profile

**Bloco 1 - Info Basica:**
```json
{
  "stageName": "string",
  "fullName": "string",
  "birthDate": "2000-01-01",
  "phone": "string",
  "email": "string",
  "genre": "string",
  "city": "string",
  "state": "string",
  "bio": "string"
}
```

**Bloco 2 - Documentos:**
```json
{
  "cpf": "string",
  "rg": "string",
  "cnh": "string",
  "proofAddressUrl": "string",
  "profilePhotoUrl": "string",
  "documentFrontUrl": "string",
  "documentBackUrl": "string"
}
```

**Bloco 3 - Dados Bancarios:**
```json
{
  "bankName": "string",
  "bankCode": "string",
  "agency": "string",
  "account": "string",
  "accountType": "corrente",
  "holderName": "string",
  "holderDocument": "string",
  "pixKeyType": "cpf",
  "pixKey": "string"
}
```

**Bloco 4 - Redes Sociais:**
```json
{
  "instagram": "string",
  "youtube": "string",
  "spotify": "string",
  "tiktok": "string",
  "facebook": "string",
  "website": "string"
}
```

**Bloco 5 - Portfolio:**
```json
{
  "portfolioSummary": "string",
  "portfolioHighlights": "string",
  "portfolioLinks": "string",
  "pressKitUrl": "string",
  "demoVideoUrl": "string",
  "repertoireDocUrl": "string"
}
```

> Na aba Portfolio, o frontend permite enviar imagem ou abrir camera (selfie) para foto do artista.
> Essa imagem preenche o campo `profilePhotoUrl` (Bloco 2 - Documentos), sem criar novo campo de API.

**Bloco 6 - Equipamentos:**
```json
{
  "hasOwnSoundSystem": true,
  "hasOwnLighting": true,
  "bringsBand": false,
  "musiciansCount": "2",
  "instruments": "voz, violao",
  "technicalRider": "string",
  "transportInfo": "string",
  "setupTimeMinutes": "40"
}
```

> A tela envia o payload completo acima para `PUT /api/v1/artists/me/complete-profile`.
> O frontend tambem mantem um espelho local em `localStorage (artistCompleteProfileData)` como rascunho/cache.

### GET /api/v1/artists/me/weekly-stats
Retorna ObjectApiResponse.

### GET /api/v1/artists/me/feedbacks
Retorna ObjectApiResponse.

### GET/POST /api/v1/artists/{id}/reviews
**POST Body (objeto livre):**
```json
{
  "rating": 5,
  "comment": "string"
}
```

---

## 5) ESTABELECIMENTOS (VENUES)

### GET /api/v1/venues?query=
**Tela:** SearchPage, UserHome

**Response 200:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "userId": "uuid",
      "name": "string",
      "description": "string",
      "capacity": 500,
      "type": 1,
      "isVerified": false,
      "rating": 4.2,
      "totalEvents": 10,
      "phone": "string",
      "email": "string",
      "website": "string",
      "venueAddress": {
        "street": "string",
        "number": "string",
        "complement": "string",
        "neighborhood": "string",
        "city": "string",
        "state": "string",
        "zipCode": "string",
        "country": "string",
        "latitude": -23.5,
        "longitude": -46.6
      },
      "amenities": [
        { "id": "uuid", "name": "Estacionamento", "icon": "string", "description": "" }
      ]
    }
  ]
}
```

---

### GET /api/v1/venues/nearby?lat=&lng=&radius=
**Tela:** UserHome - estabelecimentos proximos

**Query Params:**
- lat (number)
- lng (number)
- radius (int, default 10, em km)

---

### PUT /api/v1/venues/me/profile
**Tela:** EstablishmentProfile (home/profile/menu)

**Request Body (frontend atual):**
```json
{
  "name": "string",
  "description": "string",
  "capacity": 500,
  "type": 1,
  "phone": "string",
  "email": "string",
  "website": "string",
  "street": "string",
  "number": "string",
  "neighborhood": "string",
  "city": "string",
  "state": "string",
  "zipCode": "string",
  "latitude": -23.5,
  "longitude": -46.6
}
```

> A tela de estabelecimento tambem possui:  
> - Cardapio por item com imagem  
> - Cardapio pronto (PDF/imagem) por link ou upload  
> - Galeria de fotos  
> Atualmente esses blocos estao persistidos no frontend (localStorage `venuePresentationData`).

**Campos complementares da tela (persistencia atual):**

**Cardapio item a item:**
```json
{
  "menuItems": [
    {
      "id": "string",
      "name": "string",
      "description": "string",
      "category": "Pratos|Porcoes|Bebidas",
      "price": "39.90",
      "imageUrl": "string",
      "isPortion": true
    }
  ]
}
```

**Cardapio pronto (PDF/Imagem):**
```json
{
  "menuDocumentUrl": "string",
  "menuDocumentName": "string"
}
```

**Galeria:**
```json
{
  "gallery": ["url1", "url2", "url3"]
}
```

---

### GET /api/v1/addresses/cep/{cep}
**Tela:** EstablishmentProfile (aba Local e Mapa)

Usado para auto-preencher `street`, `neighborhood`, `city`, `state` ao digitar CEP.

---

## 6) EVENTOS

### GET /api/v1/events?page=1&pageSize=10
**Tela:** Home, Explore

**Response 200 - EventDtoIEnumerableApiResponse:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "title": "string",
      "description": "string",
      "category": 1,
      "date": "datetime",
      "startTime": { "ticks": 0, "hours": 20, "minutes": 0, "seconds": 0 },
      "endTime": { "ticks": 0, "hours": 23, "minutes": 0, "seconds": 0 },
      "timezone": "America/Sao_Paulo",
      "status": 2,
      "visibility": 1,
      "isAgeRestricted": false,
      "minimumAge": null,
      "isOnline": false,
      "streamUrl": null,
      "coverImage": "string",
      "isPaid": false,
      "minPrice": null,
      "maxPrice": null,
      "currency": "BRL",
      "likes": 0,
      "views": 0,
      "shares": 0,
      "totalCapacity": 200,
      "availableTickets": 200,
      "soldTickets": 0,
      "publishedAt": "datetime",
      "createdAt": "datetime",
      "createdBy": { "id": "uuid", "email": "string", "firstName": "string" },
      "artistProfile": { "id": "uuid", "stageName": "string", "rating": 4.5 },
      "venueProfile": { "id": "uuid", "name": "string", "rating": 4.2 },
      "genres": [],
      "tags": []
    }
  ]
}
```

---

### POST /api/v1/events
**Tela:** Criar evento

**Request Body - CreateEventCommand:**
```json
{
  "name": "string",
  "description": "string",
  "startDateTime": "2025-01-01T20:00:00Z",
  "endDateTime": "2025-01-01T23:00:00Z",
  "artistProfileId": "uuid",
  "venueProfileId": "uuid",
  "price": 30.00,
  "maxCapacity": 200,
  "isPublic": true,
  "acceptSongRequests": true
}
```

---

### PUT /api/v1/events/{id}
**Request Body - UpdateEventRequest:**
```json
{
  "title": "string",
  "description": "string",
  "date": "2025-01-01T00:00:00Z",
  "startTime": { "ticks": 72000000000 },
  "endTime": { "ticks": 828000000000 }
}
```

---

### GET /api/v1/events/week
**Tela:** WeekSchedule

Retorna EventDtoIEnumerableApiResponse dos eventos da semana do usuario logado.

---

### GET /api/v1/events/today/live
**Tela:** Home - eventos ao vivo

Retorna EventDtoIEnumerableApiResponse.

---

### PATCH /api/v1/events/{id}/status
**Tela:** ArtistHome - iniciar/encerrar show

**Request Body - ChangeEventStatusRequest:**
```json
{ "status": 4 }
```
> status: 4=Ongoing (iniciar show), 5=Finished (encerrar show)

---

### POST /api/v1/events/{id}/like
### DELETE /api/v1/events/{id}/like
### POST /api/v1/events/{id}/share
Sem body.

---

## 7) REPERTORIO

### GET /api/v1/repertoires?artistProfileId=
**Tela:** RepertoireManager

**Response 200:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "artistProfileId": "uuid",
      "name": "string",
      "description": "string",
      "isActive": true,
      "songs": [
        {
          "id": "uuid",
          "title": "string",
          "artist": "string",
          "genre": { "id": "uuid", "name": "Rock", "color": "#ff0000", "description": "" },
          "duration": "string",
          "difficulty": 1,
          "key": "string",
          "hasLyrics": false,
          "lyrics": null,
          "notes": null,
          "year": 2020,
          "isPopular": false
        }
      ],
      "createdAt": "datetime"
    }
  ]
}
```

---

### POST /api/v1/repertoires
**Request Body - CreateRepertoireRequest:**
```json
{
  "artistProfileId": "uuid",
  "name": "setlist principal",
  "description": "string",
  "isActive": true
}
```

---

### PUT /api/v1/repertoires/{id}
**Request Body - UpdateRepertoireRequest:**
```json
{
  "name": "string",
  "description": "string",
  "isActive": true
}
```

---

### DELETE /api/v1/repertoires/{id}
### PATCH /api/v1/repertoires/{id}/activate
### PATCH /api/v1/repertoires/{id}/deactivate
Sem body.

---

### POST /api/v1/repertoires/{id}/songs
**Request Body - AddSongsRequest:**
```json
{
  "songIds": ["uuid1", "uuid2", "uuid3"]
}
```

---

### DELETE /api/v1/repertoires/{id}/songs/{songId}
Sem body.

---

### PATCH /api/v1/repertoires/{id}/songs/reorder
**Request Body - ReorderSongsRequest:**
```json
{
  "orders": [
    { "songId": "uuid1", "order": 1 },
    { "songId": "uuid2", "order": 2 }
  ]
}
```

---

## 8) MUSICAS (SONGS)

### GET /api/v1/songs?query=
**Tela:** RepertoireManager, WeekSchedule

**Response 200 - SongDtoIEnumerableApiResponse:**
```json
{
  "success": true,
  "data": [
    {
      "id": "uuid",
      "title": "string",
      "artist": "string",
      "genre": { "id": "uuid", "name": "Rock", "color": "#ff0000", "description": "" },
      "duration": "string",
      "difficulty": 1,
      "key": "string",
      "hasLyrics": false,
      "lyrics": null,
      "notes": null,
      "year": 2020,
      "isPopular": false
    }
  ]
}
```

---

### POST /api/v1/songs
**Request Body - CreateSongCommand:**
```json
{
  "title": "string",
  "artist": "string",
  "album": "string",
  "duration": 240,
  "genreId": "uuid",
  "key": "Dm",
  "tempo": 120,
  "year": 2020,
  "spotifyId": "string",
  "youTubeId": "string"
}
```

---

### PUT /api/v1/songs/{id}
**Request Body - UpdateSongRequest:**
```json
{
  "title": "string",
  "artist": "string",
  "duration": "string",
  "year": 2020
}
```

---

## 9) PEDIDOS DE MUSICA

### POST /api/v1/song-requests
**Tela:** WeekSchedule - usuario faz pedido

**Request Body - CreateSongRequestCommand:**
```json
{
  "eventId": "uuid",
  "songId": "uuid",
  "userId": "uuid",
  "message": "Por favor toca essa!"
}
```

**Response 200 - SongRequestDtoApiResponse:**
```json
{
  "success": true,
  "data": {
    "id": "uuid",
    "eventId": "uuid",
    "song": { "id": "uuid", "title": "string", "artist": "string" },
    "requestedBy": { "id": "uuid", "email": "string", "firstName": "string" },
    "status": 1,
    "message": "string",
    "votes": 0,
    "createdAt": "datetime",
    "playedAt": null
  }
}
```

---

### GET /api/v1/song-requests/me
**Tela:** Usuario - meus pedidos

Response: SongRequestIEnumerableApiResponse

---

### GET /api/v1/song-requests/event/{eventId}?pendingOnly=false
**Tela:** EventDetails

Response: SongRequestDtoIEnumerableApiResponse

---

### GET /api/v1/song-requests/live/{artistId}
**Tela:** LiveRequests - painel ao vivo

> artistId = id do ArtistProfile (nao do User)

Response: SongRequestIEnumerableApiResponse

---

### PATCH /api/v1/song-requests/{id}/accept
### PATCH /api/v1/song-requests/{id}/decline
### PATCH /api/v1/song-requests/{id}/play
### PATCH /api/v1/song-requests/{id}/finish
Sem body.

---

### POST /api/v1/song-requests/{id}/vote
### DELETE /api/v1/song-requests/{id}/vote
Sem body.

---

## 10) NOTIFICACOES

### GET /api/v1/notifications/me
### PATCH /api/v1/notifications/{id}/read
### PATCH /api/v1/notifications/read-all
Sem body nas acoes de leitura.

---

## 11) UPLOADS

### POST /api/v1/uploads/image
### POST /api/v1/uploads/audio
### DELETE /api/v1/uploads/{fileId}

---

## 12) ADMIN

### GET /api/v1/admin/users
### PATCH /api/v1/admin/users/{id}/status
### GET /api/v1/admin/moderation/comments
### PATCH /api/v1/admin/moderation/comments/{id}/approve
### PATCH /api/v1/admin/moderation/comments/{id}/reject
### GET /api/v1/admin/reports

---

## 13) TEMPO REAL (SignalR)

### Eventos Servidor > Cliente
- songRequestCreated
- songRequestUpdated
- songRequestVoted
- showStatusChanged
- nowPlayingChanged

### Acoes Cliente > Servidor
- JoinArtistRoom(artistId)
- JoinEventRoom(eventId)
- LeaveRoom(room)
- SendSongRequest(payload)
- VoteSongRequest(requestId)
- ChangeSongRequestStatus(requestId, status)

---

## 14) MAPEAMENTO TELAS x ENDPOINTS

### Login / Cadastro
| Acao | Endpoint | Campos |
|------|----------|--------|
| Criar conta | POST /auth/register | email, password, confirmPassword, username, firstName, lastName, role(string), acceptTerms, acceptPrivacy |
| Entrar | POST /auth/login | email, password |
| Refresh token | POST /auth/refresh-token | refreshToken |
| Logout | POST /auth/logout | refreshToken |
| Restaurar sessao | GET /auth/me | — |

> Redirecionamento por perfil apos login:  
> - AUDIENCE -> /user/home  
> - SINGER/ARTIST -> /artist/home  
> - VENUE/ESTABLISHMENT -> /establishment/home  
> - ADMIN -> /admin/moderation

---

### Finalizar Cadastro (CompleteProfile)
| Acao | Endpoint | Campos |
|------|----------|--------|
| Salvar perfil completo | PUT /users/me/complete-profile | cpf, birthDate, gender(string), address{cep,street,number,complement,neighborhood,city,state}, contact{phone,whatsapp,emergencyContact,emergencyPhone} |
| Buscar CEP | GET /addresses/cep/{cep} | cep (path) — retorna logradouro, bairro, localidade, uf |
| Verificar completude | GET /users/me/profile-completion | — |

> A tela preenche automaticamente os campos quando /auth/me ja retorna dados existentes.

---

### User Profile (Meu Perfil)
| Acao | Endpoint | Campos |
|------|----------|--------|
| Carregar dados | GET /auth/me | dados basicos + cpf + contato + endereco |
| Salvar dados basicos | PUT /users/me | id, username, email, firstName, lastName, phoneNumber |
| Salvar dados completos | PUT /users/me/complete-profile | cpf, birthDate, gender, address{}, contact{} |

---

### UserHome
| Acao | Endpoint | Campos |
|------|----------|--------|
| Dados do usuario | GET /users/me | — |
| Venues proximos | GET /venues/nearby | lat, lng, radius |
| Artistas | GET /artists | query |
| Favoritos | GET /users/me/favorites | type |
| Adicionar favorito | POST /users/me/favorites | type, refId |
| Remover favorito | DELETE /users/me/favorites/{id} | — |

---

### SearchPage
| Acao | Endpoint | Campos |
|------|----------|--------|
| Buscar venues | GET /venues | query |
| Buscar artistas | GET /artists | query |
| Seguir artista | POST /artists/{id}/follow | — |

---

### WeekSchedule
| Acao | Endpoint | Campos |
|------|----------|--------|
| Eventos da semana | GET /events/week | — |
| Repertorio do artista | GET /artists/{id}/repertoire | — |
| Criar pedido | POST /song-requests | eventId, songId, userId, message |

---

### ArtistHome (Dashboard)
| Acao | Endpoint | Campos |
|------|----------|--------|
| Dashboard | GET /artists/me/dashboard | — |
| Stats semana | GET /artists/me/weekly-stats | — |
| Feedbacks | GET /artists/me/feedbacks | — |
| Iniciar show | PATCH /events/{id}/status | status: 4 |
| Encerrar show | PATCH /events/{id}/status | status: 5 |
| Logout | POST /auth/logout | refreshToken |

---

### Artist Complete Profile (nova tela)
| Acao | Endpoint | Campos |
|------|----------|--------|
| Abrir tela de finalizacao | /artist/complete-profile | 6 secoes: Info Basica, Documentos, Dados Bancarios, Redes Sociais, Portfolio, Equipamentos |
| Salvar cadastro completo | PUT /artists/me/complete-profile | stageName, fullName, birthDate, phone, email, genre, city, state, bio, cpf, rg, cnh, proofAddressUrl, profilePhotoUrl, documentFrontUrl, documentBackUrl, bankName, bankCode, agency, account, accountType, holderName, holderDocument, pixKeyType, pixKey, instagram, youtube, spotify, tiktok, facebook, website, portfolioSummary, portfolioHighlights, portfolioLinks, pressKitUrl, demoVideoUrl, repertoireDocUrl, hasOwnSoundSystem, hasOwnLighting, bringsBand, musiciansCount, instruments, technicalRider, transportInfo, setupTimeMinutes |
| Foto do artista na aba Portfolio | Campo reaproveitado no mesmo PUT | upload/ camera atualiza `profilePhotoUrl` |
| Ao sair para /artist/home | GET /auth/me | sincroniza sessao para refletir stageName e genre atualizados na home |
| Rascunho local | localStorage (artistCompleteProfileData) | espelho do payload completo para cache local |

---

### RepertoireManager
| Acao | Endpoint | Campos |
|------|----------|--------|
| Listar | GET /repertoires | artistProfileId |
| Criar | POST /repertoires | artistProfileId, name, description, isActive |
| Editar | PUT /repertoires/{id} | name, description, isActive |
| Deletar | DELETE /repertoires/{id} | — |
| Ativar | PATCH /repertoires/{id}/activate | — |
| Adicionar musicas | POST /repertoires/{id}/songs | songIds[] |
| Remover musica | DELETE /repertoires/{id}/songs/{songId} | — |
| Reordenar | PATCH /repertoires/{id}/songs/reorder | orders[]{songId, order} |
| Buscar musicas | GET /songs | query |
| Criar musica | POST /songs | title, artist, album, duration, genreId, key, tempo, year |

---

### LiveRequests
| Acao | Endpoint | Campos |
|------|----------|--------|
| Carregar pedidos | GET /song-requests/live/{artistId} | — |
| Aceitar | PATCH /song-requests/{id}/accept | — |
| Recusar | PATCH /song-requests/{id}/decline | — |
| Tocar | PATCH /song-requests/{id}/play | — |
| Finalizar | PATCH /song-requests/{id}/finish | — |
| Votar | POST /song-requests/{id}/vote | — |

---

### Establishment Home/Profile/Menu (nova tela)
| Acao | Endpoint | Campos |
|------|----------|--------|
| Salvar perfil comercial | PUT /venues/me/profile | name, description, capacity, type, phone, email, website, street, number, neighborhood, city, state, zipCode, latitude?, longitude? |
| Buscar CEP | GET /addresses/cep/{cep} | preenche rua/bairro/cidade/estado automaticamente |
| Cardapio item por item | Persistencia atual localStorage | menuItems[]{id,name,description,category,price,imageUrl,isPortion} |
| Cardapio pronto (PDF/Imagem) | Persistencia atual localStorage | menuDocumentUrl, menuDocumentName |
| Galeria de fotos | Persistencia atual localStorage | gallery[] (urls) |
| Logout | POST /auth/logout | refreshToken |

---

## 15) INCONSISTENCIAS E ALERTAS IMPORTANTES

| # | Problema | Descricao |
|---|----------|-----------|
| 1 | Register frontend != contrato antigo | Frontend envia username, firstName, lastName, role string e flags de termos/privacidade |
| 2 | CompleteProfile expandido | Frontend envia cpf + address{} + contact{} no mesmo payload de /users/me/complete-profile |
| 3 | User role pode vir string ou int | Redirecionamentos e guards tratam SINGER/ARTIST (2), VENUE/ESTABLISHMENT (3), ADMIN (4) |
| 4 | User Profile faz dupla atualizacao | Tela Meu Perfil salva em /users/me e depois em /users/me/complete-profile |
| 5 | Venue profile expandido | /venues/me/profile no frontend inclui endereco e coordenadas alem dos campos basicos |
| 6 | Artist Complete Profile usa endpoint dedicado | Frontend agora envia todas as 6 secoes para /artists/me/complete-profile; manter backend alinhado com o payload completo |
| 7 | Cardapio e galeria do estabelecimento | Hoje persistem localmente; recomendado endpoint de menu, galeria e upload de arquivos |
| 8 | CEP pt-BR precisa mapeamento | API retorna logradouro/bairro/localidade/uf e frontend mapeia para street/neighborhood/city/state |
| 9 | artistId em live requests | Usar id do ArtistProfile, nao id do User |
