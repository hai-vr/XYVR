using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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
        internal const string TEMP__CredentialsJsonFileName = "TEMP__credentials.json";
        internal const string DataCollectionFileName = "data-collection.jsonl";
        internal const string ResponseCollectionFileName = "response-collection.jsonl";
        internal const string ResoniteUidFileName = "resonite.uid";
        internal const string ReactAppJsonFileName = "ui-preferences.json";
    }
    
    private static string IndividualsJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.IndividualsJsonFileName);
    private static string ConnectorsJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ConnectorsJsonFileName);
    private static string TEMP__CredentialsJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.TEMP__CredentialsJsonFileName);
    private static string DataCollectionFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.DataCollectionFileName);
    private static string ResponseCollectionFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ResponseCollectionFileName);
    private static string ResoniteUidFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ResoniteUidFileName);
    private static string ReactAppJsonFilePath => Path.Combine(SavePath(), ScaffoldingFileNames.ReactAppJsonFileName);
    
    private static readonly Encoding Encoding = Encoding.UTF8;
    private static readonly JsonSerializerSettings Serializer = new()
    {
        Converters = { new StringEnumConverter() }
    };
    
    private static readonly SemaphoreSlim DataCollectionFileLock = new(1, 1);
    private static bool _folderCreated;
    
    private static string _pathLateInit;
    
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
    
    public static async Task<SerializedCredentials> OpenCredentials() => await OpenIfExists<SerializedCredentials>(TEMP__CredentialsJsonFilePath, () => new SerializedCredentials());
    public static async Task SaveCredentials(SerializedCredentials serialized) => await SaveTo(serialized, TEMP__CredentialsJsonFilePath);
    
    public static async Task<string> OpenResoniteUID() => await OpenIfExists<string>(ResoniteUidFilePath, RandomUID__NotCryptographicallySecure);
    public static async Task SaveResoniteUID(string serialized) => await SaveTo(serialized, ResoniteUidFilePath);
    
    public static async Task<ReactAppPreferences> OpenReactAppPreferences() => await OpenIfExists<ReactAppPreferences>(ReactAppJsonFilePath, () => new ReactAppPreferences());
    public static async Task SaveReactAppPreferences(ReactAppPreferences serialized) => await SaveTo(serialized, ReactAppJsonFilePath);

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

    private static async Task<T> OpenIfExists<T>(string fileName, Func<T> defaultGen)
    {
        return File.Exists(fileName)
            ? JsonConvert.DeserializeObject<T>(await File.ReadAllTextAsync(fileName, Encoding), Serializer)!
            : defaultGen();
    }

    private static async Task SaveTo(object element, string fileName)
    {
        EnsureFolderCreated();
        
        // FIXME: If the disk is full, this WILL corrupt the data that already exists, causing irrepairable loss.
        var serialized = JsonConvert.SerializeObject(element, Formatting.Indented, Serializer);
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
        return async () =>
        {
            var uid = await Scaffolding.OpenResoniteUID();
            await Scaffolding.SaveResoniteUID(uid);
            return uid;
        };
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
}