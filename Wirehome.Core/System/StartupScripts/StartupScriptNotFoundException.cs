﻿using Wirehome.Core.Exceptions;

namespace Wirehome.Core.System.StartupScripts;

public sealed class StartupScriptNotFoundException : WirehomeException
{
    public StartupScriptNotFoundException(string uid) : base($"Startup script with UID '{uid}' not found.")
    {
    }
}