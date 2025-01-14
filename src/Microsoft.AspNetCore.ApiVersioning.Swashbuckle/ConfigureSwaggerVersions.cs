// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.ApiVersioning.Schemes;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.AspNetCore.ApiVersioning.Swashbuckle;

[PublicAPI]
public class ConfigureSwaggerVersions : IConfigureOptions<SwaggerGenOptions>
{
    public ConfigureSwaggerVersions(
        VersionedControllerProvider controllerProvider,
        IOptions<ApiVersioningOptions> versioningOptionsAccessor,
        Action<string, OpenApiInfo> configureVersionInfo)
    {
        ControllerProvider = controllerProvider;
        ConfigureVersionInfo = configureVersionInfo;
        VersioningOptions = versioningOptionsAccessor.Value;
    }

    private ApiVersioningOptions VersioningOptions { get; }

    private VersionedControllerProvider ControllerProvider { get; }

    private Action<string, OpenApiInfo> ConfigureVersionInfo { get; }

    public void Configure(SwaggerGenOptions options)
    {
        ConfigureSwaggerVersioningParameters(options, VersioningOptions.VersioningScheme);
        foreach (string version in ControllerProvider.Versions.Keys)
        {
            var info = new OpenApiInfo
            {
                Version = version,
                Title = $"Version {version}",
            };
            ConfigureVersionInfo?.Invoke(version, info);
            options.SwaggerDoc(version, info);
        }

        options.DocInclusionPredicate(IsVersion);
        options.TagActionsBy(
            desc =>
            {
                string controller = desc.ActionDescriptor.RouteValues["controller"];
                string version = desc.ActionDescriptor.RouteValues["version"];
                if (controller.EndsWith(version))
                {
                    controller = controller.Substring(0, controller.Length - version.Length - 1);
                }

                return new[] {controller};
            });
        options.OperationFilter<SwaggerModelBindingOperationFilter>();
        options.OperationFilter<SwaggerNamingOperationFilter>();
    }

    private void ConfigureSwaggerVersioningParameters(SwaggerGenOptions options, IVersioningScheme versioningScheme)
    {
        if (versioningScheme is QueryVersioningScheme qvs)
        {
            ConfigureQueryVersioning(options, qvs);
        }

        if (versioningScheme is HeaderVersioningScheme hvs)
        {
            ConfigureHeaderVersioning(options, hvs);
        }

        if (versioningScheme is ISwaggerVersioningScheme svs)
        {
            ConfigureSwaggerVersioning(options, svs);
        }
    }

    private void ConfigureQueryVersioning(SwaggerGenOptions options, QueryVersioningScheme versioningScheme)
    {
        options.OperationFilter<QueryVersioningOperationFilter>(versioningScheme);
    }

    private void ConfigureHeaderVersioning(SwaggerGenOptions options, HeaderVersioningScheme versioningScheme)
    {
        options.OperationFilter<HeaderVersioningOperationFilter>(versioningScheme);
    }

    private void ConfigureSwaggerVersioning(SwaggerGenOptions options, ISwaggerVersioningScheme versioningScheme)
    {
        options.OperationFilter<VersioningOperationFilter>(versioningScheme);
    }

    private bool IsVersion(string version, ApiDescription desc)
    {
        return desc.ActionDescriptor.RouteValues["version"] == version;
    }
}

public class SwaggerNamingOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.ActionDescriptor is ControllerActionDescriptor desc)
        {
            operation.OperationId = desc.ActionName;
        }
    }
}
