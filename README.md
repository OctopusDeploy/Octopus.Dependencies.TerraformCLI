This package provides the Terraform executable and cached plugins as a Calamari package.

The Terraform executable is updated by running `build.ps1`

Terraform plugins can be saved in the `plugins` directory. These plugins will be used instead of the downloads from the Terraform 
website if they match the versions being requested.

Plugins are updated by downloading them from https://releases.hashicorp.com and overwriting the executables in the `plugins/[platform]` directory. 