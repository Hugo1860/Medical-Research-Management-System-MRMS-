using System;
using System.IO;
using System.Net;
using System.Reflection;
using log4net;
using log4net.Config;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Dapper;
using ScientificResearch.Infrastucture;
using ScientificResearch.Infrastucture.Storage;

namespace ScientificResearch
{
    /// <summary>
    /// Application services and HTTP pipeline configuration.
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // PostgreSQL date values are surfaced as DateOnly by Npgsql 10, while
            // the existing generated models use DateTime?/DateTime.
            SqlMapper.AddTypeHandler(new NpgsqlDateTimeTypeHandler());

            var keycloak = KeycloakOptions.FromConfiguration(Configuration);
            services.AddSingleton(keycloak);
            services.AddScoped<IKeycloakUserResolver, KeycloakUserResolver>();

            if (keycloak.Enabled)
            {
                if (string.IsNullOrWhiteSpace(keycloak.Authority) ||
                    string.IsNullOrWhiteSpace(keycloak.Audience) ||
                    string.IsNullOrWhiteSpace(keycloak.ClientId))
                {
                    throw new InvalidOperationException("Keycloak 已启用，但未配置 Authority、Audience 或 ClientId");
                }

                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.Authority = keycloak.Authority;
                    options.Audience = keycloak.Audience;
                    options.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;
                    options.MapInboundClaims = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        NameClaimType = keycloak.UserIdentifierClaim
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                            logger.LogDebug("Keycloak bearer token received: {HasToken}", !string.IsNullOrWhiteSpace(context.Token));
                            return Task.CompletedTask;
                        },
                        OnTokenValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                            var authorizedParty = context.Principal?.FindFirst("azp")?.Value;
                            if (!string.Equals(authorizedParty, keycloak.ClientId, StringComparison.Ordinal))
                            {
                                logger.LogWarning("Keycloak token authorized party mismatch: {AuthorizedParty}", authorizedParty ?? "missing");
                                context.Fail("Keycloak token client mismatch");
                                return Task.CompletedTask;
                            }

                            if (keycloak.RequireMfa)
                            {
                                var acceptedAmr = keycloak.MfaAmr
                                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                                var tokenAmr = context.Principal?.FindAll("amr")
                                    .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
                                    ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                if (acceptedAmr.Count == 0 || !acceptedAmr.Overlaps(tokenAmr))
                                {
                                    logger.LogWarning("Keycloak token MFA claim missing or not accepted: {Amr}", string.Join(",", tokenAmr));
                                    context.Fail("Keycloak token MFA requirement not satisfied");
                                    return Task.CompletedTask;
                                }
                            }

