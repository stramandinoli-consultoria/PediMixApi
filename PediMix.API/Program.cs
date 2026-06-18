using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;
using PediMix.Infrastructure.Data;
using PediMix.Application.Interfaces;
using PediMix.Infrastructure.Repositories;
using PediMix.Infrastructure.Services;
using PediMix.Infrastructure.Policies;
using MediatR;
using PediMix.Application.Handlers.CommandHandlers;
using PediMix.Application.Handlers.QueryHandlers;
using PediMix.Application.Mappings;
using PediMix.API.Models;
using PediMix.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

// Database Configuration
var connectionString = RailwayConnectionStringResolver.Resolve(builder.Configuration);
builder.Services.AddDbContext<PediMixDbContext>(options =>
    options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21))));

// Repository Pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IArtistProfileRepository, ArtistProfileRepository>();
builder.Services.AddScoped<IVenueProfileRepository, VenueProfileRepository>();
builder.Services.AddScoped<ISongRepository, SongRepository>();
builder.Services.AddScoped<IRepertoireRepository, RepertoireRepository>();
builder.Services.AddScoped<IEventRepository, EventRepository>();
builder.Services.AddScoped<ISongRequestRepository, SongRequestRepository>();
builder.Services.AddScoped<IGenreRepository, GenreRepository>();

// MediatR
builder.Services.AddMediatR(typeof(CreateUserCommandHandler).Assembly);

// AutoMapper
builder.Services.AddAutoMapper(typeof(UserMappingProfile).Assembly);

// Auth
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var jwtKey = Encoding.UTF8.GetBytes(jwtOptions.Key);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKey),
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers["Token-Expired"] = "true";
                }

                return Task.CompletedTask;
            },
            OnChallenge = async context =>
            {
                if (context.Response.HasStarted)
                {
                    return;
                }

                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";

                var tokenExpired = context.Response.Headers.ContainsKey("Token-Expired");
                var message = tokenExpired
                    ? "Token expirado. Faça login novamente ou use refresh-token."
                    : "Token inválido ou ausente.";

                var payload = ApiResponse<object>.Fail("Não autorizado.", message);
                await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<IAuthService, AuthService>();

// External Services
builder.Services.AddHttpClient<IViaCepService, ViaCepService>();

// ============================================================
// MUSIC INTEGRATIONS (Spotify, Lyrically, Vagalume, YouTube)
// ============================================================

// Cache em memória (sempre disponível — fallback do Redis)
builder.Services.AddMemoryCache();

// Redis (opcional — só registra IDistributedCache se houver connection string)
var redisConnection = RailwayConnectionStringResolver.ResolveRedis(builder.Configuration);

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "pedimix:";
    });
    Console.WriteLine("[Startup] Redis configurado.");
}
else
{
    Console.WriteLine("[Startup] Redis NÃO configurado — MusicCacheService usará IMemoryCache.");
}

// MusicCacheService: tenta Redis -> cai em IMemoryCache se indisponível.
// Usamos factory para resolver IDistributedCache de forma OPCIONAL — se Redis
// não foi registrado acima, GetService<IDistributedCache>() devolve null e o
// MusicCacheService passa a operar 100% em memória.
builder.Services.AddSingleton<IMusicCacheService>(sp =>
{
    var memory = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
    var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<MusicCacheService>>();
    var redis = sp.GetService<Microsoft.Extensions.Caching.Distributed.IDistributedCache>();
    return new MusicCacheService(memory, logger, redis);
});

// HttpClients tipados com Polly (Retry + CircuitBreaker)
builder.Services.AddHttpClient<ISpotifyService, SpotifyService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddPolicyHandler(HttpPolicies.RetryPolicy())
    .AddPolicyHandler(HttpPolicies.CircuitBreakerPolicy());

builder.Services.AddHttpClient<ILyricsService, LyricsService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddPolicyHandler(HttpPolicies.RetryPolicy())
    .AddPolicyHandler(HttpPolicies.CircuitBreakerPolicy());

builder.Services.AddHttpClient<IYouTubeService, YouTubeService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(15);
    })
    .AddPolicyHandler(HttpPolicies.RetryPolicy())
    .AddPolicyHandler(HttpPolicies.CircuitBreakerPolicy());

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "PediMix API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Railway runs behind a reverse proxy; honor forwarded proto/ip headers.
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeadersOptions);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    app.Urls.Add($"http://*:{port}");
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
