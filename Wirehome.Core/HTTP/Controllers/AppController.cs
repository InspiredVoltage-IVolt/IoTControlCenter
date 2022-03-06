﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.App;
using Wirehome.Core.Constants;
using Wirehome.Core.GlobalVariables;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class AppController : Controller
{
    readonly AppService _appService;
    readonly GlobalVariablesService _globalVariablesService;

    public AppController(AppService appService, GlobalVariablesService globalVariablesService)
    {
        _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        _globalVariablesService = globalVariablesService ?? throw new ArgumentNullException(nameof(globalVariablesService));
    }

    [HttpGet]
    [Route("api/v1/app/access_type")]
    [ApiExplorerSettings(GroupName = "v1")]
    public ActionResult<string> GetAccessType()
    {
        // This API will always return "local" because it IS local access if this point is reached.
        // Wirehome.Cloud will override this API and return "remote" always. Using this same URI the
        // app can properly distinguish.
        return "local";
    }

    [HttpGet]
    [Route("api/v1/app/package_uid")]
    [ApiExplorerSettings(GroupName = "v1")]
    public ActionResult<object> GetPackageUid()
    {
        return _globalVariablesService.GetValue(GlobalVariableUids.AppPackageUid);
    }

    [HttpGet]
    [Route("api/v1/app/panels")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<AppPanelDefinition> GetRegisteredPanels()
    {
        return _appService.GetRegisteredPanels();
    }

    [HttpGet]
    [Route("api/v1/app/status")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IActionResult GetStatus()
    {
        return new ObjectResult(_appService.GenerateStatusContainer());
    }
}