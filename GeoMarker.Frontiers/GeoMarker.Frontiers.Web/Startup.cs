using FluentValidation;
using GeoMarker.Frontiers.Core.HealthCheck;
using GeoMarker.Frontiers.Core.Infrastructure.EmailNotifications;
using GeoMarker.Frontiers.Core.Models;
using GeoMarker.Frontiers.Core.Models.Request;
using GeoMarker.Frontiers.Core.Models.Request.Validation;
using GeoMarker.Frontiers.Web.Clients;
using GeoMarker.Frontiers.Web.Data;
using GeoMarker.Frontiers.Web.Extensions;
using GeoMarker.Frontiers.Web.Models.Configuration;
using GeoMarker.Frontiers.Web.Models.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using MySqlConnector;
using Quartz;
using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;

namespace GeoMarker.Frontiers.Web
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Env { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            Env = env;

            SetCompanyAttributes(Configuration);
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardLimit = null;
                options.ForwardedHeaders = ForwardedHeaders.All;
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
            });
            services.AddControllersWithViews()
                    .AddJsonOptions(x =>
                    {
                        x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    });
            services.AddOptions();
            services.AddGeoMarkerIdentityServer(Configuration);
            services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = int.MaxValue;
            });

            services.AddHttpContextAccessor();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var discoveryUrls = Configuration.GetSection("ServiceDiscovery");

            services.AddSingleton((provider) =>
            {
                var baseUrl = discoveryUrls.GetValue<string>("GeoCode");
                return new GeoCodeClient(baseUrl, new HttpClient());
            });

            services.AddSingleton((provider) =>
            {
                var baseUrl = discoveryUrls.GetValue<string>("CensusBlockGroup");
                return new CensusBlockGroupClient(baseUrl, new HttpClient());
            });

            services.AddSingleton((provider) =>
            {
                var baseUrl = discoveryUrls.GetValue<string>("DriveTime");
                return new DriveTimeClient(baseUrl, new HttpClient());
            });

            services.AddSingleton((provider) =>
            {
                var baseUrl = discoveryUrls.GetValue<string>("DepIndex");
                return new DeprivationIndexClient(baseUrl, new HttpClient());
            });

            services.Configure<Models.WebApplication>(Configuration.GetSection("WebApplication"));

            services.Configure<FileMetadata>(Configuration.GetSection("FileMetadata"));

            services.AddSession();
            services.AddScoped<IValidator<DeGaussRequest>, DeGaussRequestValidator>()
                    .AddScoped<IValidator<DeGaussDrivetimeRequest>, DeGaussDrivetimeRequestValidator>()
                    .AddScoped<IValidator<DeGaussCensusBlockGroupRequest>, DeGaussCensusBlockGroupRequestValidator>()
                    .AddScoped<IValidator<Core.Models.Request.DeGaussJsonRequest>, DeGaussJsonRequestValidator>()
                    .AddScoped<IValidator<Core.Models.Request.DeGaussGeocodedJsonRequest>, DeGaussGeocodedJsonRequestValidator>()
                    .AddScoped<IValidator<Core.Models.Request.DeGaussCensusBlockGroupsJsonRequest>, DeGaussCensusBlockGroupsJsonRequestValidator>()
                    .AddScoped<IValidator<Core.Models.Request.DeGaussDriveTimesJsonRequest>, DeGaussDriveTimesJsonRequestValidator>()
                    .AddScoped<IValidator<DeGaussCompositeJsonRequest>, DeGaussCompositeJsonRequestValidator>();

            services.AddSingleton<IPingService, PingService>();
            AddDbContext(services, Configuration);

            var authUrl = Configuration.GetValue<string>("AuthUrl");
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "GeoMarker API Gateway", Version = "v1" });
                c.AddSecurityDefinition("OAuth", new OpenApiSecurityScheme()
                {
                    OpenIdConnectUrl = new Uri(authUrl + "/.well-known/openid-configuration"),
                    Type = SecuritySchemeType.OAuth2,
                    In = ParameterLocation.Header,
                    Name = HeaderNames.Authorization,
                    Flows = new OpenApiOAuthFlows()
                    {
                        ClientCredentials = new OpenApiOAuthFlow()
                        {
                            TokenUrl = new Uri(authUrl + "connect/token"),
                            AuthorizationUrl = new Uri(authUrl + "connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "geocode", "Geocodes address data." },
                                { "drivetime", "Append drivetime data." },
                                { "censusblock", "Append census block group data." },
                                { "deprivationindex", "Append deprivation index data." }
                            }
                        }
                    }
                });

                c.AddSecurityRequirement(
                    new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                    { Type = ReferenceType.SecurityScheme, Id = "OAuth" },
                            },
                            new[] { "geocode", "drivetime", "censusblock", "deprivationindex" }
                        }
                    }
                );

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
                c.EnableAnnotations();
            });

            services.AddEmailSetting(Configuration).AddEmailServices(Configuration);

            services.AddQuartz(q =>
            {
                q.UseMicrosoftDependencyInjectionJobFactory();
                q.UseXmlSchedulingConfiguration(x =>
                {
                    x.Files = new[] { Configuration.GetValue<string>("QuartzSchedulePath") };
                });
            }).AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

            services.AddAuthorization(options =>
            {
                options.AddPolicy("GeoMarkerUserPolicy", builder =>
                {
                    builder.RequireAuthenticatedUser();
                    builder.RequireRole("Admin", "GeoMarker");
                });

                options.AddPolicy("AdminUserPolicy", builder =>
                {

                    builder.RequireAuthenticatedUser();
                    builder.RequireRole("Admin");
                });
            });

            services.AddScoped<IMetadataService, MetadataService>()
                    .AddScoped<IUserRequestRepository, UserRequestRepository>()
                    .AddScoped<IGeoMarkerAPIRequestService, GeoMarkerAPIRequestService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseForwardedHeaders();
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                IdentityModelEventSource.ShowPII = true;
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
            app.UseMiddleware<CustomErrorHandler>();
            app.UseSession();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapDefaultControllerRoute();
            });
            app.UseSwagger();
            app.UseSwaggerUI();
            using (var scope = app.ApplicationServices.CreateScope())
            using (var context = scope.ServiceProvider.GetService<UserRequestsDbContext>())
                context.Database.EnsureCreated();

            // Serilog flat file logging must be configured with ILoggerFactory
            var logFile = Configuration.GetValue<string>("LogFile");
            if (!string.IsNullOrEmpty(logFile))
            {
                loggerFactory.AddFile(logFile);
            }
        }

        private static void SetCompanyAttributes(IConfiguration config)
        {
            Company.Name = config.GetValue<string>("COMPANY_NAME");
            Company.LogoURL = config.GetValue<string>("COMPANY_LOGO_URL");
            Company.LogoWidth = config.GetValue<string>("COMPANY_LOGO_WIDTH");
            Company.LogoHeight = config.GetValue<string>("COMPANY_LOGO_HEIGHT");
            Company.FaviconUrl = config.GetValue<string>("COMPANY_FAVICON_URL");
            Company.SupportContactInformation = config.GetValue<string>("COMPANY_SupportContactInformation");
            if (Enum.TryParse(config.GetValue<string>("COMPANY_LOGO_POSITION"),
                true, out Company.Position position))
                Company.LogoPosition = position;
        }

        private static void AddDbContext(IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = "server=" + configuration.GetValue<string>("DBServer") + ";user=" + configuration.GetValue<string>("DBUser") +
            ";password=" + configuration.GetValue<string>("DBPassword") + ";database=" + configuration.GetValue<string>("Database");

            WaitForConnection(connectionString);

            var serverVersion = ServerVersion.AutoDetect(connectionString);

            services.AddDbContext<UserRequestsDbContext>(options =>
            {
                options.UseMySql(connectionString, serverVersion);
            });
        }

        private static void WaitForConnection(string connectionString)
        {
            var connection = new MySqlConnection(connectionString);

            int retryCounter = 1;

            while (retryCounter < 10)
            {
                try
                {
                    connection.Open();
                    connection.Close();
                    break;
                }
                catch (MySqlException)
                {
                    Thread.Sleep(2000);
                    retryCounter++;
                }
            }
        }

    }
}
