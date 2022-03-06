﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wirehome.Core.Contracts;
using Wirehome.Core.Packages.Exceptions;
using Wirehome.Core.Packages.GitHub;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Packages;

public sealed class PackageManagerService : WirehomeCoreService
{
    const string PackagesDirectory = "Packages";
    readonly ILogger _logger;

    readonly StorageService _storageService;

    string _rootPath;

    public PackageManagerService(StorageService storageService, ILogger<PackageManagerService> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void DeletePackage(PackageUid uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        var rootPath = GetPackageRootPath(uid);
        if (!Directory.Exists(rootPath))
        {
            return;
        }

        Directory.Delete(rootPath, true);
        _logger.Log(LogLevel.Information, $"Deleted package '{uid}'.");
    }

    public Task DownloadPackageAsync(PackageUid uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _storageService.SafeReadSerializedValue(out PackageManagerServiceOptions options, DefaultDirectoryNames.Configuration, PackageManagerServiceOptions.Filename);

        var downloader = new GitHubRepositoryPackageDownloader(options, _logger);
        return downloader.DownloadAsync(uid, GetPackageRootPath(uid));
    }

    public async Task<List<PackageUid>> FetchRemotePackageUidsAsync()
    {
        await Task.CompletedTask.ConfigureAwait(false);
        // TODO: Implement.
        return new List<PackageUid>();
    }

    public Task ForkPackageAsync(PackageUid packageUid, PackageUid packageForkUid)
    {
        if (packageUid == null)
        {
            throw new ArgumentNullException(nameof(packageUid));
        }

        if (packageForkUid == null)
        {
            throw new ArgumentNullException(nameof(packageForkUid));
        }

        if (!PackageExists(packageUid))
        {
            throw new WirehomePackageNotFoundException(packageUid);
        }

        if (PackageExists(packageForkUid))
        {
            throw new InvalidOperationException($"Package '{packageForkUid}' already exists.");
        }

        var sourcePath = GetPackageRootPath(packageUid);
        var destinationPath = GetPackageRootPath(packageForkUid);

        Directory.CreateDirectory(destinationPath);

        foreach (var directory in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(sourcePath, destinationPath, StringComparison.Ordinal));
        }

        foreach (var file in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(sourcePath, destinationPath, StringComparison.Ordinal), true);
        }

        return Task.CompletedTask;
    }

    public string GetPackageDescription(PackageUid uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return LoadPackage(uid).Description;
    }

    public PackageMetaData GetPackageMetaData(PackageUid uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return LoadPackage(uid).MetaData;
    }

    public string GetPackageReleaseNotes(PackageUid uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return LoadPackage(uid).ReleaseNotes;
    }

    public string GetPackageRootPath(PackageUid uid)
    {
        if (uid is null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        var path = _rootPath;

        if (string.IsNullOrEmpty(uid.Version))
        {
            path = GetLatestVersionPath(uid.Id);
        }
        else
        {
            path = Path.Combine(path, uid.Id, uid.Version);
        }

        return path;
    }

    public List<PackageUid> GetPackageUids()
    {
        var packageUids = new List<PackageUid>();

        foreach (var packageId in _storageService.EnumerateDirectories("*", _rootPath))
        {
            if (packageId.StartsWith(".", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var packageVersion in _storageService.EnumerateDirectories("*", _rootPath, packageId))
            {
                packageUids.Add(new PackageUid(packageId, packageVersion));
            }
        }

        return packageUids;
    }

    public Package LoadPackage(PackageUid uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (string.IsNullOrEmpty(uid.Id))
        {
            throw new ArgumentException("The ID of the package UID is not set.");
        }

        var path = GetPackageRootPath(uid);
        var source = LoadPackage(uid, path);

        return source;
    }

    public bool PackageExists(PackageUid uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        var path = GetPackageRootPath(uid);
        return Directory.Exists(path);
    }

    protected override void OnStart()
    {
        _storageService.SafeReadSerializedValue(out PackageManagerServiceOptions options, DefaultDirectoryNames.Configuration, PackageManagerServiceOptions.Filename);

        _rootPath = options.RootPath;
        if (string.IsNullOrEmpty(_rootPath) || !Directory.Exists(_rootPath))
        {
            _rootPath = Path.Combine(_storageService.DataPath, PackagesDirectory);
        }
    }

    string GetLatestVersionPath(string id)
    {
        var packageRootPath = Path.Combine(_rootPath, id);

        if (!Directory.Exists(packageRootPath))
        {
            throw new WirehomePackageNotFoundException(PackageUid.Parse(id));
        }

        var versions = Directory.GetDirectories(packageRootPath).OrderByDescending(d => d.ToLowerInvariant());

        return versions.First();
    }

    static Package LoadPackage(PackageUid uid, string path)
    {
        if (!Directory.Exists(path))
        {
            throw new WirehomePackageNotFoundException(uid);
        }

        var source = new Package();

        var metaFile = Path.Combine(path, "meta.json");
        if (!File.Exists(metaFile))
        {
            throw new WirehomePackageException($"Package directory '{path}' contains no 'meta.json'.");
        }

        try
        {
            var metaData = File.ReadAllText(metaFile, Encoding.UTF8);
            source.MetaData = JsonConvert.DeserializeObject<PackageMetaData>(metaData);
        }
        catch (Exception exception)
        {
            throw new WirehomePackageException("Unable to parse 'meta.json'.", exception);
        }

        source.Uid = new PackageUid
        {
            Id = Directory.GetParent(path).Name,
            Version = new DirectoryInfo(path).Name
        };

        source.Description = ReadFileContent(path, "description.md");
        source.ReleaseNotes = ReadFileContent(path, "releaseNotes.md");
        source.Script = ReadFileContent(path, "script.py");

        return source;
    }

    static string ReadFileContent(string path, string filename)
    {
        var descriptionFile = Path.Combine(path, filename);
        if (!File.Exists(descriptionFile))
        {
            return string.Empty;
        }

        return File.ReadAllText(descriptionFile, Encoding.UTF8);
    }
}