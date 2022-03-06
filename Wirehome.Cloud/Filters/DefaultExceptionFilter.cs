﻿using System;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Wirehome.Cloud.Services.DeviceConnector;

namespace Wirehome.Cloud.Filters;

public class DefaultExceptionFilter : ExceptionFilterAttribute
{
    public static bool HandleException(Exception exception, HttpContext httpContext)
    {
        if (exception is UnauthorizedAccessException)
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            return true;
        }

        if (exception is OpenChannelNotFoundException)
        {
            httpContext.Response.Redirect("/Cloud/Channel/DeviceNotConnected?returnUrl=" + httpContext.Request.Path);
            return true;
        }

        return false;
    }

    public override void OnException(ExceptionContext exceptionContext)
    {
        exceptionContext.ExceptionHandled = HandleException(exceptionContext.Exception, exceptionContext.HttpContext);
        base.OnException(exceptionContext);
    }
}