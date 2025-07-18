using Amazon.Runtime;
using Amazon.S3;
using Cfo.Cats.Application.Common.Interfaces.Contracts;
using Cfo.Cats.Application.Common.Interfaces.Locations;
using Cfo.Cats.Application.Common.Interfaces.MultiTenant;
using Cfo.Cats.Application.Common.Interfaces.Serialization;
using Cfo.Cats.Application.Features.Documents.IntegrationEventHandlers;
using Cfo.Cats.Application.Features.Identity.Notifications.IdentityEvents;
using Cfo.Cats.Application.Features.ManagementInformation.IntegrationEventHandlers;
using Cfo.Cats.Application.Features.Participants.MessageBus;
using Cfo.Cats.Application.Features.PRIs.IntegrationEventHandlers;
using Cfo.Cats.Application.SecurityConstants;
using Cfo.Cats.Domain.Identity;
using Cfo.Cats.Infrastructure.Configurations;
using Cfo.Cats.Infrastructure.Constants.ClaimTypes;
using Cfo.Cats.Infrastructure.Jobs;
using Cfo.Cats.Infrastructure.Persistence.Interceptors;
using Cfo.Cats.Infrastructure.Services.Candidates;
using Cfo.Cats.Infrastructure.Services.Contracts;
using Cfo.Cats.Infrastructure.Services.Delius;
using Cfo.Cats.Infrastructure.Services.Locations;
using Cfo.Cats.Infrastructure.Services.MultiTenant;
using Cfo.Cats.Infrastructure.Services.OffLoc;
using Cfo.Cats.Infrastructure.Services.Ordnance;
using Cfo.Cats.Infrastructure.Services.Serialization;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.AspNetCore;
using ZiggyCreatures.Caching.Fusion;

