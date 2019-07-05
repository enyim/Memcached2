Properties {
    [string] $Version = $null
    [string] $Prerelease = $null
    [string] $Branch = $null
    [string] $Configuration = "Release"
    [string] $CustomMetadata = $null
    [string] $OutputPath = "out"
}

function ??($cond, $t, $f) { if (!!$cond) { $t } else { $f } }
function Get-CommonMSBuildArgs {
    @(
        "--verbosity", $(?? ($VerbosePreference -eq "Continue") "n" "m" ),
        "-nologo",
        "--configuration", $Configuration
    )
}

Task default -Depends Build

Task Clean {
    Write-Host "Removing bin,obj directories"
    Get-ChildItem -Recurse bin, obj -Directory | Remove-Item -Recurse -Force

    if (Test-Path $OutputPath) {
        Write-Host "Cleaning up $OutputPath"
        Remove-Item $OutputPath\* -Recurse -Force
    }
}

function dobuild {
    param(
        [switch] $pack = $false
    )
    function concat ($separator, $list) { ($list | Where-Object { !!$_ }) -join $separator }

    function Get-CIValue($names) {
        $names `
      | ForEach-Object { Get-ChildItem "env:$_" -ErrorAction SilentlyContinue } `
      | Select-Object -First 1 -ExpandProperty value
    }

    # set up build metadata
    #
    if (!$Version) { $Version = Get-CIValue "BUILD_NUMBER", "APPVEYOR_BUILD_VERSION" }
    if (!$Version) { $Version = Get-Content VERSION }
    if (!$Branch) { $Branch = Get-CIValue "APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH", "APPVEYOR_REPO_BRANCH" }

    $LongHash = Get-CIValue "STANDARD_CI_SOURCE_REVISION_ID", "BUILD_VCS_NUMBER", "APPVEYOR_PULL_REQUEST_HEAD_COMMIT", "APPVEYOR_REPO_COMMIT"

    if (!$LongHash -and (Get-Command "git")) {
        $ShortHash = (git log --pretty=format:%h -1)
        $LongHash = (git log --pretty=format:%H -1)
        if (!$Branch) { $Branch = (git rev-parse --abbrev-ref HEAD) -replace "/", "-" }
    }

    if (!$ShortHash) { $ShortHash = $LongHash }

    $FullVersion = concat "+" (concat "-" $Version, $Prerelease), (concat "." $Branch, $ShortHash, $CustomMetadata)

    # produce nuget packages
    Exec {
        dotnet $( ?? $pack ("pack", "--no-build", "--output", $OutputPath) "build" ) Enyim.Caching.Memcached.sln $(Get-CommonMSBuildArgs) /p:version=$FullVersion /p:ContinuousIntegrationBuild=true /p:STANDARD_CI_SOURCE_REVISION_ID=$LongHash
    }
}

Task Build -Depends Clean {
    dobuild
}

Task Pack -Depends Build {
    dobuild -pack
}

Task Test -Depends Build {
    Exec {
        dotnet test Enyim.Caching.Memcached.sln $(Get-CommonMSBuildArgs)
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