                            logger.LogDebug("Keycloak token validated for {Subject}", context.Principal?.FindFirst("sub")?.Value ?? "unknown");
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Startup>>();
                            logger.LogWarning("Keycloak token validation failed: {ExceptionType}", context.Exception.GetType().Name);
                            return Task.CompletedTask;
                        }
                    };
                });
            }

            services.AddAuthorization();
            var redisConfiguration = Configuration.GetValue<string>("Redis:Configuration");
            var redisEnabled = !string.IsNullOrWhiteSpace(redisConfiguration);
            var healthChecks = services.AddHealthChecks()
                .AddCheck<PostgresReadinessCheck>("postgres", tags: new[] { "ready" })
                .AddCheck<KeycloakReadinessCheck>("keycloak", tags: new[] { "ready", "keycloak" });
            if (redisEnabled)
            {
                healthChecks.AddCheck<RedisReadinessCheck>("redis", tags: new[] { "ready", "redis" });
            }
            services.AddSingleton<ILog>(_ =>
            {
                var repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
                if (!repository.Configured)
                {
                    var logConfig = Path.Combine(AppContext.BaseDirectory, "log4net.xml");
                    XmlConfigurator.Configure(repository, new FileInfo(logConfig));
                }

                return LogManager.GetLogger(repository.Name, "ScientificResearch");
            });

            services.AddHttpContextAccessor();
            services.AddSingleton<IDatabaseConnectionFactory, DatabaseConnectionFactory>();
            services.AddHttpClient("keycloak", client => client.Timeout = TimeSpan.FromSeconds(5));
            services.AddHttpClient();

            // 方案B：对象存储驱动注入 (MinIO / S3 / Local)
            var storageConfig = new StorageConfig();
            Configuration.GetSection("Storage").Bind(storageConfig);
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Storage__Provider")))
            {
                storageConfig.Provider = Environment.GetEnvironmentVariable("Storage__Provider")!;
            }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Storage__Minio__Endpoint")))
            {
                storageConfig.Minio.Endpoint = Environment.GetEnvironmentVariable("Storage__Minio__Endpoint")!;
            }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Storage__Minio__ExternalEndpoint")))
            {
                storageConfig.Minio.ExternalEndpoint = Environment.GetEnvironmentVariable("Storage__Minio__ExternalEndpoint")!;
            }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Storage__Minio__AccessKey")))
            {
                storageConfig.Minio.AccessKey = Environment.GetEnvironmentVariable("Storage__Minio__AccessKey")!;
            }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Storage__Minio__SecretKey")))
            {
                storageConfig.Minio.SecretKey = Environment.GetEnvironmentVariable("Storage__Minio__SecretKey")!;
            }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Storage__Minio__BucketName")))
            {
                storageConfig.Minio.BucketName = Environment.GetEnvironmentVariable("Storage__Minio__BucketName")!;
            }
            if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Storage__Minio__UseSSL")))
            {
                storageConfig.Minio.UseSSL = bool.TryParse(Environment.GetEnvironmentVariable("Storage__Minio__UseSSL"), out var ssl) && ssl;
            }

            services.AddSingleton(storageConfig);
            services.AddSingleton<IStorageProvider>(sp =>
            {
                var cfg = sp.GetRequiredService<StorageConfig>();
                var httpFactory = sp.GetRequiredService<IHttpClientFactory>();
                var env = sp.GetRequiredService<IWebHostEnvironment>();
                if (string.Equals(cfg.Provider, "Minio", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(cfg.Provider, "S3", StringComparison.OrdinalIgnoreCase))
                {
                    return new MinioStorageProvider(cfg.Minio, httpFactory.CreateClient());
                }
                return new LocalStorageProvider(cfg.Local, env.WebRootPath);
            });

            services.AddHostedService<ExternalSyncWorker>();
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                                           ForwardedHeaders.XForwardedProto;
                var trustedProxies = Configuration.GetSection("ForwardedHeaders:TrustedProxies").Get<string[]>()
                    ?? Array.Empty<string>();
                foreach (var proxy in trustedProxies)
                {
                    if (!IPAddress.TryParse(proxy, out var address))
                        throw new InvalidOperationException($"ForwardedHeaders:TrustedProxies 包含无效地址: {proxy}");
                    options.KnownProxies.Add(address);
                }
            });
            if (string.IsNullOrWhiteSpace(redisConfiguration))
            {
                services.AddDistributedMemoryCache();
            }
            else
            {
                services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConfiguration;
                    options.InstanceName = Configuration.GetValue<string>("Redis:InstanceName")
                        ?? "ScientificResearch:";
                });
            }
            services.AddResponseCompression();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            });

            services.AddMvc(options =>
            {
                options.Filters.Add(new MyAuthorizationFilter());
                options.Filters.Add(new MyActionFilter());
                options.Filters.Add(new MyResultFilter());
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateFormatString = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            });

            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "科研管理系统-API",
                    Version = "v1"
                });

                var filePath = Path.Combine(AppContext.BaseDirectory, "ScientificResearchApi.xml");
                if (File.Exists(filePath))
                {
                    options.IncludeXmlComments(filePath);
                }
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILog log)
        {
            // Restore the HTTPS scheme when TLS is terminated by Nginx.
            app.UseForwardedHeaders();

            app.Use(async (context, next) =>
            {
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
                    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
                    return Task.CompletedTask;
                });
                await next();
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
                app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
                {
                    var feature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = feature?.Error;
                    var isAuthRequired = exception is UnauthorizedAccessException uae &&
                        (uae.Message.Contains("请先登录") || uae.Message.Contains("未登录") || uae.Message.Contains("凭证已失效"));
                    var statusCode = exception is ForbiddenAccessException
                        ? StatusCodes.Status403Forbidden
                        : isAuthRequired
                            ? StatusCodes.Status401Unauthorized
                            : exception is ArgumentException
                                ? StatusCodes.Status400BadRequest
                                : StatusCodes.Status500InternalServerError;

                    context.Response.StatusCode = statusCode;
                    context.Response.ContentType = "application/json; charset=utf-8";
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(new
                    {
                        error = statusCode == StatusCodes.Status403Forbidden
                            ? "当前人员无权执行该操作"
                            : statusCode == StatusCodes.Status401Unauthorized
                                ? "未登录或登录已过期"
                                : statusCode == StatusCodes.Status400BadRequest
                                    ? exception?.Message ?? "请求参数无效"
                                    : (exception?.Message != null && exception.Message.Contains("Access to the path")
                                        ? "服务器存储目录权限异常，请联系管理员检查上传目录权限"
                                        : "服务器处理请求时发生错误")
                    }));
                }));
            }

            app.UseMiddleware<RequestIdMiddleware>();

            app.Use(async (context, next) =>
            {
                try
                {
                    await next();
                }
                catch (Exception exception)
                {
                    // Do not log request bodies, cookies or authorization headers.
                    log.Error($"请求失败: {context.Request.Method} {context.Request.Path} ({exception.GetType().Name})", exception);
                    throw;
                }
            });

            app.UseResponseCompression();
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();

            if (Configuration.GetValue<bool>("Keycloak:Enabled"))
            {
                app.UseAuthentication();
                app.UseMiddleware<KeycloakUserMiddleware>();
                app.UseAuthorization();
            }

            app.UseMiddleware<AuditMiddleware>();

            if (Configuration.GetValue("Swagger:Enabled", env.IsDevelopment()))
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "ScientificResearchApi-v1");
                });
            }

            // Vue Router uses history mode. Serve the modern SPA shell for
            // browser deep links while leaving API, health, Swagger and the
            // legacy /IMIS pages to their normal handlers.
            app.Use(async (context, next) =>
            {
                await next();
                var path = context.Request.Path.Value ?? string.Empty;
                var acceptsHtml = context.Request.Headers.Accept.Any(value => value.Contains("text/html", StringComparison.OrdinalIgnoreCase));
                var isDeepLink = !path.StartsWith("/IMIS", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
                    && !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
                    && !Path.HasExtension(path);
                if (!context.Response.HasStarted && context.Response.StatusCode == StatusCodes.Status404NotFound
                    && (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                    && acceptsHtml && isDeepLink)
                {
                    var index = env.WebRootFileProvider.GetFileInfo("index.html");
                    if (index.Exists)
                    {
                        context.Response.Clear();
                        context.Response.StatusCode = StatusCodes.Status200OK;
                        context.Response.ContentType = "text/html; charset=utf-8";
                        context.Response.ContentLength = index.Length;
                        if (HttpMethods.IsGet(context.Request.Method))
                        {
                            await using var stream = index.CreateReadStream();
                            await stream.CopyToAsync(context.Response.Body);
                        }
                    }
                }
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                {
                    Predicate = _ => false
                });
                endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("ready")
                });
                endpoints.MapHealthChecks("/health/keycloak", new HealthCheckOptions
                {
                    Predicate = check => check.Tags.Contains("keycloak")
                });
                endpoints.MapControllers();
            });
        }
    }
}
