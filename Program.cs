using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProductApi.Data;
using ProductApi.Middleware;
using ProductApi.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// 1. Database (EF Core InMemory - swap provider for production databases)
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("ProductApiDb"));

// ---------------------------------------------------------------------------
// 2. In-memory cache (used by ProductsService)
// ---------------------------------------------------------------------------
builder.Services.AddMemoryCache();

// ---------------------------------------------------------------------------
// 3. Application services (DI registration)
// ---------------------------------------------------------------------------
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductsService, ProductsService>();
builder.Services.AddScoped<ICurrentUserResolver, CurrentUserResolver>();

// ---------------------------------------------------------------------------
// 4. JWT Authentication configuration
// ---------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? "SuperSecretProductApiKey_2025_WithAtLeast32Chars!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ProductApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ProductApi";

// Make sure the signing key is at least 256 bits long.
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
if (keyBytes.Length < 32)
{
    keyBytes = System.Security.Cryptography.SHA256.HashData(keyBytes);
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // set true behind TLS reverse proxy
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ---------------------------------------------------------------------------
// 5. CORS - allow web apps from ANY domain to interact with this API.
//    This satisfies the delivery requirement "Pastikan aplikasi web dari
//    berbagai domain untuk bisa berinteraksi dengan API".
// ---------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ---------------------------------------------------------------------------
// 6. Rate limiting policies
//    - write-policy : 1 request / 5 seconds  (POST/PUT/DELETE products)
//    - auth-policy  : 3 requests / 60 seconds (register & login)
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("write-policy", httpContext =>
    {
        // Per-user when authenticated, otherwise per client IP.
        var userKey = httpContext.User?.Identity?.Name ?? "anonymous";
        return RateLimitPartition.GetFixedWindowLimiter(userKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 1,
            Window = TimeSpan.FromSeconds(5),
            QueueLimit = 0
        });
    });

    options.AddPolicy("auth-policy", httpContext =>
    {
        var ipKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ipKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 3,
            Window = TimeSpan.FromSeconds(60),
            QueueLimit = 0
        });
    });

    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var message = "Too many requests. Please slow down.";
        var body = System.Text.Json.JsonSerializer.Serialize(new
        {
            success = false,
            message
        });
        await context.HttpContext.Response.WriteAsync(body);
    };
});

// ---------------------------------------------------------------------------
// 7. Controllers + JSON options (camelCase by default, ignore nulls)
// ---------------------------------------------------------------------------
builder.Services.AddControllers(options =>
{
    // Use our custom validation filter for consistent error envelopes.
    options.Filters.Add<ProductApi.Middleware.CustomValidationFilter>();
})
.ConfigureApiBehaviorOptions(options =>
{
    // Suppress the default ProblemDetails validation response; our
    // CustomValidationFilter will produce the consistent JSON envelope.
    options.SuppressModelStateInvalidFilter = true;
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy =
        System.Text.Json.JsonNamingPolicy.CamelCase;
});

// ---------------------------------------------------------------------------
// 8. Swagger / OpenAPI documentation with JWT support
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Product Management API",
        Version = "v1",
        Description = "REST API for product management & authentication. " +
                      "Backend Developer Coding Test solution written in C# / ASP.NET Core 8."
    });

    // JWT bearer token input box in Swagger UI.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT authentication_token returned by /api/auth/login. " +
                      "Example: Bearer eyJhbGciOi..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ---------------------------------------------------------------------------
// 9. Logging + global exception middleware
// ---------------------------------------------------------------------------
builder.Services.AddLogging();

var app = builder.Build();

// Seed the database with sample products on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(db);
}

// ---------------------------------------------------------------------------
// 10. Middleware pipeline
// ---------------------------------------------------------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Global exception handler must be registered early.
app.UseMiddleware<GlobalExceptionMiddleware>();

// CORS must be called before auth/authorization.
app.UseCors("AllowAll");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Lightweight root health endpoint.
app.MapGet("/", () => Results.Ok(new
{
    name = "Product Management API",
    version = "v1",
    status = "running",
    docs = "/swagger",
    endpoints = new[]
    {
        "GET    /api/products",
        "GET    /api/products/{id}",
        "POST   /api/products      (auth required)",
        "PUT    /api/products/{id} (auth required)",
        "DELETE /api/products/{id} (auth required)",
        "POST   /api/auth/register",
        "POST   /api/auth/login",
        "POST   /api/auth/refresh"
    }
}));

app.Run();
