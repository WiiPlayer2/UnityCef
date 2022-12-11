#addin nuget:?package=SharpZipLib&version=1.4.1
#addin nuget:?package=Cake.Compression&version=0.3.0

const string CONFIGURATION = "Release";
readonly string[] platforms = new[]
{
   "win-x64",
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

Task("build-companion")
.DoesForEach(platforms, platform =>
{
   Information($"Publishing companion app ({platform})...");
   DotNetPublish("./UnityCef.Companion/UnityCef.Companion/UnityCef.Companion.csproj", new DotNetPublishSettings()
   {
      Configuration = CONFIGURATION,
      SelfContained = true,
      Runtime = platform,
      OutputDirectory = $"./Builds/companion/{platform}",
   });
});

Task("copy-libraries")
.IsDependentOn("build-companion")
.Does(() =>
{
   Information("Copying companion libraries...");
   CopyFile("./UnityCef.Companion/UnityCef.Shared/bin/Release/netstandard2.0/UnityCef.Shared.dll", "./Assets/UnityCef/libs/UnityCef.Shared.dll");
   CopyFile("./tools/Addins/SharpZipLib.1.4.1/lib/netstandard2.0/ICSharpCode.SharpZipLib.dll", "./Assets/UnityCef/libs/ICSharpCode.SharpZipLib.dll");
});

Task("pack-companion")
.IsDependentOn("build-companion")
.DoesForEach(platforms, platform =>
{
   var companionDirectory = $"./Builds/companion/{platform}";
   var zipFile = $"./Assets/UnityCef/Companion/{platform}.zip";
   EnsureDirectoryExists("./Assets/UnityCef/Companion");
   Information($"Zipping {companionDirectory} to {zipFile}...");
   ZipCompress(companionDirectory, zipFile);
});

RunTarget(target);
