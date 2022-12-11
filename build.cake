#addin nuget:?package=SharpZipLib&version=1.4.1
#addin nuget:?package=Cake.Compression&version=0.3.0
#addin nuget:?package=Cake.Unity&version=0.9.0
#addin nuget:?package=Cake.Yaml&version=4.0.0
#addin nuget:?package=YamlDotNet&version=6.1.2

const string CONFIGURATION = "Release";
readonly string[] platforms = new[]
{
   "win-x64",
};

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var packageVersion = Argument("version", "dev");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

UnityEditorDescriptor unityEditor = default!;

Setup(ctx =>
{
   Information("Find Unity version...");
   var unityProjectVersionValues = DeserializeYamlFromFile<Dictionary<string, string>>("./ProjectSettings/ProjectVersion.txt");
   var unityVersion = UnityVersion.Parse(unityProjectVersionValues["m_EditorVersion"]);
   unityEditor = FindUnityEditor(unityVersion.Year, unityVersion.Stream, unityVersion.Update);
   if(unityEditor is null)
      Error($"Unity {unityVersion} was not found.");
   Information($"Found Unity {unityVersion} at {unityEditor.Path}");
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

Task("pack-unity")
.IsDependentOn("pack-companion")
.Does(() => {
   var outPath = $"./UnityCef-{packageVersion}.unitypackage";
   Information($"Packing {outPath}...");
   var files = GetFiles("./Assets/UnityCef/*");
   UnityEditor(
      unityEditor,
      new UnityEditorArguments()
      {
         ProjectPath = "./",
         ExportPackage = new ExportPackage()
         {
            PackageName = outPath,
            AssetPaths = new[]{"Assets/UnityCef"},
         },
         LogFile = "./unity.log",
      },
      new UnityEditorSettings()
      {
         RealTimeLog = true,
      }
   );
});

RunTarget(target);
