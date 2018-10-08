//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"
#addin "nuget:?package=Cake.Incubator"

using Path = System.IO.Path;
using IO = System.IO;
using Cake.Common.Tools;
using Cake.Incubator;

//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////
var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var package = Argument("package", string.Empty);

//////////////////////////////////////////////////////////////////////
// GLOBAL VARIABLES
//////////////////////////////////////////////////////////////////////
var localPackagesDir = "../LocalPackages";
var artifactsDir = @".\artifacts";
var nugetVersion = string.Empty;
GitVersion gitVersionInfo;

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("GetVersion")
    .Does(() => 
{
    gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });
    nugetVersion = gitVersionInfo.NuGetVersion;

    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(nugetVersion);

    Information("Building Terraform v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
    Verbose("GitVersion:\n{0}", gitVersionInfo.Dump());
});

Task("Pack")
    .Does(() => 
{
    Information($"Building Octopus.Dependencies.TerraformCLI v{nugetVersion}");
    NuGetPack("terraform.nuspec", new NuGetPackSettings {
        BasePath = ".",
        OutputDirectory = artifactsDir,
        Version = nugetVersion
    });
});

Task("Publish")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn("Pack")
    .Does(() =>
{
    NuGetPush($"{artifactsDir}/Octopus.Dependencies.TerraformCLI.{nugetVersion}.nupkg", new NuGetPushSettings {
        Source = EnvironmentVariable("OctopusDependeciesFeedUrl"),
        ApiKey = EnvironmentVariable("FeedzIoApiKey")
    });
});

Task("CopyToLocalPackages")
    .WithCriteria(BuildSystem.IsLocalBuild)
    .IsDependentOn("Pack")
    .Does(() =>
{
    CreateDirectory(localPackagesDir);
    CopyFileToDirectory(Path.Combine(artifactsDir, $"Octopus.Dependencies.TerraformCLI.{nugetVersion}.nupkg"), localPackagesDir);
});


Task("FullChain")
    .IsDependentOn("GetVersion")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish")
    .IsDependentOn("CopyToLocalPackages");

Task("Default").IsDependentOn("FullChain");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);