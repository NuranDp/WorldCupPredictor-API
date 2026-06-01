using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorldCupPredictor.API.Background;
using WorldCupPredictor.API.Data;
using WorldCupPredictor.API.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
// Switch via appsettings.json: "DatabaseProvider": "SqlServer" | "Supabase"
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("Supabase", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("SupabaseConnection"),
            o => o.MigrationsHistoryTable("__EFMigrationsHistory")
                  .MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));
    else
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// ── Auth services ─────────────────────────────────────────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IBracketService, BracketService>();
builder.Services.AddScoped<IDraftService, DraftService>();
builder.Services.Configure<ScoringOptions>(builder.Configuration.GetSection("Scoring"));
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<ApiFootballService>();   // kept for manual admin use
builder.Services.AddScoped<EspnSoccerService>();
builder.Services.AddHostedService<ResultsPollingService>();

// ESPN — free, no key required
builder.Services.AddHttpClient("Espn", client =>
{
    client.BaseAddress = new Uri("https://site.api.espn.com/apis/site/v2/sports/soccer/fifa.world");
});

// API-Football — kept as fallback, requires paid plan for 2026
builder.Services.AddHttpClient("ApiFootball", (sp, client) =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    client.BaseAddress = new Uri(cfg["ApiFootball:BaseUrl"] ?? "https://v3.football.api-sports.io");
    client.DefaultRequestHeaders.Add("x-apisports-key", cfg["ApiFootball:ApiKey"] ?? "");
});

// ── JWT Authentication ────────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("Angular", policy =>
    {
        var origins = builder.Configuration["Cors:AllowedOrigins"]!.Split(',');
        policy.WithOrigins(origins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ── Controllers + OpenAPI ─────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddOpenApi();

// ═════════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Run migrations + seed ─────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection(); // disabled for local dev (use http://localhost:5001)
app.UseCors("Angular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
