This package provides the Terraform executable and cached plugins as a Calamari package.

The Terraform executable is updated by overwriting the `terraform.exe` file.

Terraform plugins can be saved in the `plugins` directory. These plugins will be used instead of the downloads from the Terraform 
website if they match the versions being requested.

How to update this project:

1. Download the 32 bit version of the Terraform exe from https://www.terraform.io/downloads.html and overwrite terraform.exe.
2. Download plugins from https://releases.hashicorp.com and overwrite the executables in the plugins/windows_386 directory.
3. The terraform.nuspec file version x.y.z matches the Terraform version. The final component of the terraform.nuspec version (the release version) can be incremented to reflect internal releases. 