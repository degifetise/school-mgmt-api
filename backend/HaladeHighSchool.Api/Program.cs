using System.Text;
using System.Security.Claims;
using System.Text.Json.Serialization;
using HaladeHighSchool.Api.Configuration;
using HaladeHighSchool.Api.Data;
using HaladeHighSchool.Api.Models;
using HaladeHighSchool.Api.Middleware;
using HaladeHighSchool.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using FluentValidation;
using FluentValidation.AspNetCore;

static string GetConfiguredValue(IConfiguration configuration, params string[] keys)
{
    foreach (var key in keys)
    {
        var value = configuration[key];
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }
    }

    return string.Empty;
}

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Configuration
// ---------------------------------------------------------------------------
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<ProvisioningSettings>()
    .Bind(builder.Configuration.GetSection(ProvisioningSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services
    .AddOptions<EmailSettings>()
    .Bind(builder.Configuration.GetSection(EmailSettings.SectionName))
    .Validate(settings => string.IsNullOrWhiteSpace(settings.SendGridApiKey) ||
        (!string.IsNullOrWhiteSpace(settings.FromAddress) && !string.IsNullOrWhiteSpace(settings.FromName)),
        "Email:FromAddress and Email:FromName are required when Email:SendGridApiKey is configured.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ResendSettings>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration.GetSection(ResendSettings.SectionName).Bind(settings);
        settings.ApiKey = GetConfiguredValue(
            configuration,
            "Resend:ApiKey",
            "EmailSettings:ApiKey");
        settings.FromAddress = GetConfiguredValue(
            configuration,
            "Resend:FromAddress",
            "EmailSettings:SenderEmail",
            "EmailSettings:FromAddress",
            "Email:FromAddress");
        settings.FromName = GetConfiguredValue(
            configuration,
            "Resend:FromName",
            "EmailSettings:SenderName",
            "Email:FromName");
    });

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("The 'Jwt' configuration section is missing.");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
}

// ---------------------------------------------------------------------------
// Database
// ---------------------------------------------------------------------------
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, postgres =>
    {
        postgres.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
        postgres.CommandTimeout(60);
    });

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
    }
});

builder.Services.AddScoped<IApplicationDbContext>(serviceProvider =>
    serviceProvider.GetRequiredService<ApplicationDbContext>());

// ---------------------------------------------------------------------------
// Identity
// ---------------------------------------------------------------------------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 8;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedEmail = false;

        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// ---------------------------------------------------------------------------
// JWT bearer authentication
// ---------------------------------------------------------------------------
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ValidateLifetime = true,
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (string.IsNullOrWhiteSpace(context.Token)
                    && context.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }

                return Task.CompletedTask;
            },
            OnAuthenticationFailed = context =>
            {
                if (context.Exception is SecurityTokenExpiredException)
                {
                    context.Response.Headers.Append("X-Token-Expired", "true");
                }

                return Task.CompletedTask;
            },
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail("The account is not active.");
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users
                    .AsNoTracking()
                    .Where(user => user.Id == userId)
                    .Select(user => new
                    {
                        user.IsActive,
                        user.SchoolId,
                        SchoolIsActive = db.Schools
                            .Where(school => school.Id == user.SchoolId)
                            .Select(school => (bool?)school.IsActive)
                            .FirstOrDefault()
                    })
                    .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

                var tokenSchoolId = context.Principal?.FindFirstValue(PortalClaims.SchoolId);
                var isSuperAdmin = context.Principal?.IsInRole(Roles.SuperAdmin) == true;
                if (user is null
                    || !user.IsActive
                    || !Guid.TryParse(tokenSchoolId, out var schoolId)
                    || schoolId != user.SchoolId
                    || (!isSuperAdmin && user.SchoolIsActive != true))
                {
                    context.Fail("The account is not active or its school assignment has changed.");
                }
            }
        };
    });

builder.Services.AddAuthorization();




// Read from Render environment variable, or fall back to localhost for development
var frontendEnv = Environment.GetEnvironmentVariable("FRONTEND_URL");
var allowedOrigins = !string.IsNullOrWhiteSpace(frontendEnv)
    ? frontendEnv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    : new[] { "http://localhost:5173", "http://127.0.0.1:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercelFrontend", policy => policy
        .WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("X-Token-Expired", "Content-Disposition")
        .AllowCredentials());
});

// ---------------------------------------------------------------------------
// Application services
// ---------------------------------------------------------------------------
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAccountProvisioningService, AccountProvisioningService>();
builder.Services.AddScoped<SmartIDCardService>();
builder.Services.AddSingleton<IProfilePhotoStorage, ProfilePhotoStorage>();
builder.Services.AddSingleton(serviceProvider =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<JwtSettings>>().Value;
    return new SmartIDTokenKeyProtector(settings.Key);
});
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();
builder.Services.AddScoped<IGradingPolicyService, GradingPolicyService>();
builder.Services.AddScoped<ITeachingAssignmentService, TeachingAssignmentService>();
builder.Services.AddScoped<ITeacherAssignmentService, TeacherAssignmentService>();
builder.Services.AddScoped<IRegistrationRequestService, RegistrationRequestService>();
builder.Services.AddScoped<IReportCardService, ReportCardService>();
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IPdfReportCardService, PdfReportCardService>();
builder.Services.AddScoped<IReportPdfService, ReportPdfService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();
builder.Services.AddScoped<IPasswordResetApprovalService, PasswordResetApprovalService>();
builder.Services.AddScoped<IEmailSender, SendGridEmailSender>();
builder.Services.AddHttpClient<IEmailService, EmailService>(client =>
{
    client.BaseAddress = new Uri("https://api.resend.com/");
});
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddSingleton<IEventImageStorage, EventImageStorage>();
builder.Services.AddSingleton<ISchoolLogoStorage, SchoolLogoStorage>();
builder.Services.AddSingleton<ILessonFileStorage, LessonFileStorage>();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.AddAutoMapper(_ => { }, typeof(Program).Assembly);
builder.Services.AddValidatorsFromAssemblyContaining<HaladeHighSchool.Api.Validation.CreateEventDtoValidator>();

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();

// ---------------------------------------------------------------------------
// Swagger with a bearer token input
// ---------------------------------------------------------------------------
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "School Management System API",
        Version = "v1",
        Description = "School management API for Nursery through Grade 12: authentication, students, marks, lessons and announcements."
    });

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste the JWT access token returned by /api/auth/login. The 'Bearer ' prefix is added automatically.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document)] = []
    });
});

var app = builder.Build();
app.UseCors("AllowVercelFrontend");

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "School Management System API v1");
        options.DocumentTitle = "School Management System API";
    });
}
else
{
    app.UseHttpsRedirection();
}

// Uploaded images are public card assets. Finalize their CORS headers after the
// credentialed API policy runs, so it cannot replace the wildcard for canvas reads.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads"))
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Headers"] = "*";
            return Task.CompletedTask;
        });
    }

    await next();
});

app.UseStaticFiles();

app.UseAuthentication();
app.UseMiddleware<RequiredPasswordChangeMiddleware>();
app.UseAuthorization();
app.MapControllers();

// ---------------------------------------------------------------------------
// Start-up checks and seeding
// ---------------------------------------------------------------------------
try
{
    app.Logger.LogInformation("Initializing the database when it is reachable.");
    await app.InitializeDatabaseAsync();
    app.Run();
}
catch (Exception ex)
{
    app.Logger.LogCritical(ex, "The API failed during startup.");
    throw;
}
