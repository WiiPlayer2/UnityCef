#load cake/vars.cake
//#module nuget:?package=Cake.Parallel.Module&version=0.20.3
#addin nuget:?package=Cake.Git
#addin nuget:?package=SharpZipLib
#addin nuget:?package=Cake.Compression
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
   // Executed BEFORE the first task.
   Information("Running tasks...");
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

// cef ////////////////////////////////////////////////////////////////////////
Task("cef-update")
.Does(() => {
    Information("Updating cef repository...");
    GitCheckout("./cef", $"origin/{cef_branch}");
    GitReset("./cef", GitResetMode.Hard);
    GitClean("./cef");
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
.IsDependentOn("cef-download")
.DoesForEach(cefPlatforms, (platform) => {
    Information($"Copy cef {platform} binaries...");
    var cefDir = $"./tmp/cef_binary_{cef_version}_{platform}";
    var cefOutDir = $"./cef_{platform}";
    EnsureDirectoryExists(cefOutDir);
    CleanDirectory(cefOutDir);
    CopyDirectory($"{cefDir}/Resources", cefOutDir);
    CopyDirectory($"{cefDir}/Release", cefOutDir);
    MoveFile($"{cefOutDir}/libcef.dll", $"{cefOutDir}/libcef-unity.dll");
    MoveFile($"{cefOutDir}/libcef.lib", $"{cefOutDir}/libcef-unity.lib");
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

Task("cefglue-copy")
.IsDependentOn("cefglue-build")
.Does(() => {
    Information("Copy binaries to Assets...");
    CopyFile("./cefglue/CefGlue/bin/Release/Xilium.CefGlue.dll", "./Assets/UnityCef/Xilium.CefGlue.dll");
});

// companion /////////////////////////////////////////////////////////////////////////////
Task("companion-build")
.IsDependentOn("cefglue-build")
.Does(() => {
    Information("Building companion app...");
    MSBuild("./UnityCef.Companion/UnityCef.Companion.sln", config =>
        config.SetConfiguration("Release")
            .SetVerbosity(msbuild_verbosity)
            .SetPlatformTarget(PlatformTarget.x64));
});

Task("companion-copy")
.IsDependentOn("companion-build")
.Does(() => {
    Information("Copying companion app...");
    CopyFile("./UnityCef.Companion/UnityCef.Companion/bin/Release/UnityCef.Companion.exe", "./Assets/UnityCef/UnityCef.Companion.exe");
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
        .WithToken("skip_update", skip_update)
        .ToString();
    FileWriteText("./cake/vars.sample.cake", text);
});

Task("Default")
.IsDependentOn("cef-copy")
.IsDependentOn("cefglue-copy")
.IsDependentOn("companion-copy");

RunTarget(target);
