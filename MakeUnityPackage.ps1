$unity_cli = "Unity.exe"
$project_path = Get-Location
$package_name = ".\AOSetter.unitypackage"
$export_packages = "Assets\‚â‚­‚à‚ñ‚à‚ñƒVƒ‡ƒbƒv\AOSetter\Editor\Tool"
$log_file = "unity_export_package.log"

$packages = $export_packages -join " "
$argument_list = "-exportPackage " + $packages + " " + $package_name + " -projectPath " + $project_path  + " -batchmode " + "-nographics " + "-logfile " + $log_file + " -quit"

Write-Host "Start exporting"
$proc = Start-Process -FilePath $unity_cli -ArgumentList $argument_list -Wait -PassThru
Write-Host "End exporting [Exit Code: " $proc.ExitCode "]"