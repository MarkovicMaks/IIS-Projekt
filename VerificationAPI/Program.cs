// Program.cs
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VerificationAPI.Services;
using VerificationAPI.Services.SoapContracts;
using VerificationAPI.XmlModels;

var builder = WebApplication.CreateBuilder(args);

var xsdPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataXSD.xsd");
builder.Services.AddSingleton(new XmlValidationService(xsdPath));

var rngPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataRNG.rng");
builder.Services.AddSingleton(new RngValidationService(rngPath));

builder.Services.AddSingleton<List<VideoMetadata>>();
builder.Services.AddSingleton<SearchService>();
builder.Services.AddServiceModelServices();

// Single AddControllers registration with your formatter
builder.Services
    .AddControllers(opts => {
        opts.InputFormatters.Insert(0, new RawXmlInputFormatter());
    })
    .AddXmlSerializerFormatters();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme {
                Reference = new OpenApiReference {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Verification API",
        Version = "v1",
        Description = "TikTok XML → XSD/RNG validation + JWT‐protected CRUD + SOAP search"
    });
});

builder.Services.AddCors(o =>
    o.AddPolicy("AllowVue", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
    )
);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                 Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            )
        };

        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var raw = ctx.Request.Headers["Authorization"].FirstOrDefault();
                Console.WriteLine($"🔍 RAW HEADER = '{raw ?? "<null>"}'");

                if (!string.IsNullOrWhiteSpace(raw) &&
                    raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    var token = raw.Substring("Bearer ".Length).Trim();
                    Console.WriteLine($"🎫 TOKEN = '{token.Substring(0, Math.Min(30, token.Length))}...' dots={token.Count(c => c == '.')}");
                    ctx.Token = token;
                }
                else
                {
                    Console.WriteLine("❌ No valid Bearer token found");
                }

                return Task.CompletedTask;
            },

            OnTokenValidated = ctx =>
            {
                Console.WriteLine("✅ JWT token validated successfully!");
                var user = ctx.Principal?.FindFirst("sub")?.Value;
                Console.WriteLine($"👤 User: {user}");
                return Task.CompletedTask;
            },

            OnAuthenticationFailed = ctx =>
            {
                Console.WriteLine($"❌ JWT auth failed: {ctx.Exception.GetType().Name}");
                Console.WriteLine($"❌ Error message: {ctx.Exception.Message}");
                if (ctx.Exception.InnerException != null)
                {
                    Console.WriteLine($"❌ Inner exception: {ctx.Exception.InnerException.Message}");
                }
                return Task.CompletedTask;
            },

            OnChallenge = ctx =>
            {
                Console.WriteLine($"🚫 JWT Challenge triggered: {ctx.Error}");
                Console.WriteLine($"🚫 Error description: {ctx.ErrorDescription}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseCors("AllowVue");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verification API v1")
);

app.UseServiceModel(sb => {
    sb.AddService<SearchService>();
    sb.AddServiceEndpoint<SearchService, ISearchService>(
        new CoreWCF.BasicHttpBinding(),
        "/soap/search"
    );
});

app.MapControllers();

app.Run();