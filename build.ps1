param ( [string] $Version, [string] $Prerelease, [string] $Branch, [string] $Configuration = "Release", [string] $CustomMetadata, [string] $OutputPath = "out" )

function concat ($separator, $list) { ($list | Where-Object { !!$_}) -join $separator }

function Get-CIValue($names) {
  $names `
  | ForEach-Object { Get-ChildItem "env:$_" -ErrorAction SilentlyContinue } `
  | Select-Object -First 1 -ExpandProperty value
}

$root = split-path -parent $MyInvocation.MyCommand.Path
Push-Location $root

try {

    # cleanup
    #
    Get-ChildItem -Recurse bin -Directory | Remove-Item -recurse -ErrorAction SilentlyContinue
    Get-ChildItem -Recurse obj -Directory | Remove-Item -recurse -ErrorAction SilentlyContinue

    $OutputPath = Join-Path $root $OutputPath
    if (Test-Path $OutputPath) { Remove-Item $OutputPath -Recurse -ErrorAction SilentlyContinue }

    # set up build metadata
    #
    if (!$Version) { $Version = Get-CIValue "BUILD_NUMBER", "APPVEYOR_BUILD_VERSION" }
    if (!$Version) { $Version = get-content VERSION }
    if (!$Branch) { $Branch = Get-CIValue "APPVEYOR_PULL_REQUEST_HEAD_REPO_BRANCH", "APPVEYOR_REPO_BRANCH" }

    $LongHash = Get-CIValue "STANDARD_CI_SOURCE_REVISION_ID", "BUILD_VCS_NUMBER", "APPVEYOR_PULL_REQUEST_HEAD_COMMIT", "APPVEYOR_REPO_COMMIT"

    if (!$LongHash -and (get-command "git")) {
      $ShortHash = (git log --pretty=format:%h -1)
      $LongHash = (git log --pretty=format:%H -1)
      if (!$Branch) { $Branch = (git rev-parse --abbrev-ref HEAD) -replace "/", "-" }
    }
    
    if (!$ShortHash) { $ShortHash = $LongHash }

    $FullVersion = concat "+" (concat "-" $Version, $Prerelease), (concat "." $Branch, $ShortHash, $CustomMetadata)

    # produce nuget packages
    write-host "dotnet pack Enyim.Memcached.sln -c $Configuration -v m /p:version=$FullVersion /p:ContinuousIntegrationBuild=true -o $OutputPath" /p:STANDARD_CI_SOURCE_REVISION_ID=$LongHash

}
finally {
    Pop-Location
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