namespace Cfo.Cats.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddSettings(configuration)
            .AddDatabase(configuration)
            .AddServices(configuration);

        services.AddAuthenticationService(configuration)
            .AddFusionCacheService();

        services.AddSingleton<IUsersStateContainer, UsersStateContainer>();
        services.AddScoped<INetworkIpProvider, NetworkIpProvider>();

        services.AddScoped<INotificationHandler<IdentityAuditNotification>, IdentityCacheClearanceHandler>();

        return services;
    }

    private static IServiceCollection AddSettings(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var options = configuration.GetAWSOptions();

        string? accessKey = configuration.GetValue<string>("AWS:AccessKey");
        string? secretKey = configuration.GetValue<string>("AWS:SecretKey");

        if (string.IsNullOrEmpty(accessKey) is false && string.IsNullOrEmpty(secretKey) is false)
        {
            options.Credentials = new BasicAWSCredentials(accessKey, secretKey);
        }

        services.AddDefaultAWSOptions(options);

        services
            .Configure<IdentitySettings>(configuration.GetSection(IdentitySettings.Key))
            .AddSingleton(s => s.GetRequiredService<IOptions<IdentitySettings>>().Value)
            .AddSingleton<IIdentitySettings>(s =>
                s.GetRequiredService<IOptions<IdentitySettings>>().Value
            );

        services.Configure<AppConfigurationSettings>(configuration.GetSection(AppConfigurationSettings.Key))
            .AddSingleton(s => s.GetRequiredService<IOptions<AppConfigurationSettings>>().Value)
            .AddSingleton<IApplicationSettings>(s => s.GetRequiredService<IOptions<AppConfigurationSettings>>().Value);

        services
            .Configure<DatabaseSettings>(configuration.GetSection(DatabaseSettings.Key))
            .AddSingleton(s => s.GetRequiredService<IOptions<DatabaseSettings>>().Value);

        services.Configure<RightToWorkSettings>(configuration.GetSection(RightToWorkSettings.Key))
            .AddSingleton(s => s.GetRequiredService<IOptions<RightToWorkSettings>>().Value)
            .AddSingleton<IRightToWorkSettings>(s =>
                s.GetRequiredService<RightToWorkSettings>()
            );

        services.AddMassTransit(x =>
        {

            x.AddConsumers(typeof(RecordEnrolmentPaymentConsumer).Assembly); // Automatically add all consumers



            if(configuration.GetConnectionString("rabbit")!.Equals("InMemory", StringComparison.CurrentCultureIgnoreCase))
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConcurrencyLimit(1); // all consumers should be limited to 1 unless otherwise specified

                    cfg.ConfigureEndpoints(context);

                    // Override for specific consumer with a custom concurrency limit
                    cfg.ReceiveEndpoint("overnight-service", e =>
                    {
                        e.Consumer<SyncParticipantCommandHandler>(context, c =>
                        {
                            c.UseConcurrencyLimit(5); // Custom concurrency limit for this consumer
                        });
                    });


                });
            }
            else
            {
                x.UsingRabbitMq((context, cfg) =>
                {

                    cfg.Host(configuration.GetConnectionString("rabbit"));

                    cfg.ReceiveEndpoint("overnight-service", e =>
                    {
                        e.PrefetchCount = 64;
                        e.ConcurrentMessageLimit = 5;
                        e.ConfigureConsumer<SyncParticipantCommandHandler>(context);
                    });

                    cfg.ReceiveEndpoint("tasks-service", e =>
                    {
                        e.PrefetchCount = 64;
                        e.ConcurrentMessageLimit = 5;

                        e.ConfigureConsumer<PriTaskCompletedWatcherConsumer>(context);
                        e.ConfigureConsumer<RaisePaymentsAfterApprovalConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("payment-service", e =>
                    {
                        e.ConcurrentMessageLimit = 1;

                        e.ConfigureConsumer<RecordActivityPaymentConsumer>(context);
                        e.ConfigureConsumer<RecordEducationPayment>(context);
                        e.ConfigureConsumer<RecordEmploymentPayment>(context);
                        e.ConfigureConsumer<RecordEnrolmentPaymentConsumer>(context);
                        e.ConfigureConsumer<RecordHubInductionPaymentConsumer>(context);
                        e.ConfigureConsumer<RecordPreReleaseSupportPayment>(context);
                        e.ConfigureConsumer<RecordThroughTheGatePaymentConsumer>(context);
                        e.ConfigureConsumer<RecordWingInductionPaymentConsumer>(context);
                        e.ConfigureConsumer<RecordReassessmentPaymentConsumer>(context);
                    });

                    cfg.ReceiveEndpoint("document-service", e =>
                    {
                        e.ConcurrentMessageLimit = 1;

                        e.ConfigureConsumer<DocumentExportCaseWorkloadIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportKeyValuesIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportRiskDueAggregateIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportRiskDueIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportParticipantsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportPqaActivitiesIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportPqaEnrolmentsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportActivityPaymentsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportEducationPaymentsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportEmploymentPaymentsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportEnrolmentPaymentsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportInductionPaymentsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportSupportAndReferralPaymentsIntegrationEventConsumer>(context);
                        e.ConfigureConsumer<DocumentExportCumulativesIntegrationEventConsumer>(context);
                    });

                });
            }

        });

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();

        services.AddDbContext<ApplicationDbContext>(
            (p, m) => {
                m.AddInterceptors(p.GetServices<ISaveChangesInterceptor>());
                m.UseSqlServer(configuration.GetConnectionString("CatsDb")!);
            });

        services.AddDbContextFactory<ApplicationDbContext>((serviceProvider, optionsBuilder) =>
        {
            optionsBuilder.AddInterceptors(serviceProvider.GetServices<ISaveChangesInterceptor>());
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("CatsDb")!,
                options =>
                {
                    // options.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
        }, ServiceLifetime.Scoped);

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IApplicationDbContext, ApplicationDbContext>();

        services.AddScoped<ApplicationDbContextInitializer>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        return services;
    }

    private static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<PicklistService>()
            .AddSingleton<IPicklistService>(sp => {
                var service = sp.GetRequiredService<PicklistService>();
                var cache = sp.GetRequiredService<IFusionCache>();
                var logger = sp.GetRequiredService<ILogger<CachingPicklistService>>();

                return new CachingPicklistService(cache, service, logger);
            })
            .AddSingleton<TenantService>()
            .AddSingleton<ITenantService>(sp =>
            {
                var service = sp.GetRequiredService<TenantService>();
                var cache = sp.GetRequiredService<IFusionCache>();
                var logger = sp.GetRequiredService<ILogger<CachingTenantService>>();

                return new CachingTenantService(cache, service, logger);
            })
            .AddSingleton<LocationService>()
            .AddSingleton<ILocationService>(sp =>
            {
                var service = sp.GetRequiredService<LocationService>();
                var cache = sp.GetRequiredService<IFusionCache>();
                var logger = sp.GetRequiredService<ILogger<CachingLocationService>>();

                return new CachingLocationService(cache, service, logger);
            })
            .AddSingleton<ContractService>()
            .AddSingleton<IContractService>(sp =>
            {
                var service = sp.GetRequiredService<ContractService>();
                var cache = sp.GetRequiredService<IFusionCache>();
                var logger = sp.GetRequiredService<ILogger<CachingContractService>>();

                return new CachingContractService(cache, service, logger);
            });


        services.Configure<NotifyOptions>(configuration.GetSection(NotifyOptions.Notify));

        services.AddAWSService<IAmazonS3>();

        if(configuration.GetValue<bool>("UseDummyCandidateService"))
        {
            services.AddSingleton<ICandidateService, DummyCandidateService>();
        }
        else
        {
            services.AddHttpClient<ICandidateService, CandidateService>((provider, client) =>
            {
                client.DefaultRequestHeaders.Add("X-API-KEY", configuration.GetRequiredValue("DMS:ApiKey"));
                client.BaseAddress = new Uri(configuration.GetRequiredValue("DMS:ApplicationUrl"));
            });
        }

        services.AddHttpClient<OfflocService>((_, client) =>
        {
            client.DefaultRequestHeaders.Add("X-API-KEY", configuration.GetRequiredValue("DMS:ApiKey"));
            client.BaseAddress = new Uri(configuration.GetRequiredValue("DMS:ApplicationUrl"));
        });

        services.AddHttpClient<DeliusService>((_, client) =>
        {
            client.DefaultRequestHeaders.Add("X-API-KEY", configuration.GetRequiredValue("DMS:ApiKey"));
            client.BaseAddress = new Uri(configuration.GetRequiredValue("DMS:ApplicationUrl"));
        });

        services.AddSingleton<IOfflocService>(sp =>
        {
            var offloc = sp.GetRequiredService<OfflocService>();
            var cache = sp.GetRequiredService<IFusionCache>();

            return new CachingOffLocService(cache, offloc);
        });

        services.AddSingleton<IDeliusService>(sp =>
        {
            var delius = sp.GetRequiredService<DeliusService>();
            var cache = sp.GetRequiredService<IFusionCache>();

            return new CachingDeliusService(cache, delius);
        });

        services.AddHttpClient<IAddressLookupService, AddressLookupService>((provider, client) =>
        {
            client.DefaultRequestHeaders.Add("key", configuration.GetRequiredValue("Ordnance:Places:ApiKey"));
            client.BaseAddress = new Uri(configuration.GetRequiredValue("Ordnance:Places:ApplicationUrl"));
        });

        services.AddQuartzJobsAndTriggers(configuration);

        return services
            .AddSingleton<ISerializer, SystemTextJsonSerializer>()
            .AddScoped<ICurrentUserService, CurrentUserService>()
            .AddScoped<ITenantProvider, TenantProvider>()
            .AddScoped<IValidationService, ValidationService>()
            .AddScoped<IDateTime, DateTimeService>()
            .AddScoped<ICommunicationsService, CommunicationsService>()
            .AddScoped<IExcelService, ExcelService>()
            .AddScoped<ICumulativeExcelService, CumulativeExcelService>()
            .AddScoped<IUploadService, UploadService>();
    }

    private static IServiceCollection AddAuthenticationService(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AllowlistOptions>(configuration.GetSection(nameof(AllowlistOptions)));

        services
            .AddIdentityCore<ApplicationUser>()
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddUserManager<ApplicationUserManager>()
            //.AddSignInManager()
            .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>()
            .AddDefaultTokenProviders();

        services.AddScoped<UserManager<ApplicationUser>, ApplicationUserManager>();
        services.AddScoped<SignInManager<ApplicationUser>, CustomSigninManager>();
        services.AddScoped<ISecurityStampValidator, SecurityStampValidator<ApplicationUser>>();

        services.Configure<IdentityOptions>(options => {
            var identitySettings = configuration
                .GetRequiredSection(IdentitySettings.Key)
                .Get<IdentitySettings>();

            // Password settings
            options.Password.RequireDigit = identitySettings!.RequireDigit;
            options.Password.RequiredLength = identitySettings.RequiredLength;
            options.Password.RequireNonAlphanumeric = identitySettings.RequireNonAlphanumeric;
            options.Password.RequireUppercase = identitySettings.RequireUpperCase;
            options.Password.RequireLowercase = identitySettings.RequireLowerCase;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromDays(identitySettings.DefaultLockoutTimeSpan * 365);
            options.Lockout.MaxFailedAccessAttempts = identitySettings.MaxFailedAccessAttempts;
            options.Lockout.AllowedForNewUsers = true;

            // Default SignIn settings.
            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedPhoneNumber = false;
            options.SignIn.RequireConfirmedAccount = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = identitySettings.AllowedUserNameCharacters;
            //options.Tokens.EmailConfirmationTokenProvider = "Email";
        });

        services
            .AddScoped<IIdentityService, IdentityService>()
            .AddAuthorizationCore(options => {

                options.AddPolicy(SecurityPolicies.Export, policy => {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager);
                });

                options.AddPolicy(SecurityPolicies.CandidateSearch, policy => {
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy(SecurityPolicies.DocumentUpload, policy => {
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy(SecurityPolicies.AuthorizedUser, policy => {
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy(SecurityPolicies.Enrol, policy => {
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireAuthenticatedUser();
                });

                options.AddPolicy(SecurityPolicies.Import, policy => {
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireAuthenticatedUser();
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager);
                });

                options.AddPolicy(SecurityPolicies.SystemFunctionsRead, policy => {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager, RoleNames.QAOfficer, RoleNames.QASupportManager);
                });

                options.AddPolicy(SecurityPolicies.SystemFunctionsWrite, policy => {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager, RoleNames.QAOfficer, RoleNames.QASupportManager);
                });

                options.AddPolicy(SecurityPolicies.Pqa, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAFinance);
                });

                options.AddPolicy(SecurityPolicies.Qa1, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager, RoleNames.QAOfficer, RoleNames.QASupportManager);
                });

                options.AddPolicy(SecurityPolicies.Qa2, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager, RoleNames.QASupportManager);
                });

                options.AddPolicy(SecurityPolicies.UserHasAdditionalRoles, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager, RoleNames.QAOfficer, RoleNames.QASupportManager, RoleNames.QAFinance, RoleNames.PerformanceManager);
                });

                options.AddPolicy(SecurityPolicies.SeniorInternal, policy => {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport, RoleNames.SMT, RoleNames.QAManager);
                });

                options.AddPolicy(SecurityPolicies.Internal, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireClaim(ApplicationClaimTypes.InternalStaff, "True");
                });

                options.AddPolicy(SecurityPolicies.ViewAudit, policy => {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport,
                        RoleNames.SMT,
                        RoleNames.QAManager
                        );
                });

                options.AddPolicy(SecurityPolicies.SystemSupportFunctions, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim(ApplicationClaimTypes.AccountLocked, "False");
                    policy.RequireRole(RoleNames.SystemSupport,
                        RoleNames.SystemSupport,
                        RoleNames.SMT
                    );
                });
              
            })
            .AddAuthentication(options => {
                options.DefaultScheme = IdentityConstants.ApplicationScheme;
                options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
            })
            .AddIdentityCookies(options => {});

        services.AddDataProtection().PersistKeysToDbContext<ApplicationDbContext>();

        services.AddSingleton<IPasswordService, PasswordService>();

        CookieSecurePolicy policy = CookieSecurePolicy.SameAsRequest;
        if(configuration["IdentitySettings:SecureCookies"] is not null && configuration["IdentitySettings:SecureCookies"]!.Equals("True", StringComparison.CurrentCultureIgnoreCase))
        {
            policy = CookieSecurePolicy.Always;
        }        

        services.ConfigureApplicationCookie(options => {
            options.LoginPath = "/pages/authentication/login";
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.Cookie.SecurePolicy = policy;
        });

        services.AddSingleton<UserService>();
        services.AddSingleton<IUserService>(sp => {
            var service = sp.GetRequiredService<UserService>();
            var cache = sp.GetRequiredService<IFusionCache>();
            var logger =sp.GetRequiredService<ILogger<CachingUserService>>();

            return new CachingUserService(cache, service, logger);
        });

        return services;
    }

    private static IServiceCollection AddQuartzJobsAndTriggers(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetRequiredSection("Quartz");

        services.Configure<QuartzOptions>(options);

        services.AddQuartz(quartz =>
        {
            if (options.GetSection(SyncParticipantsJob.Key.Name).Get<JobOptions>() is
                { Enabled: true } syncParticipantsJobOptions)
            {
                quartz.AddJob<SyncParticipantsJob>(opts =>
                    opts.WithIdentity(SyncParticipantsJob.Key)
                        .WithDescription(SyncParticipantsJob.Description)
                );

                quartz.AddTrigger(opts => opts
                    .ForJob(SyncParticipantsJob.Key)
                    .WithIdentity($"{SyncParticipantsJob.Key}-trigger")
                    .WithDescription(SyncParticipantsJob.Description)
                    .WithCronSchedule(syncParticipantsJobOptions.CronSchedule));
            }

            if (options.GetSection(DisableDormantAccountsJob.Key.Name).Get<JobOptions>() is
                { Enabled: true } disableDormantAccountsJobOptions)
            {
                quartz.AddJob<DisableDormantAccountsJob>(opts =>
                    opts.WithIdentity(DisableDormantAccountsJob.Key)
                        .WithDescription(DisableDormantAccountsJob.Description));

                quartz.AddTrigger(opts => opts
                    .ForJob(DisableDormantAccountsJob.Key)
                    .WithIdentity($"{DisableDormantAccountsJob.Key}-trigger")
                    .WithDescription(DisableDormantAccountsJob.Description)
                    .WithCronSchedule(disableDormantAccountsJobOptions.CronSchedule));
            }

            if (options.GetSection(NotifyAccountDeactivationJob.Key.Name).Get<JobOptions>() is
                { Enabled: true } notifyAccountDeactivationJobOptions)
            {
                quartz.AddJob<NotifyAccountDeactivationJob>(opts =>
                    opts.WithIdentity(NotifyAccountDeactivationJob.Key)
                        .WithDescription(NotifyAccountDeactivationJob.Description));

                quartz.AddTrigger(opts => opts
                    .ForJob(NotifyAccountDeactivationJob.Key)
                    .WithIdentity($"{NotifyAccountDeactivationJob.Key}-trigger")
                    .WithDescription(NotifyAccountDeactivationJob.Description)
                    .WithCronSchedule(notifyAccountDeactivationJobOptions.CronSchedule));
            }

            if (options.GetSection(PublishOutboxMessagesJob.Key.Name).Get<JobOptions>() is
                { Enabled: true } publishOutboxMessagesOptions)
            {
                quartz.AddJob<PublishOutboxMessagesJob>(opts =>
                    opts.WithIdentity(PublishOutboxMessagesJob.Key)
                        .WithDescription(PublishOutboxMessagesJob.Description));

                quartz.AddTrigger(opts => opts
                    .ForJob(PublishOutboxMessagesJob.Key)
                    .WithIdentity($"{PublishOutboxMessagesJob.Key}-trigger")
                    .WithDescription(PublishOutboxMessagesJob.Description)
                    .WithCronSchedule(publishOutboxMessagesOptions.CronSchedule));
            }


        });

        services.AddQuartzServer(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }

    public static T GetRequiredSection<T>(this IConfiguration configuration, string name) =>
        configuration.GetSection(name).Get<T>() ?? throw new InvalidOperationException($"Configuration missing section for: {(configuration is IConfigurationSection s ? s.Path + ":" + name : name)}");

    public static string GetRequiredValue(this IConfiguration configuration, string name) =>
        configuration[name] ?? throw new InvalidOperationException($"Configuration missing value for: {(configuration is IConfigurationSection s ? s.Path + ":" + name : name)}");

    private static IServiceCollection AddFusionCacheService(this IServiceCollection services)
    {
        services.AddMemoryCache();
        services
            .AddFusionCache()
            .WithDefaultEntryOptions(
            new FusionCacheEntryOptions
            {
                // CACHE DURATION
                Duration = TimeSpan.FromMinutes(120),
                // FAIL-SAFE OPTIONS
                IsFailSafeEnabled = true,
                FailSafeMaxDuration = TimeSpan.FromHours(8),
                FailSafeThrottleDuration = TimeSpan.FromSeconds(30),
                // FACTORY TIMEOUTS
                FactorySoftTimeout = TimeSpan.FromMilliseconds(100),
                FactoryHardTimeout = TimeSpan.FromMilliseconds(1500)
            }
            );
        return services;
    }
}
