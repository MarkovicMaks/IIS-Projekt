using CoreWCF.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using VerificationAPI.Data;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using VerificationAPI.Models;
using VerificationAPI.Services;
using VerificationAPI.Services.SoapContracts;
using VerificationAPI.XmlModels;

namespace VerificationAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var rngPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataRNG.rng");
            builder.Services.AddSingleton(new RngValidationService(rngPath));
            var xsdPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataXSD.xsd");
            builder.Services.AddSingleton(new XmlValidationService(xsdPath));
            //builder.Services.AddSingleton<List<VideoMetadata>>();
            //builder.Services.AddSingleton<List<string>>();

            builder.Services.AddDbContext<VerificationDbContext>(options =>
                options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<JwtService>();

            builder.Services.AddAuthentication(options =>
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
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(builder.Configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured"))),
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();

            builder.Services.AddScoped<SearchService>();
            builder.Services.AddServiceModelServices();

            builder.Services.AddHttpClient<WeatherXmlRpcService>();
            builder.Services.AddScoped<WeatherXmlRpcService>();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Verification API",
                    Version = "v1",
                    Description = "Import TikTok JSON → XML → XSD validate with JWT Authentication"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

            builder.Services.AddCors(o =>
            {
                o.AddPolicy("AllowVue", p => p
                    .WithOrigins("http://localhost:5173")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials());
            });

            builder.Services.AddControllers(opt =>
            {
                opt.InputFormatters.Insert(0, new RawXmlInputFormatter());
            })
            .AddXmlSerializerFormatters();

            var app = builder.Build();

            app.UseCors("AllowVue");

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verification API v1");
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseServiceModel(builder =>
            {
                builder
                    .AddService<VerificationAPI.Services.SoapContracts.SearchService>()
                    .AddServiceEndpoint<
                        VerificationAPI.Services.SoapContracts.SearchService,
                        VerificationAPI.Services.SoapContracts.ISearchService
                    >(
                        new CoreWCF.BasicHttpBinding(),
                        "/soap/search"
                    );
            });

            app.MapControllers();

            app.Map("/xmlrpc/weather", async (HttpContext context, WeatherXmlRpcService xmlRpcService) =>
            {
                if (context.Request.Method == "POST")
                {
                    await xmlRpcService.ProcessXmlRpcRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 405; 
                    await context.Response.WriteAsync("XML-RPC endpoint - POST only");
                }
            });
            app.Run();
        }
    }
}
