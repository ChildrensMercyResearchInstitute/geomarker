using GeoMarker.Frontiers.Web.Data;
using GeoMarker.Frontiers.Web.Models.Clients;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Client;
using Quartz;

namespace GeoMarker.Frontiers.Web.Extensions
{
    public static class IdentityServerServices
    {
        public static void AddGeoMarkerIdentityServer(this IServiceCollection services, IConfiguration configuration)
        {
            ApplicationRegistration applicationRegistration = new ApplicationRegistration();

            applicationRegistration.Issuer = configuration.GetValue<string>("AuthUrl");

            configuration.GetSection("WebApplicationRegistration").Bind(applicationRegistration);

            var clientRegistration = new OpenIddictClientRegistration();
            clientRegistration.Issuer = new Uri(applicationRegistration.Issuer, UriKind.Absolute);
            clientRegistration.ClientId = applicationRegistration.ClientId;
            clientRegistration.ClientSecret = applicationRegistration.ClientSecret;
            if (applicationRegistration.ClientSecret != null)
                foreach (var item in applicationRegistration.Scopes)
                {
                    clientRegistration.Scopes.Add(item);
                }
            clientRegistration.RedirectUri = new Uri(applicationRegistration.RedirectUri, UriKind.Relative);
            clientRegistration.PostLogoutRedirectUri = new Uri(applicationRegistration.PostLogoutRedirectUri, UriKind.Relative);

            services.AddAuthentication(options =>
                    {
                        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                    })
                   .AddCookie(options =>
                   {

                       options.LoginPath = "/login";
                       options.LogoutPath = "/logout";
                       options.ExpireTimeSpan = TimeSpan.FromMinutes(50);
                       options.SlidingExpiration = false;

                       options.Events.OnRedirectToAccessDenied = new Func<RedirectContext<CookieAuthenticationOptions>, Task>(context =>
                       {
                           context.Response.Redirect($"/AccessDenied?statusCode=403&failedUrl={context.Request.Path}");
                           return context.Response.CompleteAsync();
                       });
                   })
                   .AddJwtBearer("geocodeclient", options =>
                   {
                       options.RequireHttpsMetadata = false;
                       options.Authority = applicationRegistration.Issuer;
                       options.TokenValidationParameters = new TokenValidationParameters
                       {
                           ValidateIssuer = true,
                           ValidateAudience = true,
                           ValidAudiences = new List<string> { "geocodeapi" },
                           TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey"))),
                           IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey")))
                       };
                   })
                   .AddJwtBearer("drivetimeclient", options =>
                   {
                       options.RequireHttpsMetadata = false;
                       options.Authority = applicationRegistration.Issuer;
                       options.TokenValidationParameters = new TokenValidationParameters
                       {
                           ValidateIssuer = true,
                           ValidateAudience = true,
                           ValidAudiences = new List<string> { "drivetimeapi" },
                           TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey"))),
                           IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey")))
                       };
                   })
                   .AddJwtBearer("censusblockclient", options =>
                   {
                       options.RequireHttpsMetadata = false;
                       options.Authority = applicationRegistration.Issuer;
                       options.TokenValidationParameters = new TokenValidationParameters
                       {
                           ValidateIssuer = true,
                           ValidateAudience = true,
                           ValidAudiences = new List<string> { "censusblockapi" },
                           TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey"))),
                           IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey")))
                       };
                   })
                   .AddJwtBearer("deprivationindexclient", options =>
                   {
                       options.RequireHttpsMetadata = false;
                       options.Authority = applicationRegistration.Issuer;
                       options.TokenValidationParameters = new TokenValidationParameters
                       {
                           ValidateIssuer = true,
                           ValidateAudience = true,
                           ValidAudiences = new List<string> { "deprivationindexapi" },
                           TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey"))),
                           IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey")))
                       };
                   })
                   .AddJwtBearer("geomarkerclient", options =>
                   {
                       options.RequireHttpsMetadata = false;
                       options.Authority = applicationRegistration.Issuer;
                       options.TokenValidationParameters = new TokenValidationParameters
                       {
                           ValidateIssuer = true,
                           ValidateAudience = true,
                           ValidAudiences = new List<string> { "geomarker" },
                           TokenDecryptionKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey"))),
                           IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey")))
                       };
                   });

            services.AddQuartz(options =>
                    {
                        options.UseMicrosoftDependencyInjectionJobFactory();
                        options.UseSimpleTypeLoader();
                        options.UseInMemoryStore();
                    });

            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            services.AddOpenIddict()
                    .AddClient(options =>
                    {
                        options.DisableTokenStorage();
                        options.AllowAuthorizationCodeFlow();

                        options.AddEncryptionKey(new SymmetricSecurityKey(
                                 Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey"))));
                        options.AddSigningKey(new SymmetricSecurityKey(
                                Convert.FromBase64String(configuration.GetValue<string>("SymmetricSecurityKey"))));

                        options.UseAspNetCore()
                                .EnableStatusCodePagesIntegration()
                                .EnableRedirectionEndpointPassthrough()
                                .EnablePostLogoutRedirectionEndpointPassthrough();
                        bool isDevelopment = (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development") == "Development";
                        if (isDevelopment)
                        {
                            options.UseAspNetCore().DisableTransportSecurityRequirement();
                        }
                        options.UseSystemNetHttp()
                               .SetProductInformation(typeof(Startup).Assembly);
                        options.AddRegistration(clientRegistration);
                    }).AddValidation();

            services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(@"temp-keys")).SetApplicationName("GeoMarker.Frontiers.Web");
            ClientCredentials credentials = new ClientCredentials();
            configuration.GetSection("ClientCredentials").Bind(credentials);
            services.AddSingleton<ClientCredentials>(credentials);
        }

    }
}
