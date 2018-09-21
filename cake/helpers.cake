#addin "Cake.FileHelpers"
using System.Globalization;

var timestampList = new HashSet<string>();

var GetTimestampPath = new Func<string, string>(path =>
{
    return System.IO.Path.Combine("./tmp/timestamps", path);
});

var GetTimestamp = new Func<string, DateTime>(path =>
{
    if(FileExists(path))
        return new FileInfo(path).LastWriteTimeUtc;

    var dirInfo = new DirectoryInfo(path);
    if(!dirInfo.Exists)
        return new DateTime(0);

    var timestamp = dirInfo.LastWriteTimeUtc;
    var fsEntries = dirInfo.EnumerateFileSystemInfos("*", SearchOption.AllDirectories);
    foreach(var fsInfo in fsEntries)
    {
        if(fsInfo.LastWriteTimeUtc > timestamp)
                timestamp = fsInfo.LastWriteTimeUtc;
    }
    return timestamp;
});

var CheckTimestamp = new Func<string, bool>(path =>
{
    var timestamp = GetTimestamp(path);
    var dir = GetTimestampPath(path);
    var file = System.IO.Path.Combine(dir, "___timestamp");
    
    if(FileExists(file))
    {
        var oldTimestampText = FileReadText(file);
        var oldTimestampUtc = long.Parse(oldTimestampText, CultureInfo.InvariantCulture);

        return oldTimestampUtc == timestamp.ToFileTimeUtc();
    }
    return false;
});

bool CheckTimestamps(params string[] paths)
{
    return paths
        .Select(o => CheckTimestamp(o))
        .Aggregate(true, (acc, curr) => acc && curr);
}

bool CheckTimestampsForEach<T>(IEnumerable<T> values, Func<T, string> pathFunc)
{
    return CheckTimestamps(values
        .Select(o => pathFunc(o))
        .ToArray());
}

var SetTimestamp = new Action<string>(path =>
{
    timestampList.Add(path);
});

var CommitTimestamps = new Action(() =>
{
    foreach(var path in timestampList)
    {
        var timestamp = GetTimestamp(path);
        var dir = GetTimestampPath(path);
        var file = System.IO.Path.Combine(dir, "___timestamp");

        EnsureDirectoryExists(dir);
        FileWriteText(file, timestamp.ToFileTimeUtc().ToString(CultureInfo.InvariantCulture));
    }
});

void ForceCleanDirectory(string path)
{
    Warning("Initiating force clean...");

    var dirInfo = new DirectoryInfo(path);
    foreach(var f in dirInfo.EnumerateFiles("*", SearchOption.AllDirectories))
    {
        try
        {
            if(f.Attributes.HasFlag(FileAttributes.ReadOnly))
                f.Attributes = f.Attributes & ~FileAttributes.ReadOnly;
            f.Delete();
        }
        catch
        {
            Information(f.FullName);
            Information(f.Attributes);
            throw;
        }
    }
    foreach(var d in dirInfo.EnumerateDirectories("*", SearchOption.AllDirectories))
    {
        try
        {
            if(d.Attributes.HasFlag(FileAttributes.ReadOnly))
                d.Attributes &= ~FileAttributes.ReadOnly;
            if(d.Attributes.HasFlag(FileAttributes.Archive))
                d.Attributes &= ~FileAttributes.Archive;
            d.Delete();
        }
        catch
        {
            Information(d.FullName);
            Information(d.Attributes);
            throw;
        }
    }
}
