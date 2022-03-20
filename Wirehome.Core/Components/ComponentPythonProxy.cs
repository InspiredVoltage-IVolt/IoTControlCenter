﻿#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

using System;
using Wirehome.Core.Python;

namespace Wirehome.Core.Components;

public sealed class ComponentPythonProxy : IPythonProxy
{
    readonly ComponentRegistryService _componentRegistryService;
    readonly string _componentUid;

    public ComponentPythonProxy(string componentUid, ComponentRegistryService componentRegistryService)
    {
        _componentUid = componentUid ?? throw new ArgumentNullException(nameof(componentUid));
        _componentRegistryService = componentRegistryService ?? throw new ArgumentNullException(nameof(componentRegistryService));
    }

    public string ModuleName => "component";

    public bool add_tag(string tag)
    {
        return set_tag(tag);
    }

    public object get_configuration(string configuration_uid, object default_value = null)
    {
        return PythonConvert.ToPython(_componentRegistryService.GetComponentConfigurationValue(_componentUid, configuration_uid, default_value));
    }

    public object get_setting(string setting_uid, object default_value = null)
    {
        return PythonConvert.ToPython(_componentRegistryService.GetComponentSetting(_componentUid, setting_uid, default_value));
    }

    public object get_status(string status_uid, object default_value = null)
    {
        return PythonConvert.ToPython(_componentRegistryService.GetComponentStatusValue(_componentUid, status_uid, default_value));
    }

    public string get_uid()
    {
        return _componentUid;
    }

    public bool has_setting(string setting_uid)
    {
        return _componentRegistryService.ComponentHasSetting(_componentUid, setting_uid);
    }

    public bool has_status(string status_uid)
    {
        return _componentRegistryService.ComponentHasStatusValue(_componentUid, status_uid);
    }

    public bool has_tag(string tag)
    {
        return _componentRegistryService.ComponentHasTag(_componentUid, tag);
    }

    public void register_setting(string setting_uid, object value)
    {
        _componentRegistryService.RegisterComponentSetting(_componentUid, setting_uid, PythonConvert.FromPython(value));
    }

    public bool remove_tag(string tag)
    {
        return _componentRegistryService.RemoveComponentTag(_componentUid, tag);
    }

    public void set_configuration(string configuration_uid, object value)
    {
        _componentRegistryService.SetComponentConfigurationValue(_componentUid, configuration_uid, PythonConvert.FromPython(value));
    }

    public void set_setting(string setting_uid, object value)
    {
        _componentRegistryService.SetComponentSetting(_componentUid, setting_uid, PythonConvert.FromPython(value));
    }

    public void set_status(string status_uid, object value)
    {
        _componentRegistryService.SetComponentStatusValue(_componentUid, status_uid, PythonConvert.FromPython(value));
    }

    public bool set_tag(string tag)
    {
        return _componentRegistryService.SetComponentTag(_componentUid, tag);
    }
}