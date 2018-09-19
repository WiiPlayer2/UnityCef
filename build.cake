#load cake/vars.cake
//#module nuget:file:///d:/nuget/?package=Cake.Parallel.Module
#addin nuget:?package=Cake.Git
#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression
#addin nuget:?package=Cake.Unity3D
#addin "Cake.FileHelpers"

var cefPlatforms = new string[]
{
    "windows32",
    "windows64",
};

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

Setup(ctx =>
{
    Information("Checking python 2.7 path...");
    if(string.IsNullOrWhiteSpace(python27_path))
        python27_path = "python.exe";
    var success = true;
    try
    {
        var exitCode = StartProcess(python27_path, new ProcessSettings
        {
            Arguments = "-V",
            Silent = true,
            RedirectStandardError = true,
        }, out var _, out var error);
        if(!string.Join("\n", error).Contains("Python 2.7"))
            success = false;
    }
    catch(System.ComponentModel.Win32Exception)
    {
        success = false;
    }
    if(!success)
        throw new ArgumentException($"Invalid python2.7 path ({python27_path})", nameof(python27_path));

    Information($"Checking Unity version \"{unity_version}\"...");
    if(!TryGetUnityInstall(unity_version, out var __))
        throw new ArgumentException($"Unity version \"{unity_version}\" not found", nameof(unity_version));

    Information("Running tasks...");

    // Disabling this check for now until I figure out how to sucessfully compile cef on windows
    /*
    var fullCefDownloadPath = System.IO.Path.GetFullPath(cef_download_dir);
    if (fullCefDownloadPath.Length >= 35)
        throw new ArgumentException($"Path too long (>= 35 characters, full path: {fullCefDownloadPath})", nameof(cef_download_dir));
    */
});

Teardown(ctx =>
{
   // Executed AFTER the last task.
   Information("Finished running tasks.");
});

///////////////////////////////////////////////////////////////////////////////
// TASKS
///////////////////////////////////////////////////////////////////////////////

Task("tmp-create")
.Does(() => {
    EnsureDirectoryExists("./tmp");
});

Task("tmp-clean")
.IsDependentOn("tmp-create")
.Does(() =>
{
    CleanDirectory("./tmp");
})
.OnError(exception =>
{
    Warning("Failed to clean directory the normal way.");
    Warning("Initiating force clean...");

    var dirInfo = new DirectoryInfo("./tmp");
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
                d.Attributes = d.Attributes & ~FileAttributes.ReadOnly;
            d.Delete();
        }
        catch
        {
            Information(d.FullName);
            Information(d.Attributes);
            throw;
        }
    }
});

// cef ////////////////////////////////////////////////////////////////////////
Task("cef-update")
.Does(() => {
    Information("Updating cef repository...");
    GitCheckout("./cef", $"origin/{cef_branch}");
    GitReset("./cef", GitResetMode.Hard);
    GitClean("./cef");
});

Task("cef-automate-git")
.IsDependentOn("tmp-create")
.Does(() => {
    EnsureDirectoryExists(cef_download_dir);
    DownloadFile("https://bitbucket.org/chromiumembedded/cef/raw/master/tools/automate/automate-git.py", "./tmp/automate-git.py");
    //StartProcess(python27_path, $"./tmp/automate-git.py \"--download-dir={cef_download_dir}\" --branch={cef_branch} --no-build");
    StartProcess(python27_path, new ProcessSettings
    {
        Arguments = $"./tmp/automate-git.py \"--download-dir={cef_download_dir}\" --branch={cef_branch} --force-build --force-distrib --x64-build --no-debug-build",
        EnvironmentVariables = new Dictionary<string, string>
            {
                { "GN_DEFINES", "is_official_build=true" },
                { "GYP_MSVS_VERSION", "2017" },
                { "CEF_ARCHIVE_FORMAT", "tar.bz2" },
            },
    });
});

Task("cef-download")
.IsDependentOn("tmp-create")
.DoesForEach(cefPlatforms, (platform) => {
    var dlUrl = $"http://opensource.spotify.com/cefbuilds/cef_binary_{cef_version}_{platform}.tar.bz2";
    var fileName = $"./tmp/cef_binary_{cef_version}_{platform}.tar.bz2";

    if(!FileExists(fileName))
    {
        Information($"Downloading {dlUrl}...");
        DownloadFile(dlUrl, fileName);
        Information($"Uncompressing {fileName}...");
        BZip2Uncompress(fileName, "./tmp");
    }
    else
    {
        Information($"{fileName} already downloaded");
    }
});

Task("cef-copy")
.IsDependentOn("companion-copy")
.IsDependentOn("cef-download")
.DoesForEach(cefPlatforms, (platform) => {
    Information($"Copying cef {platform} binaries...");
    var cefDir = $"./tmp/cef_binary_{cef_version}_{platform}";
    var cefOutDir = $"./Assets/UnityCef/Companion/{platform}";
    CopyDirectory($"{cefDir}/Resources", cefOutDir);
    CopyDirectory($"{cefDir}/Release", cefOutDir);
});

// cefglue ///////////////////////////////////////////////////////////////////
Task("cefglue-update")
.WithCriteria(!skip_update)
.Does(() => {
    Information("Updating cefglue repository...");
    GitCheckout("./cefglue", $"origin/{cefglue_branch}");
    GitReset("./cefglue", GitResetMode.Hard);
    GitClean("./cefglue");
});

Task("cefglue-copy-headers")
.IsDependentOn("cef-download")
.IsDependentOn("cefglue-update")
.Does(() => {
    Information("Copying cef include files...");
    CleanDirectory("./cefglue/CefGlue.Interop.Gen/include");
    CopyDirectory($"./tmp/cef_binary_{cef_version}_windows64/include", "./cefglue/CefGlue.Interop.Gen/include");
    DeleteFile("./cefglue/CefGlue.Interop.Gen/include/cef_thread.h");
    DeleteFile("./cefglue/CefGlue.Interop.Gen/include/cef_waitable_event.h");
});

