#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var version = new Version("3.2.0");
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(Directory("./output"));
    CleanDirectory(Directory("./src/Authentiqr.NET/bin") + Directory(configuration));
    CleanDirectory(Directory("./src/Authentiqr.Core/bin") + Directory(configuration));
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./src/Authentiqr.NET.sln");
});

Task("Solution-Info")
    .IsDependentOn("Clean")
    .Does(() =>
{
    var file = "./src/SolutionInfo.cs";
    CreateAssemblyInfo(file, new AssemblyInfoSettings {
        Product = "Authentiqr.NET",
        Version = version.ToString(),
        FileVersion = version.ToString(),
        InformationalVersion = version.ToString(),
        Copyright = string.Format("Copyright (c) Richard Green 2011 - {0}", DateTime.Now.Year)
    });
});

Task("Build")
    .IsDependentOn("Solution-Info")
    .IsDependentOn("Restore-NuGet-Packages")
    .Does(() =>
{
    if(IsRunningOnWindows())
    {
      // Use MSBuild
      MSBuild("./src/Authentiqr.NET.sln", settings =>
        settings.SetConfiguration(configuration));
    }
    else
    {
      // Use XBuild
      XBuild("./src/Authentiqr.NET.sln", settings =>
        settings.SetConfiguration(configuration));
    }
});

Task("Run-Unit-Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NUnit3("./src/**/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
    });
});

Task("Build-Packages")
    .IsDependentOn("Restore-NuGet-Packages")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
{
    var settings = new DotNetCorePackSettings {
        NoBuild = true,
        Configuration = configuration,
        OutputDirectory = "./output"
    };

    DotNetCorePack("./src/Authentiqr.Core/Authentiqr.Core.csproj", settings);

    Zip("./src/Authentiqr.NET/bin/" + configuration, "./output/Authentiqr.NET-" + version.ToString() + ".zip");
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build-Packages");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
