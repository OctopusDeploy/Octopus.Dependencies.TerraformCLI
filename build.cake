//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////
#tool "nuget:?package=GitVersion.CommandLine&version=3.6.5"

#addin nuget:?package=Cake.Incubator&version=6.0.0
#addin nuget:?package=Cake.Http&version=1.3.0
#addin nuget:?package=Cake.Json&version=6.0.1
#addin nuget:?package=Newtonsoft.Json&version=13.0.1

using Path = System.IO.Path;
using IO = System.IO;
using Cake.Common.Tools;
using Cake.Incubator;
using System.Linq;

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
var artifactsDir = @"./artifacts";
var nugetVersion = string.Empty;
var buildDir = @"./build";

string terraformVersion = "0.11.15";
string awsPluginVersion = "1.39.0";
string azurePluginVersion = "0.1.1";
string azureRmPluginVersion = "1.17.0";

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
    CleanDirectory(artifactsDir);
    if (FileExists("./terraform.temp.nuspec"))
    {
        DeleteFile("./terraform.temp.nuspec");
    }
});

Task("GetVersion")
    .Does(() =>
{
    var gitVersionInfo = GitVersion(new GitVersionSettings {
        OutputType = GitVersionOutput.Json
    });
    nugetVersion = gitVersionInfo.NuGetVersion;

    if(BuildSystem.IsRunningOnTeamCity)
        BuildSystem.TeamCity.SetBuildNumber(nugetVersion);

    Information("Building Terraform v{0}", nugetVersion);
    Information("Informational Version {0}", gitVersionInfo.InformationalVersion);
    Verbose("GitVersion:\n{0}", gitVersionInfo.Dump());
});

Task("Download")
    .Does(() =>
{
    void retrieveAndUnzip(string url, string outputPath, string destination)
    {
        CreateDirectory(destination);
        Information($"Downloading {url}");
        DownloadFile(url, outputPath);
        Unzip(outputPath, destination);
    }

    System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

    foreach (var plat in new[] {"windows_386"})
    {
        var cliDir = $"{buildDir}/{plat}";
        var pluginDir = $"{cliDir}/plugins/{plat}";

        retrieveAndUnzip(
            $"https://releases.hashicorp.com/terraform/{terraformVersion}/terraform_{terraformVersion}_{plat}.zip", 
            File($"{buildDir}/terraform_{terraformVersion}_{plat}.zip"), 
            cliDir);

        retrieveAndUnzip(
            $"https://releases.hashicorp.com/terraform-provider-aws/{awsPluginVersion}/terraform-provider-aws_{awsPluginVersion}_{plat}.zip", 
            File($"{buildDir}/terraform-provider-aws_{awsPluginVersion}_{plat}.zip"), 
            pluginDir);

        retrieveAndUnzip(
            $"https://releases.hashicorp.com/terraform-provider-azure/{azurePluginVersion}/terraform-provider-azure_{azurePluginVersion}_{plat}.zip", 
            File($"{buildDir}/terraform-provider-azure_{azurePluginVersion}_{plat}.zip"), 
            pluginDir);

        retrieveAndUnzip(
            $"https://releases.hashicorp.com/terraform-provider-azurerm/{azureRmPluginVersion}/terraform-provider-azurerm_{azureRmPluginVersion}_{plat}.zip", 
            File($"{buildDir}/terraform-provider-azurerm_{azureRmPluginVersion}_{plat}.zip"), 
            pluginDir);
    }
});

Task("Pack")
    .Does(() =>
{
    foreach (var plat in new[] {"windows_386"})
    {
        var plugins = string.Join(Environment.NewLine, System.IO.Directory.EnumerateFiles($"{buildDir}/{plat}/plugins/{plat}").Select(x => new FileInfo(x)).Select(x=>x.Name)).Trim();

        Information($"Building Octopus.Dependencies.TerraformCLI v{nugetVersion}");

        NuGetPack("terraform.nuspec", new NuGetPackSettings {
            BasePath = $"{buildDir}/{plat}",
            OutputDirectory = artifactsDir,
            Id = $"Octopus.Dependencies.TerraformCLI",
            Version = nugetVersion,
            Properties = new Dictionary<string, string> { { "terraformVersion", terraformVersion }, { "plugins", plugins }}
        });
    }
});

Task("Publish")
    .WithCriteria(BuildSystem.IsRunningOnTeamCity)
    .IsDependentOn("Pack")
    .Does(() =>
{
    var packages = GetFiles($"{artifactsDir}/Octopus.Dependencies.TerraformCLI.*.nupkg");

    NuGetPush(packages, new NuGetPushSettings {
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
    CopyFiles($"{artifactsDir}/Octopus.Dependencies.TerraformCLI.*.nupkg", localPackagesDir);
});

Task("FullChain")
    .IsDependentOn("Clean")
    .IsDependentOn("GetVersion")
    .IsDependentOn("Download")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish")
    .IsDependentOn("CopyToLocalPackages");

Task("Default").IsDependentOn("FullChain");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);