Task("cefglue-generate-files")
.IsDependentOn("cefglue-copy-headers")
.Does(() => {
    Information("Generate interop source files...");
    StartProcess(python27_path, new ProcessSettings
    {
        Arguments = @"-B cefglue_interop_gen.py --schema cef3 --cpp-header-dir include --cefglue-dir ..\CefGlue\ --no-backup",
        RedirectStandardOutput = true,
        Silent = true,
        WorkingDirectory = MakeAbsolute(Directory("./cefglue/CefGlue.Interop.Gen")),
    });
    DeleteFiles("./cefglue/CefGlue/**/*.disabled.cs");
});

Task("cefglue-csproj")
.IsDependentOn("cefglue-generate-files")
.Does(() => {
    Information("Update source files in project...");
    var dir = new DirectoryPath("./cefglue/CefGlue")
        .MakeAbsolute(Context.Environment);
    var compileOutputBuilder = new StringBuilder();
    foreach(var f in GetFiles("./cefglue/CefGlue/**/*.cs"))
    {
        var path = dir.GetRelativePath(f).FullPath.Replace('/', '\\');
        compileOutputBuilder.AppendLine($"    <Compile Include=\"{path}\" />");
    }
    compileOutputBuilder.AppendLine("    <None Include=\"..\\Xilium.CefGlue.snk\">\n      <Link>Properties\\Xilium.CefGlue.snk</Link>\n    </None>");
    XmlPoke("./cefglue/CefGlue/CefGlue.csproj", "//*[local-name() = 'Compile']/..", compileOutputBuilder.ToString());
});

Task("cefglue-build")
.IsDependentOn("cefglue-csproj")
.Does(() => {
    Information("Building cefglue...");
    MSBuild("./cefglue/CefGlue/CefGlue.csproj", config =>
        config.SetConfiguration("Release")
            .SetVerbosity(msbuild_verbosity)
            .WithConsoleLoggerParameter("ErrorsOnly"));
});

// companion /////////////////////////////////////////////////////////////////////////////
Task("companion-clean")
.Does(()=>
{
    CleanDirectory("./UnityCef.Companion/UnityCef.Shared/bin");
    CleanDirectory("./UnityCef.Companion/UnityCef.Companion/bin");
});

Task("companion-build")
.IsDependentOn("companion-clean")
.IsDependentOn("cefglue-build")
.DoesForEach(new[]{ PlatformTarget.x86, PlatformTarget.x64 }, platform =>
{
    Information($"Building companion app ({platform})...");
    NuGetRestore("./UnityCef.Companion/UnityCef.Companion.sln");
    MSBuild("./UnityCef.Companion/UnityCef.Companion.sln", config =>
        config.SetConfiguration("Release")
            .SetVerbosity(msbuild_verbosity)
            .SetPlatformTarget(platform));
});

Task("companion-copy")
.IsDependentOn("companion-build")
.Does(() => {
    Information("Copying companion binaries...");
    EnsureDirectoryExists("./Assets/UnityCef/libs");
    CleanDirectory("./Assets/UnityCef/libs");
    CopyFile("./UnityCef.Companion/UnityCef.Shared/bin/Release/netstandard2.0/UnityCef.Shared.dll", "./Assets/UnityCef/libs/UnityCef.Shared.dll");
    CopyFile("./UnityCef.Companion/packages/SharedMemory.2.1.0/lib/net45/SharedMemory.dll", "./Assets/UnityCef/libs/SharedMemory.dll");

    EnsureDirectoryExists("./Assets/UnityCef/Companion");
    CleanDirectory("./Assets/UnityCef/Companion");
    EnsureDirectoryExists("./Assets/UnityCef/Companion/windows32");
    CopyDirectory("./UnityCef.Companion/UnityCef.Companion/bin/x86/Release", "./Assets/UnityCef/Companion/windows32");
    EnsureDirectoryExists("./Assets/UnityCef/Companion/windows64");
    CopyDirectory("./UnityCef.Companion/UnityCef.Companion/bin/x64/Release", "./Assets/UnityCef/Companion/windows64");
});

// unity ///////////////////////////////////////////////////////////////////////////
Task("unity-package")
.IsDependentOn("companion-copy")
.IsDependentOn("cef-copy")
.Does(() =>
{
    Information("Packing Unity asset package...");
    TryGetUnityInstall(unity_version, out var unityPath);
    StartProcess(unityPath, @"-projectPath ./ -quit -batchmode -exportPackage Assets/UnityCef UnityCef.unitypackage");
});

// cake ////////////////////////////////////////////////////////////////////////////
Task("cake-vars")
.Does(() => {
    Information("Generating vars.sample.cake...");
    var text = TransformTextFile("./cake/vars.cake.template")
        .WithToken("cef_version", cef_version)
        .WithToken("cef_branch", cef_branch)
        .WithToken("cefglue_branch", cefglue_branch)
        .WithToken("msbuild_verbosity", msbuild_verbosity)
        .WithToken("skip_update", skip_update.ToString().ToLower())
        .WithToken("skip_cef_build", skip_cef_build.ToString().ToLower())
        .WithToken("unity_version", unity_version)
        .ToString();
    FileWriteText("./cake/vars.sample.cake", text);
});

// dev /////////////////////////////////////////////////////////////////////////////
Task("dev")
.IsDependentOn("companion-copy")
.IsDependentOn("cef-copy");

Task("Default")
.IsDependentOn("cef-copy")
.IsDependentOn("companion-copy");

RunTarget(target);
