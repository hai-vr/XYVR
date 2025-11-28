namespace XYVR.Sqlite;

using System;
using Microsoft.Data.Sqlite;

public class XYVRSqliteContainer
{
    private string _databasePath;
    private SqliteConnection? _connection;

    public XYVRSqliteContainer(string databasePath)
    {
        _databasePath = databasePath;
    }

    public void Open()
    {
        if (_connection != null) throw new InvalidOperationException("Database connection is already open.");
        
        var connectionString = $"Data Source={_databasePath}";
        _connection = new SqliteConnection(connectionString);
        _connection.Open();
        
        using var command = _connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL; PRAGMA synchronous = FULL;";
        command.ExecuteNonQuery();
    }

    public void Close()
    {
        if (_connection == null) throw new InvalidOperationException("Database connection is not open.");
        
        _connection?.Close();
        _connection?.Dispose();
        _connection = null;
    }

    public void CreateSingularStringStorageTableIfNotExists(string tableName)
    {
        EnsureThatTableNameIsValid(tableName);
        
        using var command = _connection!.CreateCommand();
        command.CommandText = $@"
                CREATE TABLE IF NOT EXISTS {tableName} (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    Content TEXT NOT NULL
                )";
        command.ExecuteNonQuery();
    }

    public void SetString(string tableName, string content)
    {
        EnsureThatConnectionIsOpen();
        EnsureThatTableNameIsValid(tableName);

        using var command = _connection!.CreateCommand();
        command.CommandText = $@"
                INSERT OR REPLACE INTO {tableName} (Id, Content)
                VALUES (1, @content)";
        command.Parameters.AddWithValue("@content", content ?? string.Empty);
        command.ExecuteNonQuery();
    }

    public string? GetString(string tableName)
    {
        EnsureThatConnectionIsOpen();
        EnsureThatTableNameIsValid(tableName);

        using var command = _connection!.CreateCommand();
        command.CommandText = $"SELECT Content FROM {tableName} WHERE Id = 1";
        var result = command.ExecuteScalar();
        return result?.ToString();
    }

    private void EnsureThatConnectionIsOpen()
    {
        if (_connection?.State != System.Data.ConnectionState.Open)
            throw new InvalidOperationException("Database connection is not open.");
    }

    private static void EnsureThatTableNameIsValid(string tableName)
    {
        if (!IsValidTableName(tableName))
        {
            throw new ArgumentException("Invalid table name. Use only letters, digits, and underscore.", nameof(tableName));
        }
    }

    private static bool IsValidTableName(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName)) return false;
        
        // Prevent SQL injection.
        foreach (var c in tableName)
        {
            if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
        }
        return true;
    }
}