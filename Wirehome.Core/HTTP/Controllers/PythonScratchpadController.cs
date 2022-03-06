﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class PythonScratchpadController : Controller
{
    readonly PythonScriptHostFactoryService _pythonScriptHostFactoryService;

    public PythonScratchpadController(PythonScriptHostFactoryService pythonScriptHostFactoryService)
    {
        _pythonScriptHostFactoryService = pythonScriptHostFactoryService ?? throw new ArgumentNullException(nameof(pythonScriptHostFactoryService));
    }

    [HttpPost]
    [Route("api/v1/python_scratchpad/execute")]
    [ApiExplorerSettings(GroupName = "v1")]
    public async Task<object> ExecuteScript(string function_name = "main")
    {
        try
        {
            string script;
            using (var streamReader = new StreamReader(HttpContext.Request.Body))
            {
                script = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            }

            var scriptHost = _pythonScriptHostFactoryService.CreateScriptHost();
            scriptHost.Compile(script);

            if (string.IsNullOrEmpty(function_name))
            {
                return null;
            }

            return scriptHost.InvokeFunction(function_name);
        }
        catch (Exception exception)
        {
            return new ExceptionPythonModel(exception).ToDictionary();
        }
    }
}