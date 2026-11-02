using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using ShahiZayka.Api.Middleware;
using ShahiZayka.Application.Interfaces;
using ShahiZayka.Application.Services;
using ShahiZayka.Infrastructure.Caching;
using ShahiZayka.Infrastructure.Data;
using ShahiZayka.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Controllers & Serialization
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// 2. Configure Database Context (PostgreSQL with Npgsql, with InMemory fallback for seamless development)
var connectionString = builder.Configuration.GetConnectionString("PostgreSql");
var provider = builder.Configuration["DatabaseProvider"] ?? "PostgreSQL";
bool usePostgreSql = false;

if (provider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(connectionString))
{
    try
    {
        using var testConn = new Npgsql.NpgsqlConnection(connectionString);
        testConn.Open();
        usePostgreSql = true;
        Console.WriteLine("[Database] Successfully connected to PostgreSQL 17 database!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Notice] PostgreSQL connection attempt note: {ex.Message}");
        Console.WriteLine("[Database Notice] Active provider: InMemoryDatabase (Zero-friction local dev).");
        Console.WriteLine("[Database Notice] To use local PostgreSQL 17: set your password in appsettings.json.");
    }
}

builder.Services.AddDbContext<ShahiZaykaDbContext>(options =>
{
    if (usePostgreSql && !string.IsNullOrWhiteSpace(connectionString))
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    }
    else
    {
        options.UseInMemoryDatabase("ShahiZaykaDb");
    }
});

builder.Services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ShahiZaykaDbContext>());

// 3. Add In-Memory Caching
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();

// 4. Register Domain & Application Services
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<INewsletterService, NewsletterService>();

// 5. Configure JWT Authentication & Authorization
var secretKey = builder.Configuration["Jwt:SecretKey"] ?? "ShahiZayka_SuperSecretKey_For_Amazon_Interview_TopTierProject_2026!";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "ShahiZaykaApi";
var audience = builder.Configuration["Jwt:Audience"] ?? "ShahiZaykaClient";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ValidateIssuer = true,
        ValidIssuer = issuer,
        ValidateAudience = true,
        ValidAudience = audience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// 6. Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 7. Configure Swagger / OpenAPI with Bearer Auth
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Shahi Zayka API",
        Version = "v1",
        Description = "Enterprise E-Commerce & Food Delivery REST API with Clean Architecture, PostgreSQL, Idempotent Checkout, and JWT Auth."
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token like: Bearer {your token}"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", null),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 8. Configure HTTP Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors("AllowAll");

// Serve static frontend files (from wwwroot or project root containing index.html)
var currentDir = new DirectoryInfo(Directory.GetCurrentDirectory());
DirectoryInfo? rootWithIndex = currentDir;
while (rootWithIndex != null && !File.Exists(Path.Combine(rootWithIndex.FullName, "index.html")))
{
    rootWithIndex = rootWithIndex.Parent;
}

var staticFilesPath = rootWithIndex != null 
    ? rootWithIndex.FullName 
    : Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

if (Directory.Exists(staticFilesPath))
{
    var fileProvider = new PhysicalFileProvider(staticFilesPath);
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
}
else
{
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shahi Zayka API v1");
    c.RoutePrefix = "swagger";
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Fallback route for SPA or root
if (File.Exists(Path.Combine(staticFilesPath, "index.html")))
{
    app.MapFallbackToFile("index.html", new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(staticFilesPath)
    });
}
else
{
    app.MapFallbackToFile("index.html");
}

// 9. Database Auto-Migration & Seed on Startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<ShahiZaykaDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        
        logger.LogInformation("Initializing database and seeding royal culinary catalog...");
        await DbInitializer.SeedAsync(context, passwordHasher);
        logger.LogInformation("Database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not initialize PostgreSQL database. Ensure PostgreSQL is running and credentials in appsettings.json are valid. Falling back to in-memory mode.");
    }
}

app.Run();
