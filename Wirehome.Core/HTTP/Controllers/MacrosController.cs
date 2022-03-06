﻿using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Macros;
using Wirehome.Core.Macros.Configuration;
using Wirehome.Core.Macros.Exceptions;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class MacrosController : Controller
{
    readonly MacroRegistryService _macroRegistryService;

    public MacrosController(MacroRegistryService macroRegistryService)
    {
        _macroRegistryService = macroRegistryService ?? throw new ArgumentNullException(nameof(macroRegistryService));
    }

    [HttpDelete]
    [Route("api/v1/macros/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteMacro(string uid)
    {
        _macroRegistryService.DeleteMacro(uid);
    }

    [HttpDelete]
    [Route("/api/v1/macros/{macroUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object DeleteSetting(string macroUid, string settingUid)
    {
        return _macroRegistryService.RemoveMacroSetting(macroUid, settingUid);
    }

    [HttpGet]
    [Route("api/v1/macros/{uid}/configuration")]
    [ApiExplorerSettings(GroupName = "v1")]
    public MacroConfiguration GetConfiguration(string uid)
    {
        try
        {
            return _macroRegistryService.ReadMacroConfiguration(uid);
        }
        catch (MacroNotFoundException)
        {
            HttpContext.Response.StatusCode = (int)HttpStatusCode.NotFound;
            return null;
        }
    }

    [HttpGet]
    [Route("api/v1/macros/uids")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<string> GetMacroUids()
    {
        return _macroRegistryService.GetMacroUids();
    }

    [HttpGet]
    [Route("/api/v1/macros/{macroUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public object GetSettingValue(string macroUid, string settingUid)
    {
        return _macroRegistryService.GetMacroSetting(macroUid, settingUid);
    }

    [HttpGet]
    [Route("/api/v1/macros/{uid}/settings")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary<string, object> GetSettingValues(string uid)
    {
        return _macroRegistryService.GetMacro(uid).Settings;
    }

    [HttpPost]
    [Route("api/v1/macro/{uid}/configuration")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostConfiguration(string uid, [FromBody] MacroConfiguration configuration)
    {
        _macroRegistryService.WriteMacroConfiguration(uid, configuration);
    }

    [HttpPost]
    [Route("api/v1/macros/{uid}/execute")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary<object, object> PostExecuteMacro(string uid)
    {
        return _macroRegistryService.ExecuteMacro(uid);
    }

    [HttpPost]
    [Route("api/v1/macros/{uid}/initialize")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostInitializeMacro(string uid)
    {
        _macroRegistryService.InitializeMacro(uid);
    }

    [HttpPost]
    [Route("/api/v1/macros/{macroUid}/settings/{settingUid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void PostSettingValue(string macroUid, string settingUid, [FromBody] object value)
    {
        _macroRegistryService.SetMacroSetting(macroUid, settingUid, value);
    }
}