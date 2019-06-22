[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $false)]
    [string[]]$Tasks = @(),

    [Parameter(Position = 1, Mandatory = $false)]
    [System.Collections.Hashtable]$Parameters = @{ },

    [Parameter(Position = 2, Mandatory = $false)]
    [System.Collections.Hashtable]$Properties = @{ },

    [switch]$Help = $false
)

Push-Location $PSScriptRoot -StackName build

try {
    
    Import-Module ./build/buildhelpers.psm1
    bootstrap "psake"

    Import-Module psake -Verbose:$false -ErrorAction Stop

    $Preferences = @{
        Verbose        = $VerbosePreference -eq "Continue"
        Debug          = $DebugPreference -eq "Continue"
        nologo         = $true
        docs           = $Help
        parameters     = $Parameters
        properties     = $Properties
        taskList       = $Tasks | ForEach-Object { (Get-Culture).TextInfo.ToTitleCase($_) }
        initialization = { Set-Location $PSScriptRoot }
    }

    Invoke-psake -buildFile ./build/buildtasks.ps1 @Preferences
}
finally {
    Remove-Module buildhelpers -Force -ErrorAction SilentlyContinue
    Remove-Module psake -Force -ErrorAction SilentlyContinue

    Pop-Location -StackName build
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
