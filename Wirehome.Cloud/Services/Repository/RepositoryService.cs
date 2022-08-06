﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wirehome.Cloud.Services.Repository.Entities;
using Wirehome.Core.Foundation;
using Wirehome.Core.Storage;

namespace Wirehome.Cloud.Services.Repository;

public class RepositoryService
{
    readonly AsyncLock _lock = new();
    readonly ILogger _logger;
    readonly string _rootPath;

    public RepositoryService(ILogger<RepositoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        string _localPath = AppDomain.CurrentDomain.BaseDirectory;

        if (File.Exists(_localPath + "Resources\\Security\\info.dat"))
        {
            
            if (File.Exists(File.ReadAllText(_rootPath)))
            {
                _rootPath = File.ReadAllText(_rootPath);
            }
            else
            {
                _rootPath = _localPath + "Resources\\Security\\";
            }
        }
        else
        {
            if (Debugger.IsAttached)
            {
                _rootPath = Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), "Wirehome.Cloud", "Identities");
            }
            else
            {
                _rootPath = "C:\\act\\IoTControlCenter\\";
            }
        }
    }

    public async Task<KeyValuePair<string, IdentityEntity>> FindIdentityEntityByChannelAccessToken(string channelAccessToken)
    {
        if (channelAccessToken == null)
        {
            throw new ArgumentNullException(nameof(channelAccessToken));
        }

        await _lock.EnterAsync().ConfigureAwait(false);
        try
        {
            foreach (var identityPath in Directory.GetDirectories(_rootPath))
            {
                var identityUid = Path.GetFileName(identityPath);

                var identityEntity = await TryReadIdentityEntityAsync(identityUid).ConfigureAwait(false);
                var channelEntity = identityEntity?.Channels?.Values.FirstOrDefault(c => string.Equals(c.AccessToken?.Value, channelAccessToken, StringComparison.Ordinal));

                if (channelEntity != null && channelEntity.AccessToken.ValidUntil > DateTime.UtcNow)
                {
                    return new KeyValuePair<string, IdentityEntity>(identityUid, identityEntity);
                }
            }
        }
        finally
        {
            _lock.Exit();
        }

        return new KeyValuePair<string, IdentityEntity>(null, null);
    }

    public async Task<IdentityEntity> TryGetIdentityEntityAsync(string identityUid)
    {
        if (identityUid == null)
        {
            throw new ArgumentNullException(nameof(identityUid));
        }

        await _lock.EnterAsync().ConfigureAwait(false);
        try
        {
            return await TryReadIdentityEntityAsync(identityUid).ConfigureAwait(false);
        }
        finally
        {
            _lock.Exit();
        }
    }

    public async Task UpdateIdentity(string identityUid, Action<IdentityEntity> updateCallback)
    {
        if (updateCallback == null)
        {
            throw new ArgumentNullException(nameof(updateCallback));
        }

        await _lock.EnterAsync().ConfigureAwait(false);
        try
        {
            var identityEntity = await TryReadIdentityEntityAsync(identityUid).ConfigureAwait(false);
            updateCallback(identityEntity);

            await WriteIdentityEntityAsync(identityUid, identityEntity).ConfigureAwait(false);
        }
        finally
        {
            _lock.Exit();
        }
    }

    async Task<IdentityEntity> TryReadIdentityEntityAsync(string identityUid)
    {
        var filename = Path.Combine(_rootPath, identityUid, DefaultFileNames.Configuration);
        if (!File.Exists(filename))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filename, Encoding.UTF8).ConfigureAwait(false);
            var identityConfiguration = JsonConvert.DeserializeObject<IdentityEntity>(json);

            return identityConfiguration;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, $"Error while loading file '{filename}'.");

            return null;
        }
    }

    Task WriteIdentityEntityAsync(string identityUid, IdentityEntity identityConfiguration)
    {
        if (identityUid == null)
        {
            throw new ArgumentNullException(nameof(identityUid));
        }

        if (identityConfiguration == null)
        {
            throw new ArgumentNullException(nameof(identityConfiguration));
        }

        var filename = Path.Combine(_rootPath, identityUid);
        if (!Directory.Exists(filename))
        {
            Directory.CreateDirectory(filename);
        }

        filename = Path.Combine(filename, DefaultFileNames.Configuration);

        var json = JsonConvert.SerializeObject(identityConfiguration);
        return File.WriteAllTextAsync(filename, json, Encoding.UTF8);
    }
}