function bootstrap {
    param
    (
        [string[]] $Names
    )

    $installed = $installed = Get-Module -ListAvailable -Name $names | Select-Object -ExpandProperty Name
    $missing = @($names | Where-Object { $_ -NotIn $installed })

    if ($missing.Length -gt 0) {
        $CurrentPolicy = (Get-PSRepository PSGallery).InstallationPolicy

        if ($CurrentPolicy -ne "Trusted") {
            Write-Verbose "Setting PSGallery to Trusted"
            Set-PSRepository PSGallery -InstallationPolicy Trusted
        }

        try {
            $Preferences = @{
                Verbose            = $VerbosePreference -eq "Continue"
                Confirm            = $false
                Scope              = "CurrentUser"
                Repository         = "PSGallery"
                SkipPublisherCheck = $true
                AllowClobber       = $true
                MinimumVersion     = "4.7"
                Force              = $true
            }

            if ($PSEdition -eq "Core") { $Preferences.AcceptLicense = $true }

            Install-Module -Name $missing @Preferences
        }
        finally {
            if ($CurrentPolicy -ne "Trusted") {
                Write-Verbose "Setting PSGallery back to $CurrentPolicy"
                Set-PSRepository PSGallery -InstallationPolicy $CurrentPolicy
            }
        }
    }
}

<#

Copyright (c) Attila Kiskó, enyim.com

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

  http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

#>
