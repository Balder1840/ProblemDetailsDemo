﻿using System;
using System.Data;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ProblemDetailsDemo.Api.ProblemDetailsConfig
{
    public class ProblemDetailsOptionsCustomSetup : IConfigureOptions<ProblemDetailsOptions>
    {
        public ProblemDetailsOptionsCustomSetup(IHostingEnvironment environment,
            IHttpContextAccessor httpContextAccessor, IOptions<ApiBehaviorOptions> apiOptions)
        {
            Environment = environment;
            HttpContextAccessor = httpContextAccessor;
            ApiOptions = apiOptions.Value;
        }

        private IHostingEnvironment Environment { get; }
        private IHttpContextAccessor HttpContextAccessor { get; }
        private ApiBehaviorOptions ApiOptions { get; }

        public void Configure(ProblemDetailsOptions options)
        {
            options.IncludeExceptionDetails = ctx => Environment.IsDevelopment();

            options.MapStatusCode = MapStatusCode;

            options.OnBeforeWriteDetails = (ctx, details) =>
            {
                // keep consistent with asp.net core 2.2 conventions that adds a tracing value
                ProblemDetailsHelper.SetTraceId(details, HttpContextAccessor.HttpContext);
            };

            // This will map DBConcurrencyException to the 409 Conflict status code.
            options.Map<DBConcurrencyException>(ex =>
                new ExceptionProblemDetails(ex, StatusCodes.Status409Conflict));

            // This will map NotImplementedException to the 501 Not Implemented status code.
            options.Map<NotImplementedException>(ex =>
                new ExceptionProblemDetails(ex, StatusCodes.Status501NotImplemented));
        }

        private ProblemDetails MapStatusCode(HttpContext context, int statusCode)
        {
            if (!ApiOptions.SuppressMapClientErrors &&
                ApiOptions.ClientErrorMapping.TryGetValue(statusCode, out var errorData))
            {
                // prefer the built-in mapping in asp.net core
                return new ProblemDetails
                {
                    Status = statusCode,
                    Title = errorData.Title,
                    Type = errorData.Link
                };
            }
            else
            {
                // use Hellang.Middleware.ProblemDetails mapping
                return new StatusCodeProblemDetails(statusCode);
            }
        }
    }
}