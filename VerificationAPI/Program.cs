// Program.cs
using System.Text;
using CoreWCF.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VerificationAPI.Services;
using VerificationAPI.Services.SoapContracts;
using VerificationAPI.XmlModels;

var builder = WebApplication.CreateBuilder(args);

// ─── 1) Validation services ───────────────────────────────────────────────────
var xsdPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataXSD.xsd");
builder.Services.AddSingleton(new XmlValidationService(xsdPath));

var rngPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataRNG.rng");
builder.Services.AddSingleton(new RngValidationService(rngPath));

// ─── 2) In‐memory store & SOAP search ──────────────────────────────────────────
builder.Services.AddSingleton<List<VideoMetadata>>();     // REST CRUD store
builder.Services.AddSingleton<SearchService>();           // SOAP search service
builder.Services.AddServiceModelServices();               // CoreWCF

// ─── 3) Controllers + XML formatters ───────────────────────────────────────────
builder.Services
    .AddControllers(options => {
        // raw XML input formatter for reading the POSTed XML payload
        options.InputFormatters.Insert(0, new RawXmlInputFormatter());
    })
    .AddXmlSerializerFormatters();

// ─── 4) Swagger / OpenAPI ──────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    // JWT Header input
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Enter ‘Bearer {token}’",
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

// ─── 5) CORS for your Vue frontend ─────────────────────────────────────────────
builder.Services.AddCors(o =>
    o.AddPolicy("AllowVue", p => p
        .WithOrigins("http://localhost:5173")
        .AllowAnyHeader()
        .AllowAnyMethod()
    )
);

// ─── 6) JWT Authentication & Authorization ──────────────────────────────────────
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts => {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "tvojIssuer",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "tvojaPublika",
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "superTajniKljuč")
            )
        };
    });
builder.Services.AddAuthorization();

// ─── Build the app ───────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Middlewares ────────────────────────────────────────────────────────────────
app.UseCors("AllowVue");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verification API v1")
);

// ─── Map SOAP endpoint ──────────────────────────────────────────────────────────
app.UseServiceModel(sb => {
    sb.AddService<SearchService>();
    sb.AddServiceEndpoint<SearchService, ISearchService>(
        new CoreWCF.BasicHttpBinding(),
        "/soap/search"
    );
});

// ─── Map REST controllers ───────────────────────────────────────────────────────
app.MapControllers();

app.Run();
