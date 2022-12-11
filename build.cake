#addin nuget:?package=SharpZipLib&version=1.4.1
#addin nuget:?package=Cake.Compression&version=0.3.0
#addin nuget:?package=Cake.Unity&version=0.9.0
#addin nuget:?package=Cake.Yaml&version=4.0.0
#addin nuget:?package=YamlDotNet&version=6.1.2
#addin nuget:?package=Cake.Git&version=2.0.0
#addin nuget:?package=Cake.FileHelpers&version=5.0.0

const string CONFIGURATION = "Release";
readonly string[] platforms = new[]
{
   "win-x64",
};

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

///////////////////////////////////////////////////////////////////////////////
// ARGUMENTS
///////////////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var packageVersion = Argument("version", "dev");

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////

UnityEditorDescriptor unityEditor = default!;
string currentHash = default!;

Setup(ctx =>
{
   Information("Find Unity version...");
   var unityProjectVersionValues = DeserializeYamlFromFile<Dictionary<string, string>>("./ProjectSettings/ProjectVersion.txt");
   var unityVersion = UnityVersion.Parse(unityProjectVersionValues["m_EditorVersion"]);
   unityEditor = FindUnityEditor(unityVersion.Year, unityVersion.Stream, unityVersion.Update);
   if(unityEditor is null)
      Error($"Unity {unityVersion} was not found.");
   Information($"Found Unity {unityVersion} at {unityEditor.Path}");

   Information("Get current hash...");
   if(GitIsValidRepository("."))
   {
      var currentBranch = GitBranchCurrent(".");
      var lastCommit = currentBranch.Tip;
      currentHash = lastCommit.Sha;
      if(GitHasUncommitedChangesHACK("."))
      {
         Warning("Repository has uncommited changes. Appending \"-dirty\" to hash.");
         currentHash += "-dirty";
      }
   }
   else
   {
      Warning("Not building from repository. Hash will be \"nogit\".");
      currentHash = "nogit";
   }
   Information($"Current hash is \"{currentHash}\"");
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
.IsDependentOn("generate-hash-companion")
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
.IsDependentOn("generate-hash")
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

Task("generate-hash-companion")
.IsDependentOn("build-companion")
.DoesForEach(platforms, platform =>
{
   var path = $"./cef_{platform}/.hash";
   Information($"Generating {path}...");
   FileWriteText(path, currentHash);
});

Task("generate-hash-unity")
.Does(() =>
{
   var path = "./Assets/UnityCef/Editor/Constants.g.cs";
   Information($"Generating {path}...");
   var constantsCode = TransformTextFile("./Assets/UnityCef/Editor/Constants.g.cs.template")
      .WithToken($"hash", currentHash)
      .ToString();
   FileWriteText(path, constantsCode);
});

Task("generate-hash")
.IsDependentOn("generate-hash-companion")
.IsDependentOn("generate-hash-unity");

RunTarget(target);
