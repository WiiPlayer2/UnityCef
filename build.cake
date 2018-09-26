#load cake/vars.cake
#load cake/helpers.cake
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
    Information("Committing new timestamps...");
    CommitTimestamps();
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
    if(DirectoryExists("./tmp"))
    {
        DeleteDirectory("./tmp", new DeleteDirectorySettings
        {
            Force = true,
            Recursive = true,
        });
    }
})
.OnError(exception =>
{
    Warning("Failed to clean directory the normal way.");
    ForceCleanDirectory("./tmp");
})
.DeferOnError();

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
    SetTimestamp($"./tmp/cef_binary_{cef_version}_{platform}");
});

Task("cef-clean")
.DoesForEach(cefPlatforms, platform =>
{
    var cefDir = $"./cef_{platform}";
    if(DirectoryExists(cefDir))
    {
        Information($"Cleaning {cefDir}...");
        DeleteDirectory(cefDir, new DeleteDirectorySettings
        {
            Force = true,
            Recursive = true,
        });
    }
})
.DeferOnError();

Task("cef-copy")
.IsDependentOn("companion-copy")
.IsDependentOn("cef-download")
.WithCriteria(!CheckTimestampsForEach(cefPlatforms, platform => $"./tmp/cef_binary_{cef_version}_{platform}"))
.DoesForEach(cefPlatforms, (platform) => {
    Information($"Copying cef {platform} binaries...");
    var cefDir = $"./tmp/cef_binary_{cef_version}_{platform}";
    var cefOutDir = $"./cef_{platform}";
    EnsureDirectoryExists(cefDir);
    CopyDirectory($"{cefDir}/Resources", cefOutDir);
    CopyDirectory($"{cefDir}/Release", cefOutDir);
});

// cefglue ///////////////////////////////////////////////////////////////////
Task("cefglue-clone")
.WithCriteria(!DirectoryExists("./cefglue"))
.Does(() =>
{
    Information("Cloning cefglue repository...");
    GitClone("https://gitlab.com/xiliumhq/chromiumembedded/cefglue.git", "./cefglue");
});

//FIXME: Create local branch if it doesn't exist yet so the branch check succeeds
Task("cefglue-branch")
.IsDependentOn("cefglue-clone")
.Does(() => {
    var currentBranch = GitBranchCurrent("./cefglue");
    if(currentBranch.FriendlyName != cefglue_branch)
    {
        Information($"Checking out cefglue branch {cefglue_branch} repository...");
        GitCheckout("./cefglue", $"origin/{cefglue_branch}");
        GitReset("./cefglue", GitResetMode.Hard);
        GitClean("./cefglue");
    }
});

Task("cefglue-copy-headers")
.IsDependentOn("cef-download")
.IsDependentOn("cefglue-branch")
.WithCriteria(!CheckTimestamps("./cefglue", $"./tmp/cef_binary_{cef_version}_windows64"))
.Does(() => {
    Information("Copying cef include files...");
    CleanDirectory("./cefglue/CefGlue.Interop.Gen/include");
    CopyDirectory($"./tmp/cef_binary_{cef_version}_windows64/include", "./cefglue/CefGlue.Interop.Gen/include");
    DeleteFile("./cefglue/CefGlue.Interop.Gen/include/cef_thread.h");
    DeleteFile("./cefglue/CefGlue.Interop.Gen/include/cef_waitable_event.h");

    SetTimestamp("./cefglue/CefGlue.Interop.Gen/include");
});

Task("cefglue-generate-files")
.IsDependentOn("cefglue-copy-headers")
//.WithCriteria(!CheckTimestamp("./cefglue/CefGlue.Interop.Gen/include"))
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

    SetTimestamp("./cefglue/CefGlue");
});

Task("cefglue-csproj")
.IsDependentOn("cefglue-generate-files")
.WithCriteria(!CheckTimestamp("./cefglue/CefGlue"))
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

    SetTimestamp("./cefglue/CefGlue/CefGlue.csproj");
});

Task("cefglue-build")
.IsDependentOn("cefglue-csproj")
.WithCriteria(() => !CheckTimestamp("./cefglue/CefGlue/CefGlue.csproj"))
.Does(() => {
    Information("Building cefglue...");
    MSBuild("./cefglue/CefGlue/CefGlue.csproj", config =>
        config.SetConfiguration("Release")
            .SetVerbosity(msbuild_verbosity)
            .WithConsoleLoggerParameter("ErrorsOnly"));
    SetTimestamp("./cefglue/CefGlue/bin/Release");
});

// companion /////////////////////////////////////////////////////////////////////////////
Task("companion-clean")
.Does(()=>
{
    CleanDirectory("./UnityCef.Companion/UnityCef.Shared/bin");
    CleanDirectory("./UnityCef.Companion/UnityCef.Companion/bin");
});

