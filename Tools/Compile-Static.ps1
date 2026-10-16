param([string]$EditorRoot = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Set-Location $projectRoot
$unityData = Join-Path $EditorRoot 'Data'
$assemblies = Join-Path $unityData 'Resources\PackageManager\ProjectTemplates\libcache\com.unity.template.urp-blank-17.2.1\ScriptAssemblies'
$compiler = Join-Path $unityData 'DotNetSdk\sdk\8.0.318\Roslyn\bincore\csc.dll'
$runtime = Join-Path $unityData 'DotNetSdk\dotnet.exe'
$outputDir = Join-Path $projectRoot 'Logs\StaticCompile'
New-Item -ItemType Directory -Force $outputDir | Out-Null
$references = @()
$references += Get-ChildItem (Join-Path $unityData 'NetStandard\ref\2.1.0') -Filter *.dll
$references += Get-Item (Join-Path $unityData 'NetStandard\compat\2.1.0\shims\netfx\mscorlib.dll')
$references += Get-ChildItem (Join-Path $unityData 'Managed\UnityEngine') -Filter *.dll
$references += Get-ChildItem $assemblies -Filter *.dll | Where-Object { $_.Name -ne 'Assembly-CSharp.dll' -and $_.Name -ne 'Assembly-CSharp-Editor.dll' }
$references += Get-Item (Join-Path $unityData 'Resources\PackageManager\BuiltInPackages\com.unity.ext.nunit\net472\unity-custom\nunit.framework.dll')
$referenceArgs = $references | ForEach-Object { '/reference:"' + $_.FullName + '"' }
foreach ($part in @('Runtime','Editor','Tests')) {
    $argsList = @('/nologo','/target:library','/langversion:9','/nostdlib+','/define:UNITY_EDITOR,UNITY_6000_6_OR_NEWER,ENABLE_INPUT_SYSTEM,UNITY_INCLUDE_TESTS')
    $argsList += '/out:"' + (Join-Path $outputDir "OrbitDominion.$part.dll") + '"'
    $argsList += $referenceArgs
    if ($part -ne 'Runtime') { $argsList += '/reference:"' + (Join-Path $outputDir 'OrbitDominion.Runtime.dll') + '"' }
    $argsList += Get-ChildItem "Assets\OrbitDominion\$part" -Filter *.cs -Recurse | ForEach-Object { '"' + $_.FullName + '"' }
    $response = Join-Path $outputDir "$part.rsp"
    $argsList | Set-Content $response
    & $runtime $compiler "@$response"
    if ($LASTEXITCODE -ne 0) { throw "$part static compilation failed." }
    Write-Output "$part C# static compilation passed (not a Unity import/build/test run)."
}
