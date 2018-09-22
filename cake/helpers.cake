#addin "Cake.FileHelpers"
using System.Threading;
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

//HACK: Uses git executable even though Cake.Git is being used (but does not report the correct dirty status)
bool GitHasUncommitedChangesHACK(DirectoryPath repositoryPath)
{
    try
    {
        var exitCode = StartProcess("git", new ProcessSettings
        {
            Arguments = "diff --exit-code",
            WorkingDirectory = repositoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Silent = true,
        });
        return exitCode == 1;
    }
    catch(Exception e)
    {
        Error(e);
        return true;
    }
}

bool CompileUnityPackage(FilePath unityPath, DirectoryPath projectPath, DirectoryPath assetPath, DirectoryPath outPath, out IEnumerable<string> stdout, out IEnumerable<string> stderr)
{
    try
    {
        var exitCode = StartProcess(unityPath, new ProcessSettings
        {
            Arguments = $@"-projectPath {projectPath} -quit -batchmode -exportPackage {assetPath} {outPath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Silent = true,
        });
        Thread.Sleep(1000);
        var logFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Unity/Editor/Editor.log");
        var logLines = FileReadLines(logFile);
        stdout = logLines
            .SkipWhile(line => !line.StartsWith("-----CompilerOutput:-stdout"))
            .Skip(1)
            .TakeWhile(line => !line.StartsWith("-----CompilerOutput:-stderr"));
        stderr = logLines
            .SkipWhile(line => !line.StartsWith("-----CompilerOutput:-stderr"))
            .Skip(1)
            .TakeWhile(line => !line.StartsWith("-----EndCompilerOutput"));
        return exitCode == 0;
    }
    catch(Exception e)
    {
        Error(e);
        throw;
    }
}
