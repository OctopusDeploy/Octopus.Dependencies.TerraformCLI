# this script will download the latest versions of each of the plugins
# after downloading commit the updated plugins and run build.ps1

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip
{
    param([string]$zipfile, [string]$outpath)
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

# TLS 1.2
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

# read included plugins and versions
$plugins = Get-Content("plugin-versions.json") | Out-String | ConvertFrom-Json


# download terraform.exe
# $version = Invoke-RestMethod -Uri "https://checkpoint-api.hashicorp.com/v1/check/terraform"
# Invoke-WebRequest -Uri "https://releases.hashicorp.com/terraform/$($version.current_version)/terraform_$($version.current_version)_windows_386.zip" -OutFile "terraform_$($version.current_version)_windows_386.zip"
# $cwd = (Get-Item -Path ".\" -Verbose).FullName
# Remove-Item terraform.exe
# Unzip "$cwd\terraform_$($version.current_version)_windows_386.zip" "$cwd"


$allVersions = [System.Net.WebClient]::new().DownloadString("https://releases.hashicorp.com/index.json") | ConvertFrom-Json

foreach ($plugin in $allVersions.psobject.Properties ) {
    $requestedPlugin = $plugins | Where-Object { $_.name -eq $plugin.Name}
    #$requestedPlugin.name
    if ($requestedPlugin) {
        foreach ($requestedVersion in $requestedPlugin.versions) {
            #$requestedVersion
            #$plugin.Value.versions.psobject.Properties
            $version = $plugin.Value.versions.psobject.Properties | Where-Object { $_.Value.version -eq $requestedVersion } | Select { $_.Value}

            if ($version) {
                $version | % { $_.Value} 
                foreach ($build in $version.Value.builds) {
                    $build
                    if ($build.os -eq "windows" -and $build.arch -eq "386") {
                        $outFile = [System.IO.Path]::Combine($env:Temp, $version.filename)
                        write-host "Downloading" $version.filename 
                        Invoke-WebRequest -Uri $version.url -OutFile $outFile
                        $cwd = (Get-Item -Path ".\" -Verbose).FullName
                        Unzip $outFile "$cwd\plugins\windows_386"
                    }
                }
            } else {
                Write-Error "Could not find version $requestedVersion of ${$requestedPlugin.name}"
                exit 1    
            }
        }
        # foreach ($version in $plugin.Value.versions.psobject.Properties) {
        #     if ($mostRecent -eq $null) {
        #         $mostRecent = $version
        #     } else {
        #         ([int] $major, [int] $minor, [int] $patch) = $version.Value.version.Split('.') | % { iex $_ }
        #         ([int] $currentMajor, [int] $currentMinor, [int] $currentPatch) = $mostRecent.Value.version.Split('.') | % { iex $_ }
        #         if ($major -gt $currentMajor -or `
        #            ($major -eq $currentMajor -and $minor -gt $currentMinor) -or `
        #            ($major -eq $currentMajor -and $minor -eq $currentMinor -and $patch -gt $currentPatch)) {
        #             $mostRecent = $version
        #         } 
        #     }
        # }

        # #write-host $plugin.Name $mostRecent.Value.version
        # foreach ($build in $mostRecent.Value.builds) {
        #     if ($build.os -eq "windows" -and $build.arch -eq "386") {
        #         $outFile = [System.IO.Path]::Combine($env:Temp, $build.filename)
        #         write-host "Downloading" $build.filename 
        #         Invoke-WebRequest -Uri $build.url -OutFile $outFile
        #         $cwd = (Get-Item -Path ".\" -Verbose).FullName
        #         Unzip $outFile "$cwd\plugins\windows_386"
        #     }
        # }
    }
}
