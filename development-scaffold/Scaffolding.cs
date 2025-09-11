using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using XYVR.Core;
using XYVR.Data.Collection;

namespace XYVR.Scaffold;

public static class Scaffolding
{
    private const string DefaultAppSaveFolder = "XYVR";
    private const string DefaultSubProfileFolder = "MainProfile";

    private static class ScaffoldingFileNames
    {
        internal const string IndividualsJsonFileName = "individuals.json";
        internal const string ConnectorsJsonFileName = "connectors.json";
        internal const string CredentialsJsonFileName = ".DO_NOT_SHARE__session-cookies.encrypted";
        internal const string ResponseCollectionFileName = "response-collection.jsonl";
        internal const string ResoniteUidFileName = "resonite.uid";
        internal const string ReactAppJsonFileName = "ui-preferences.json";
        internal const string WorldNameCacheFileName = ".cache_world-names.json";
    }
    
    private static string IndividualsJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.IndividualsJsonFileName);
    private static string ConnectorsJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ConnectorsJsonFileName);
    private static string CredentialsJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.CredentialsJsonFileName);
    private static string ResponseCollectionFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ResponseCollectionFileName);
    private static string ResoniteUidFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ResoniteUidFileName);
    private static string ReactAppJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ReactAppJsonFileName);
    private static string WorldNameCacheJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.WorldNameCacheFileName);
    
    private static readonly Encoding Encoding = Encoding.UTF8;
    private static readonly JsonSerializerSettings Serializer = new()
    {
        Converters = { new StringEnumConverter() }
    };
    
    private static readonly SemaphoreSlim DataCollectionFileLock = new(1, 1);
    private static bool _folderCreated;
    
    private static string _pathLateInit;
    private static string _encryptionKeyForSessionData;
    
    public static string DefaultSavePathAbsolute()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), DefaultAppSaveFolder, DefaultSubProfileFolder);
    }

    public static void DefineSavePathFromArgsOrUseDefault(string[] args)
    {
        DefineSavePath(FindSavePathInArgsOrNull(args) ?? DefaultSavePathAbsolute());
    }

    private static string? FindSavePathInArgsOrNull(string[] args)
    {
        var watchNext = false;
        foreach (var arg in args)
        {
            if (watchNext)
            {
                var path = arg;
                if (path.StartsWith("\"") && path.EndsWith("\""))
                {
                    path = path.Substring(1, path.Length - 2);
                }

                return path;
            }
            else if (arg.ToLowerInvariant() == "--savepath")
            {
                watchNext = true;
            }
        }

        return null;
    }

    private static string SavePath()
    {
        if (_pathLateInit == null) throw new Exception("SavePath() called before initialization!");
        return _pathLateInit;
    }

    public static void DefineSavePath(string savePath)
    {
        if (_pathLateInit != null) throw new Exception("SavePath() already defined!");
        _pathLateInit = savePath;
    }

    public static async Task<Individual[]> OpenRepository() => await OpenIfExists<Individual[]>(IndividualsJsonFilePath, () => []);
    public static async Task SaveRepository(IndividualRepository repository) => await SaveTo(repository.Individuals, IndividualsJsonFilePath);
    
    public static async Task<Connector[]> OpenConnectors() => await OpenIfExists<Connector[]>(ConnectorsJsonFilePath, () => []);
    public static async Task SaveConnectors(ConnectorManagement management) => await SaveTo(management.Connectors, ConnectorsJsonFilePath);
    
    public static async Task<SerializedCredentials> OpenCredentials()
    {
        EnsureRegistryHasEncryptionKeyForSavingSessionData();
        return await OpenIfExists<SerializedCredentials>(CredentialsJsonFilePath, () => new SerializedCredentials(), _encryptionKeyForSessionData);
    }

    public static async Task SaveCredentials(SerializedCredentials serialized)
    {
        EnsureRegistryHasEncryptionKeyForSavingSessionData();
        await SaveTo(serialized, CredentialsJsonFilePath, _encryptionKeyForSessionData);
    }


    private static async Task<string> GetOrCreateResoniteUID()
    {
        Func<string> defaultGen = RandomUID__NotCryptographicallySecure;
        if (File.Exists(ResoniteUidFilePath))
        {
            var text = await File.ReadAllTextAsync(ResoniteUidFilePath, Encoding);
            return JsonConvert.DeserializeObject<string>(text, Serializer)!;
        }

        var ret = defaultGen();
        await SaveTo(ret, ResoniteUidFilePath);
        return ret;
    }
    
    public static async Task<ReactAppPreferences> OpenReactAppPreferences() => await OpenIfExists<ReactAppPreferences>(ReactAppJsonFilePath, () => new ReactAppPreferences());
    public static async Task SaveReactAppPreferences(ReactAppPreferences serialized) => await SaveTo(serialized, ReactAppJsonFilePath);
    
    public static async Task<WorldNameCache> OpenWorldNameCache()
    {
        var result = await OpenIfExists<WorldNameCache>(WorldNameCacheJsonFilePath, () => new WorldNameCache());
        result.PreProcess();
        return result;
    }

    public static async Task SaveWorldNameCache(WorldNameCache serialized) => await SaveTo(serialized, WorldNameCacheJsonFilePath);

    public static async Task<List<ResponseCollectionTrail>> RebuildTrail()
    {
        if (!File.Exists(ResponseCollectionFilePath)) return [];
        
        var results = new List<ResponseCollectionTrail>();
        
        var lines = await File.ReadAllLinesAsync(ResponseCollectionFilePath, Encoding);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                results.Add(JsonConvert.DeserializeObject<ResponseCollectionTrail>(line, Serializer));
            }
        }

        return results;
    }

    private static async Task<T> OpenIfExists<T>(string fileName, Func<T> defaultGen, string? encryptionKey = null)
    {
        if (File.Exists(fileName))
        {
            var text = await File.ReadAllTextAsync(fileName, Encoding);
            if (encryptionKey != null)
            {
                text = EncryptionOfSessionData.DecryptString(text, encryptionKey);
            }
            
            return JsonConvert.DeserializeObject<T>(text, Serializer)!;
        }

        return defaultGen();
    }

    private static async Task SaveTo(object element, string fileName, string? encryptionKey = null)
    {
        EnsureFolderCreated();
        
        // JSON files are intentionally stored indented, so that it's possible to do a readable text diff on it.
        var serialized = JsonConvert.SerializeObject(element, Formatting.Indented, Serializer);
        if (encryptionKey != null)
        {
            serialized = EncryptionOfSessionData.EncryptString(serialized, encryptionKey);
        }
        
        // FIXME: If the disk is full, this WILL corrupt the data that already exists, causing irrepairable loss.
        await File.WriteAllTextAsync(fileName, serialized, Encoding);
    }

    public static void DANGER_OpenUrl(string url)
    {
        var isHttp = url.ToLowerInvariant().StartsWith("https://") || url.ToLowerInvariant().StartsWith("http://");
        if (!isHttp) throw new Exception("URL must be HTTP or HTTPS. This must be caught by the caller, not here!");
        
        Process.Start(new ProcessStartInfo
        {
            // SECURITY: Don't allow any URL here. Otherwise, this can cause a RCE.
            FileName = url,
            UseShellExecute = true
        });
    }

    public static string RandomUID__NotCryptographicallySecure()
    {
        var randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(randomBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }
    }

    public static Func<Task<string>> ResoniteUIDLateInitializerFn()
    {
        return async () => await GetOrCreateResoniteUID();
    }

    public static async Task WriteToResponseCollectionFile(ResponseCollectionTrail trail)
    {
        // Caution: Can be called by different threads.

        await DataCollectionFileLock.WaitAsync();
        try
        {
            EnsureFolderCreated();
            var jsonLine = SerializeAsSingleLine(trail);
            await File.AppendAllTextAsync(ResponseCollectionFilePath, jsonLine + Environment.NewLine, Encoding.UTF8);
        }
        finally
        {
            DataCollectionFileLock.Release();
        }
    }

    private static void EnsureFolderCreated()
    {
        if (_folderCreated) return;
        
        Directory.CreateDirectory(SavePath());
        _folderCreated = true;
    }

    private static string SerializeAsSingleLine(ResponseCollectionTrail trail)
    {
        return JsonConvert.SerializeObject(trail, Formatting.None, Serializer);
    }

    public static void EnsureRegistryHasEncryptionKeyForSavingSessionData()
    {
        if (_encryptionKeyForSessionData != null) return;
        
        _encryptionKeyForSessionData = GetOrGenerateAndStoreEncryptionKeyInWindowsRegistry();
    }

    private static string GetOrGenerateAndStoreEncryptionKeyInWindowsRegistry()
    {
        const string registryKeyPath = @"SOFTWARE\XYVR";
        const string registryValueName = "SessionDataEncryptionKey";

        try
        {
            using (var key = Registry.CurrentUser.OpenSubKey(registryKeyPath, writable: false))
            {
                if (key?.GetValue(registryValueName) is string existingKey && !string.IsNullOrEmpty(existingKey))
                {
                    // Key exists, return it
                    return existingKey;
                }
            }

            // Key doesn't exist, generate a new one
            var newEncryptionKey = EncryptionOfSessionData.GenerateEncryptionKey();

            // Create/open the registry key for writing
            using (var key = Registry.CurrentUser.CreateSubKey(registryKeyPath))
            {
                key.SetValue(registryValueName, newEncryptionKey, RegistryValueKind.String);
            }

            return newEncryptionKey;
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to access or create encryption key in registry: {ex.Message}", ex);
        }
    }
}