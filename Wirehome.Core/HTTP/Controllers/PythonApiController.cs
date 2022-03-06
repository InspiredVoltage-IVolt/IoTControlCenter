﻿using System;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Python;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class PythonApiController : Controller
{
    readonly PythonProxyFactory _pythonProxyFactory;

    public PythonApiController(PythonProxyFactory pythonProxyFactory)
    {
        _pythonProxyFactory = pythonProxyFactory ?? throw new ArgumentNullException(nameof(pythonProxyFactory));
    }

    // TODO: Add reference in JSON format.

    [HttpGet]
    [Route("/api/v1/python_api/reference_document")]
    [ApiExplorerSettings(GroupName = "v1")]
    public string GetReferenceDocument()
    {
        var reference = new PythonProxyReferenceGenerator(_pythonProxyFactory).Generate();
        HttpContext.Response.ContentType = "text/plain";
        return reference;
    }
}