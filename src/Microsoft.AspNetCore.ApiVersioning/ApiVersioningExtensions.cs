// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiVersioning;

public static class ApiVersioningExtensions
{
    [PublicAPI]
    public static IServiceCollection AddApiVersioning(
        this IServiceCollection services,
        Action<ApiVersioningOptions> configureOptions)
    {
        return services.AddSingleton<VersionedControllerProvider>()
            .AddSingleton<VersionedApiConvention>()
            .AddSingleton<IConfigureOptions<MvcOptions>, ApiVersioningMvcOptionsSetup>()
            .AddTransient<IApiDescriptionProvider, RequiredParameterDescriptorProvider>()
            .Configure(configureOptions);
    }
}

public class ApiVersioningMvcOptionsSetup : IConfigureOptions<MvcOptions>
{
    private readonly VersionedApiConvention _apiConvention;

    public ApiVersioningMvcOptionsSetup(VersionedApiConvention apiConvention)
    {
        _apiConvention = apiConvention;
    }

    public void Configure(MvcOptions options)
    {
        options.Conventions.Add(_apiConvention);
    }
}
