﻿using tusdotnet.Helpers;
using tusdotnet.Interfaces;

namespace tusdotnet.FileLocks;

/// <inheritdoc />
public sealed class DiskFileLock : ITusFileLock
{
    private readonly string _fileLockDiskLocation;
    private bool _hasLock;

    /// <inheritdoc />
    public DiskFileLock(string lockFolderLocation, string fileId)
    {
        _fileLockDiskLocation = Path.Combine(lockFolderLocation, fileId + ".lock");
    }

    /// <inheritdoc />
    public Task<bool> Lock()
    {
        if (_hasLock) return Task.FromResult(true);

        try
        {
            new FileStream(_fileLockDiskLocation, FileMode.CreateNew).Dispose();
            _hasLock = true;
        }
        catch (Exception)
        {
            _hasLock = false;
        }

        return Task.FromResult(_hasLock);
    }

    /// <inheritdoc />
    public Task ReleaseIfHeld()
    {
        if (_hasLock) File.Delete(_fileLockDiskLocation);
        return TaskHelper.Completed;
    }
}