Task("companion-build")
.IsDependentOn("cefglue-build")
.WithCriteria(!CheckTimestamps("./cefglue/CefGlue/bin/Release", "./UnityCef.Companion"))
.DoesForEach(new[]{ PlatformTarget.x86, PlatformTarget.x64 }, platform =>
{
    Information($"Building companion app ({platform})...");
    NuGetRestore("./UnityCef.Companion/UnityCef.Companion.sln");
    MSBuild("./UnityCef.Companion/UnityCef.Companion.sln", config =>
        config.SetConfiguration("Release")
            .SetVerbosity(msbuild_verbosity)
            .SetPlatformTarget(platform));
    SetTimestamp($"./UnityCef.Companion");
});

Task("companion-libs-copy")
.IsDependentOn("companion-build")
.WithCriteria(!CheckTimestamp("./UnityCef.Companion"))
.Does(() =>
{
    Information("Copying companion libraries...");
    CopyFile("./UnityCef.Companion/UnityCef.Shared/bin/Release/netstandard2.0/UnityCef.Shared.dll", "./Assets/UnityCef/libs/UnityCef.Shared.dll");
    CopyFile("./tools/Addins/SharpZipLib.1.0.0/lib/net45/ICSharpCode.SharpZipLib.dll", "./Assets/UnityCef/libs/ICSharpCode.SharpZipLib.dll");
    SetTimestamp("./Assets/UnityCef");
});

Task("companion-copy")
.IsDependentOn("companion-build")
.WithCriteria(!CheckTimestamp("./UnityCef.Companion"))
.DoesForEach(cefPlatforms, platform =>
{
    var isX64 = platform.EndsWith("64");
    var arch = isX64 ? "x64" : "x86";
    var cefDir = $"./cef_{platform}";
    var binPath = $"./UnityCef.Companion/UnityCef.Companion/bin/{arch}/Release";

    Information($"Copying companion {arch} binaries to {cefDir}...");
    CopyDirectory(binPath, cefDir);
    SetTimestamp($"./cef_{platform}");
});

// unity ///////////////////////////////////////////////////////////////////////////
Task("unity-clean")
.Does(() =>
{
    Information("Cleaning companion assets...");
    EnsureDirectoryExists("./Assets/UnityCef/Companion");
    CleanDirectory("./Assets/UnityCef/Companion");
    EnsureDirectoryExists("./Assets/UnityCef/libs");
    CleanDirectory("./Assets/UnityCef/libs");
})
.DeferOnError();

Task("unity-generate")
.IsDependentOn("companion-copy")
.IsDependentOn("cef-copy")
.Does(() =>
{
    var hash = "nogit";
    if(GitIsValidRepository("."))
    {
        var currentBranch = GitBranchCurrent(".");
        var lastCommit = currentBranch.Tip;
        hash = lastCommit.Sha;
        if(GitHasUncommitedChangesHACK("."))
        {
            Warning("Repository has uncommited changes. Appending \"-dirty\" to hash.");
            hash += "-dirty";
        }
    }
    else
    {
        Warning("Not building from repository. Hash will be \"nogit\".");
    }

    var text = TransformTextFile("./Assets/UnityCef/Editor/Constants.g.cs.template")
        .WithToken($"hash", hash)
        .ToString();
    FileWriteText("./Assets/UnityCef/Editor/Constants.g.cs", text);
    foreach(var platform in cefPlatforms)
    {
        FileWriteText($"./cef_{platform}/hash", hash);
    }
});

Task("unity-zip")
.IsDependentOn("companion-copy")
.IsDependentOn("cef-copy")
.IsDependentOn("unity-generate")
.WithCriteria(!CheckTimestamp("./UnityCef.Companion") || !CheckTimestampsForEach(cefPlatforms, platform => $"./cef_{platform}"))
.DoesForEach(cefPlatforms, platform =>
{
    var cefDir = $"./cef_{platform}";
    var zipFile = $"./Assets/UnityCef/Companion/{platform}.zip";
    EnsureDirectoryExists("./Assets/UnityCef/Companion");
    Information($"Zipping {cefDir} to {zipFile}...");
    ZipCompress(cefDir, zipFile);
    SetTimestamp($"./Assets/UnityCef");
});

Task("unity-package-clean")
.DoesForEach(GetFiles("./*.unitypackage"), file =>
{
    Information($"Removing {file}...");
    DeleteFile(file);
});

