using tusdotnet.Interfaces;

namespace tusdotnet.FileLocks;

/// <summary>
///     Provides In-memory locks for files.
/// </summary>
public sealed class InMemoryFileLockProvider : ITusFileLockProvider
{
    private InMemoryFileLockProvider()
    {
    }

    /// <summary>
    ///     Singleton instance for this provider.
    /// </summary>
    public static ITusFileLockProvider Instance { get; } = new InMemoryFileLockProvider();

    /// <inheritdoc />
    public Task<ITusFileLock> AquireLock(string fileId)
    {
        return Task.FromResult<ITusFileLock>(new InMemoryFileLock(fileId));
    }
}