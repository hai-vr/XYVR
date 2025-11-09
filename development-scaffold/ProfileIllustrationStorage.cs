using System.Collections.Immutable;
using XYVR.Core;

namespace XYVR.Scaffold;

[Serializable]
public class ProfileIllustrationStorage
{
    [NonSerialized] private string? _profileIllustrationsFolderPath;
    
    // Public for serialization
    public readonly Dictionary<string, ProfileIllustrationEntry> illustrations = new();

    public void SetPath(string profileIllustrationsFolderPath)
    {
        _profileIllustrationsFolderPath = profileIllustrationsFolderPath;
    }

    public void PreProcess()
    {
        if (_profileIllustrationsFolderPath == null) throw new InvalidOperationException("Path must be set before pre-processing");

        var allKnownGuids = new HashSet<string>();
        
        foreach (var entry in illustrations)
        {
            if (entry.Value.previousGuids == null || entry.Value.previousGuids.Length == 0)
            {
                illustrations[entry.Key] = entry.Value with { previousGuids = [entry.Value.fileGuid] };
            }
            
            allKnownGuids.UnionWith(entry.Value.previousGuids);
        }

        var allFiles = Directory.EnumerateFiles(_profileIllustrationsFolderPath)
            .Select(Path.GetFileName)
            .Where(s => Guid.TryParse(s, out _))
            .ToHashSet();
        allFiles.ExceptWith(allKnownGuids);
        
        foreach (var file in allFiles)
        {
            XYVRLogging.WriteLine(this, $"The file {file} is not referenced. Is this intended?");
        }
    }

    public async Task Store(string individualGuid, byte[] data, string type)
    {
        if (_profileIllustrationsFolderPath == null) throw new InvalidOperationException("Path must be set before storing");

        var guid = XYVRGuids.ForProfileIllustrationFileName();

        if (!illustrations.TryGetValue(individualGuid, out var existingEntry))
        {
            existingEntry = new ProfileIllustrationEntry { type = type, fileGuid = guid, previousGuids = [] };
        }
        
        await File.WriteAllBytesAsync(Path.Combine(_profileIllustrationsFolderPath, guid), data);
        illustrations[individualGuid] = existingEntry with
        {
            fileGuid = guid,
            previousGuids = [..existingEntry.previousGuids.Add(guid).Distinct()]
        };
    }

    public async Task<(string?, byte[]?)> RetrieveOrNull(string individualGuid)
    {
        if (_profileIllustrationsFolderPath == null) throw new InvalidOperationException("Path must be set before retrieving");

        if (illustrations.TryGetValue(individualGuid, out var entry))
        {
            var fileGuid = entry.fileGuid;
            
            // For security purposes:
            // We check if the Guid is valid to prevent weird path traversal, as if the stored data could contain untrusted input.
            // Not sure what kind of attacks this may prevent, but might as well do a check here.
            if (IsValidFileGuid(fileGuid))
            {
                return (entry.type, await File.ReadAllBytesAsync(Path.Combine(_profileIllustrationsFolderPath, fileGuid)));
            }
        }

        return (null, null);
    }

    private static bool IsValidFileGuid(string fileGuid)
    {
        return Guid.TryParse(fileGuid, out _);
    }
}

[Serializable]
public record ProfileIllustrationEntry
{
    public required string type { get; init; }
    public required string fileGuid { get; init; }
    public required ImmutableArray<string> previousGuids { get; init; } = [];
}