using CoreWCF.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
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

            // Add services to the container.
            var rngPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataRNG.rng");
            builder.Services.AddSingleton(new RngValidationService(rngPath));
            var xsdPath = Path.Combine(builder.Environment.ContentRootPath, "Schemas", "MetadataXSD.xsd");
            builder.Services.AddSingleton(new XmlValidationService(xsdPath));
            builder.Services.AddSingleton<List<VideoMetadata>>();
            builder.Services.AddSingleton<List<string>>();

            //Soap stuf
            builder.Services.AddSingleton<SearchService>();          
            builder.Services.AddServiceModelServices();             

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Verification API",
                    Version = "v1",
                    Description = "Import TikTok JSON → XML → XSD validate"
                });
            });
            builder.Services.AddCors(o =>
            {
                o.AddPolicy("AllowVue", p => p
                    .WithOrigins("http://localhost:5173")   // Vite dev server
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            });

            builder.Services.AddControllers(opt =>
                            {
                                opt.InputFormatters.Insert(0, new RawXmlInputFormatter());
                            })
                            .AddXmlSerializerFormatters();

            builder.Services
              .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
              .AddJwtBearer(opts => {
                  opts.TokenValidationParameters = new TokenValidationParameters
                  {
                      ValidateIssuer = true,
                      ValidateAudience = true,
                      ValidateLifetime = true,
                      ValidateIssuerSigningKey = true,
                      ValidIssuer = "tvojIssuer",
                      ValidAudience = "tvojaPublika",
                      IssuerSigningKey = new SymmetricSecurityKey(
                      Encoding.UTF8.GetBytes("superTajniKljuč")
                    )
                  };
              });



            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseCors("AllowVue");
            // Configure the HTTP request pipeline.

            //app.UseAuthorization();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Verification API v1");
            });

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

            app.Run();
        }
    }
}