Task("unity-package")
.IsDependentOn("unity-zip")
.IsDependentOn("companion-libs-copy")
.IsDependentOn("licenses")
.Does(() =>
{
    if(FileReadLines("./Assets/UnityCef/Scripts/WebBrowser.cs").Contains("#define COMPANION_DEBUG"))
        throw new Exception("COMPANION_DEBUG flag is still set in WebBrowser.cs. Cannot build package while flag is set.");

    var postfix = "nogit";
    if(GitIsValidRepository("."))
    {
        var currentBranch = GitBranchCurrent(".");
        var lastCommit = currentBranch.Tip;
        postfix = lastCommit.Sha.Substring(0, 8);
        if(GitHasUncommitedChangesHACK("."))
        {
            Warning("Repository has uncommited changes. Appending \"-dirty\" to hash.");
            postfix += "-dirty";
        }
    }
    else
    {
        Warning("Not building from repository. Postfix will be \"nogit\".");
    }
    var outPath = $"./UnityCef-{package_version}.{postfix}.unitypackage";

    Information($"Packing {outPath}...");
    TryGetUnityInstall(unity_version, out var unityPath);
    var result = CompileUnityPackage(unityPath, "./", "./Assets/UnityCef", outPath, out var compileStdout, out var compileStderr);
    Information(string.Join("\n", compileStdout));
    if(!result)
    {
        Error(string.Join("\n", compileStderr));
        throw new Exception("Failed to create asset package");
    }
    SetTimestamp(outPath);
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
        .WithToken("package_version", package_version.ToString(2))
        .ToString();
    FileWriteText("./cake/vars.sample.cake", text);
});

// dev /////////////////////////////////////////////////////////////////////////////
Task("dev-vs-update")
.IsDependentOn("cef-copy");

Task("dev-vs")
.IsDependentOn("dev-vs-update")
.Does(() =>
{
    Information("Opening UnityCef.Companion.sln...");
    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
    {
        FileName = System.IO.Path.GetFullPath("./UnityCef.Companion/UnityCef.Companion.sln"),
        UseShellExecute = true,
    }).Dispose();
});

Task("dev-unity-update")
.IsDependentOn("cef-copy")
.IsDependentOn("companion-copy")
.IsDependentOn("companion-libs-copy");

Task("dev-unity")
.IsDependentOn("dev-unity-update")
.Does(() =>
{
    Information($"Starting Unity version \"{unity_version}\"...");
    TryGetUnityInstall(unity_version, out var unityPath);
    StartAndReturnProcess(unityPath, new ProcessSettings
    {
        Arguments = @"-projectPath ./",
    }).Dispose();
});

Task("dev-update")
.IsDependentOn("dev-unity-update")
.IsDependentOn("dev-vs-update");

Task("dev")
.IsDependentOn("dev-unity")
.IsDependentOn("dev-vs");

Task("clean")
.IsDependentOn("tmp-clean")
.IsDependentOn("cef-clean")
.IsDependentOn("companion-clean")
.IsDependentOn("unity-clean")
.IsDependentOn("unity-package-clean")
.Does(() =>
{
    if(DirectoryExists("./cefglue"))
    {
        Information("Removing cefglue");
        DeleteDirectory("./cefglue", new DeleteDirectorySettings
        {
            Force = true,
            Recursive = true,
        });
    }
})
.OnError(exception =>
{
    Information("Failed to remove ./cefglue");
    ForceCleanDirectory("./cefglue");
    DeleteDirectory("./cefglue");
})
.DeferOnError();

Task("licenses")
.IsDependentOn("cef-download")
.Does(() =>
{
    Information("Creating LICENSES.txt file...");
    EnsureDirectoryExists("./tmp/licenses");
    DownloadFile("https://raw.githubusercontent.com/icsharpcode/SharpZipLib/master/LICENSE.txt", "./tmp/licenses/SharpZipLib");

    var builder = new StringBuilder();
    var add = new Action<string, string>((title, path) =>
    {
        const int MAX_LINE_WIDTH = 90;
        var titleLength = title.Length + 2;
        var spaceCount = (MAX_LINE_WIDTH - titleLength) / 2;

        // Write first title border
        for(var i = 0; i < MAX_LINE_WIDTH; i++)
        {
            builder.Append("=");
        }
        builder.AppendLine();

        // Write title
        for(var i = 0; i < spaceCount + titleLength % 2; i++)
        {
            builder.Append("=");
        }
        builder.Append($" {title} ");
        for(var i = 0; i < spaceCount; i++)
        {
            builder.Append("=");
        }
        builder.AppendLine();

        // Write second title border
        for(var i = 0; i < MAX_LINE_WIDTH; i++)
        {
            builder.Append("=");
        }
        builder.AppendLine();

        builder.AppendLine();
        builder.AppendLine(FileReadText(path));
        builder.AppendLine();
        builder.AppendLine();
    });

    add("UnityCef", "./LICENSE");
    add("Chromium Embedded Framework", $"./tmp/cef_binary_{cef_version}_windows32/LICENSE.txt");
    add("SharpZipLib", "./tmp/licenses/SharpZipLib");

    FileWriteText("./Assets/UnityCef/LICENSES.txt", builder.ToString());
});

Task("Default")
.IsDependentOn("unity-package");

RunTarget(target);
