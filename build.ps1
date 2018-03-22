Add-Type -AssemblyName System.IO.Compression.FileSystem
function Unzip
{
    param([string]$zipfile, [string]$outpath)
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zipfile, $outpath)
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$version = Invoke-RestMethod -Uri "https://checkpoint-api.hashicorp.com/v1/check/terraform"
Invoke-WebRequest -Uri "https://releases.hashicorp.com/terraform/$($version.current_version)/terraform_$($version.current_version)_windows_386.zip" -OutFile "terraform_$($version.current_version)_windows_386.zip"
$cwd = (Get-Item -Path ".\" -Verbose).FullName
Remove-Item terraform.exe
Unzip "$cwd\terraform_$($version.current_version)_windows_386.zip" "$cwd"

(Get-Content terraform.nuspec).Replace('#{TerraformVersion}', $version.current_version) | Set-Content terraform-processed.nuspec