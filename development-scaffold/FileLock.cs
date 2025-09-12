using System.Text;

namespace XYVR.Scaffold;

public class FileLock(string filePath) : IDisposable
{
    private FileStream? _lockFileStream;
    private bool _disposed;

    public bool IsLocked => _lockFileStream != null && !_disposed;

    public void AcquireLock()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(FileLock));
        if (_lockFileStream != null) return;

        try
        {
            _lockFileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                1,
                FileOptions.DeleteOnClose
            );

            var lockInfo = Encoding.UTF8.GetBytes($"Locked by process {Environment.ProcessId} at {DateTime.UtcNow:O}");
            _lockFileStream.Write(lockInfo, 0, lockInfo.Length);
            _lockFileStream.Flush();

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("Cannot acquire file lock. Another process may already have the lock.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new InvalidOperationException("Cannot acquire file lock due to insufficient permissions.", ex);
        }
    }

    public void ReleaseLock()
    {
        if (_lockFileStream == null) return;
        
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;

        _lockFileStream.Dispose();
        _lockFileStream = null;

        try
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
        catch
        {
            // ignored
        }
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        ReleaseLock();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            ReleaseLock();
            _disposed = true;
        }
    }
}