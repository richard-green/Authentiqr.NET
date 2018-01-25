#tool nuget:?package=NUnit.ConsoleRunner&version=3.4.0

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(Directory("./src/Authentiqr.UI/bin") + Directory(configuration));
    CleanDirectory(Directory("./src/Authentiqr.Core/bin") + Directory(configuration));
});

Task("Restore-NuGet-Packages")
    .IsDependentOn("Clean")
    .Does(() =>
{
    NuGetRestore("./src/Authentiqr.NET.sln");
});

Task("Build")
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
	var projects = GetFiles("./src/**/*.csproj") - GetFiles("./src/**/*.Tests.csproj");

	var settings = new DotNetCorePackSettings {
		NoBuild = true,
		Configuration = configuration,
		OutputDirectory = "./output",
		ArgumentCustomization = (args) => {
			var version = new Version("2.1.3");
			return args
				.Append("/p:Version={0}", version)
				.Append("/p:AssemblyVersion={0}", version)
				.Append("/p:FileVersion={0}", version)
				.Append("/p:AssemblyInformationalVersion={0}", version);
		}
	};

	foreach (var project in projects)
	{
		DotNetCorePack(project.ToString(), settings);
	}
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
