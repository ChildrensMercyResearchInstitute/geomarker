using FluentValidation;
using GeoMarker.Frontiers.Core.Models.Configuration;
using GeoMarker.Frontiers.Core.Models;
using GeoMarker.Frontiers.Core.Models.Request;
using GeoMarker.Frontiers.Core.Models.Request.Validation;
using GeoMarker.Frontiers.Core.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenIddict.Validation.AspNetCore;
using Quartz;
using System.Reflection;
using System.Text.Json.Serialization;

namespace GeoMarker.Api
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    options.SetIssuer(Configuration.GetValue<string>("AuthUrl"));
                    options.AddAudiences("geocodeapi");
                    options.AddEncryptionKey(new SymmetricSecurityKey(
                          Convert.FromBase64String(Configuration.GetValue<string>("SymmetricSecurityKey"))));
                    options.Configure(options => options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                                     Convert.FromBase64String(Configuration.GetValue<string>("SymmetricSecurityKey"))));
                    options.Configure(options => options.TokenValidationParameters.TokenDecryptionKey = new SymmetricSecurityKey(
                                         Convert.FromBase64String(Configuration.GetValue<string>("SymmetricSecurityKey"))));

                    options.UseSystemNetHttp();

                    options.UseAspNetCore();
                });

            services.Configure<FileMetadata>(Configuration.GetSection("FileMetadata"));
            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

            services.AddControllers()
                     .AddJsonOptions(x =>
                     {
                         x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                     });

            services.AddEndpointsApiExplorer();

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "GeoMarker API",
                    Description = "An ASP.NET Core Web API for geomarking address data"
                });

                options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "AzureAD Oauth Authorization",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            },
                            Scheme = "apiKey",
                            Name = "Bearer",
                            In = ParameterLocation.Header,
                        },
                        new List<string>()
                    }
                });

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
            });

            services.AddAuthorization(authopt =>
            {
                authopt.AddPolicy("MustBeAuthenticated", polBuilder =>
                {
                    polBuilder
                    .RequireAuthenticatedUser();
                });
            });

            services.AddLocalization()
                    .AddSingleton<IDeGaussCommandService, DeGaussCommandService>()
                    .AddSingleton<Matcher>(x =>
                    {
                        var matcher = new Matcher();
                        matcher.AddInclude("*geocoder*.csv");
                        return matcher;
                    })
                    .AddScoped<IValidator<DeGaussRequest>, DeGaussRequestValidator>()
                    .AddScoped<IValidator<DeGaussJsonRequest>, DeGaussJsonRequestValidator>()
                    .AddScoped<IValidator<DeGaussAsyncRequest>, DeGaussAsyncRequestValidator>();
            services.AddHealthChecks();

            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = int.MaxValue;
            });
            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.UseXmlSchedulingConfiguration(x =>
                {
                    x.Files = new[] { Configuration.GetValue<string>("QuartzSchedulePath") };
                });
            }).AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            int? gracePeriod = Configuration.GetValue<int?>("GracePeriod");
            if (gracePeriod != null)
            {
                services.Configure<CommandServiceConfiguration>(x =>
                {
                    x.GracePeriod = (int)gracePeriod;
                });
            }
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseHealthChecks("/health");
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseStatusCodePages();

            if (Configuration.GetValue<bool>("UseHttpsRedirection"))
                app.UseHttpsRedirection();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            // Serilog flat file logging must be configured with ILoggerFactory
            var logFile = Configuration.GetValue<string>("LogFile");
            if (!string.IsNullOrEmpty(logFile))
            {
                loggerFactory.AddFile(logFile);
            }
        }
    }
}
