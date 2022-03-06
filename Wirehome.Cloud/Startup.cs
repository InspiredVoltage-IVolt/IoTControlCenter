﻿using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Wirehome.Cloud.Filters;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Cloud.Services.DeviceConnector;
using Wirehome.Cloud.Services.Repository;
using Wirehome.Core.Cloud.Protocol;
using Wirehome.Core.HTTP.Controllers;
using CloudController = Wirehome.Cloud.Controllers.CloudController;

namespace Wirehome.Cloud;

// ReSharper disable once ClassNeverInstantiated.Global
public class Startup
{
    AuthorizationService _authorizationService;
    DeviceConnectorService _deviceConnectorService;

    // ReSharper disable once UnusedMember.Global
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, AuthorizationService authorizationService, DeviceConnectorService deviceConnectorService)
    {
        if (app == null)
        {
            throw new ArgumentNullException(nameof(app));
        }

        if (env == null)
        {
            throw new ArgumentNullException(nameof(env));
        }

        if (authorizationService == null)
        {
            throw new ArgumentNullException(nameof(authorizationService));
        }

        if (deviceConnectorService == null)
        {
            throw new ArgumentNullException(nameof(deviceConnectorService));
        }

        _authorizationService = authorizationService;
        _deviceConnectorService = deviceConnectorService;

        if (env.EnvironmentName == "Development")
        {
            app.UseDeveloperExceptionPage();
        }

        app.UseStaticFiles();
        app.UseCors(config => config.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
        app.UseResponseCompression();

        ConfigureMvc(app);
        ConfigureSwagger(app);
        ConfigureConnector(app);
        ConfigureHttpReverseProxy(app);
    }

    // ReSharper disable once UnusedMember.Global
    public void ConfigureServices(IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<DeviceConnectorService>();
        services.AddSingleton<CloudMessageFactory>();
        services.AddSingleton<CloudMessageSerializer>();
        services.AddSingleton<AuthorizationService>();
        services.AddSingleton<RepositoryService>();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o =>
        {
            o.LoginPath = "/cloud/account/login";
            o.LogoutPath = "/cloud/account/logout";
            o.Events.OnRedirectToLogin = context =>
            {
                // This ensures that API calls are not forwarded to the login
                // page. They will only return 401 instead.
                if (context.Request.Path.StartsWithSegments("/api") && context.Response.StatusCode == (int)HttpStatusCode.OK)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                }
                else
                {
                    context.Response.Redirect(context.RedirectUri);
                }

                return Task.CompletedTask;
            };
        });

        services.AddMvc(config => { config.Filters.Add(new DefaultExceptionFilter()); }).ConfigureApplicationPartManager(config =>
        {
            // This is required because ASP.NET tries to find controllers in ALL referenced assemblies.
            // This project only covers the cloud part and only uses Wirehome.Core for protocol classes etc.
            config.FeatureProviders.Remove(config.FeatureProviders.First(f => f.GetType() == typeof(ControllerFeatureProvider)));
            config.FeatureProviders.Add(new WirehomeControllerFeatureProvider(typeof(CloudController).Namespace));
        });

        ConfigureSwaggerServices(services);

        services.AddCors();
        services.AddResponseCompression();
    }

    void ConfigureConnector(IApplicationBuilder app)
    {
        app.Map("/Connector", config =>
        {
            config.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(2)
            });

            config.Use(WebSocketMiddleware);
        });
    }

    void ConfigureHttpReverseProxy(IApplicationBuilder app)
    {
        app.Run(_deviceConnectorService.TryDispatchHttpRequestAsync);
    }

    static void ConfigureMvc(IApplicationBuilder app)
    {
        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints => { endpoints.MapControllerRoute("default", "{controller=Account}/{action=Index}/{Id?}"); });
    }

    static void ConfigureSwagger(IApplicationBuilder app)
    {
        app.UseSwagger(o => o.RouteTemplate = "/cloud/api/{documentName}/swagger.json");

        app.UseSwaggerUI(o =>
        {
            o.RoutePrefix = "cloud/api";
            o.DocumentTitle = "Wirehome.Cloud.API";
            o.SwaggerEndpoint("/cloud/api/v1/swagger.json", "Wirehome.Cloud API v1");
        });
    }

    static void ConfigureSwaggerServices(IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Wirehome.Cloud API",
                Version = "v1",
                Description = "This is the public API for the Wirehome.Cloud service.",
                License = new OpenApiLicense
                {
                    Name = "Apache-2.0",
                    Url = new Uri("https://github.com/chkr1011/Wirehome.Core/blob/master/LICENSE")
                },
                Contact = new OpenApiContact
                {
                    Name = "Wirehome.Core",
                    Email = string.Empty,
                    Url = new Uri("https://github.com/chkr1011/Wirehome.Core")
                }
            });
        });
    }

    async Task WebSocketMiddleware(HttpContext context, Func<Task> next)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            return;
        }

        try
        {
            var channelIdentifier = await _authorizationService.AuthorizeDevice(context).ConfigureAwait(false);

            using (var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false))
            {
                await _deviceConnectorService.RunAsync(channelIdentifier, webSocket, context.RequestAborted).ConfigureAwait(false);
            }
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        }
    }
}