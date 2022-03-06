﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.HTTP.Controllers.Models;
using Wirehome.Core.Python.Models;
using Wirehome.Core.Scheduler;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class SchedulerController : Controller
{
    readonly SchedulerService _schedulerService;

    public SchedulerController(SchedulerService schedulerService)
    {
        _schedulerService = schedulerService ?? throw new ArgumentNullException(nameof(schedulerService));
    }

    [HttpDelete]
    [Route("/api/v1/scheduler/active_countdowns/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteActiveCountdown(string uid)
    {
        _schedulerService.StopCountdown(uid);
    }

    [HttpDelete]
    [Route("/api/v1/scheduler/active_threads/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteActiveThread(string uid)
    {
        _schedulerService.StopThread(uid);
    }

    [HttpDelete]
    [Route("/api/v1/scheduler/active_timers/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteActiveTimer(string uid)
    {
        _schedulerService.StopTimer(uid);
    }

    [HttpGet]
    [Route("/api/v1/scheduler/active_countdowns")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary<string, ActiveCountdownModel> GetActiveCountdowns()
    {
        return _schedulerService.GetActiveCountdowns().ToDictionary(t => t.Uid, t => new ActiveCountdownModel
        {
            TimeLeft = (int)t.TimeLeft.TotalMilliseconds
        });
    }

    [HttpGet]
    [Route("/api/v1/scheduler/active_threads")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary<string, ActiveThreadModel> GetActiveThreads()
    {
        return _schedulerService.GetActiveThreads().ToDictionary(t => t.Uid, t => new ActiveThreadModel
        {
            CreatedTimestamp = t.CreatedTimestamp.ToString("O"),
            Uptime = (int)(DateTime.UtcNow - t.CreatedTimestamp).TotalMilliseconds,
            ManagedThreadId = t.ManagedThreadId
        });
    }

    [HttpGet]
    [Route("/api/v1/scheduler/active_timers")]
    [ApiExplorerSettings(GroupName = "v1")]
    public IDictionary<string, ActiveTimerModel> GetActiveTimers()
    {
        return _schedulerService.GetActiveTimers().ToDictionary(t => t.Uid, t => new ActiveTimerModel
        {
            Interval = (int)t.Interval.TotalMilliseconds,
            LastException = t.LastException != null ? new ExceptionPythonModel(t.LastException) : null,
            LastDuration = (long)t.LastDuration.TotalMilliseconds,
            InvocationCount = t.InvocationCount
        });
    }
}