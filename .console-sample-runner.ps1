param(
	[int]$TimeoutSeconds = 30,
	[int]$ThrottleLimit = 12
)

$ErrorActionPreference = 'Stop'
$root = Get-Location
$timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss K'
$projects = Get-ChildItem -Recurse -Filter '*.csproj' | Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' } | ForEach-Object {
	[xml]$projectXml = Get-Content $_.FullName
	$outputType = @($projectXml.Project.PropertyGroup | ForEach-Object { $_.OutputType } | Where-Object { $_ }) | Select-Object -First 1
	if ($outputType -eq 'Exe') { $_ }
} | Sort-Object FullName

$input = "Hello`nWhat is the weather today?`nexit`nquit`n"
$results = $projects | ForEach-Object -Parallel {
	$project = $_
	$relativeProject = $project.FullName.Substring($using:root.Path.Length + 1)
	$arguments = "run --project `"$($project.FullName)`" --no-restore"
	$processInfo = [System.Diagnostics.ProcessStartInfo]::new('dotnet', $arguments)
	$processInfo.WorkingDirectory = $using:root.Path
	$processInfo.RedirectStandardInput = $true
	$processInfo.RedirectStandardOutput = $true
	$processInfo.RedirectStandardError = $true
	$processInfo.UseShellExecute = $false
	$processInfo.CreateNoWindow = $true
	$process = [System.Diagnostics.Process]::new()
	$process.StartInfo = $processInfo
	$startedAt = Get-Date
	$null = $process.Start()
	$process.StandardInput.Write($using:input)
	$process.StandardInput.Close()
	$standardOutput = $process.StandardOutput.ReadToEndAsync()
	$standardError = $process.StandardError.ReadToEndAsync()
	$completed = $process.WaitForExit($using:TimeoutSeconds * 1000)
	if (-not $completed) {
		$process.Kill($true)
		$process.WaitForExit()
	}
	$duration = [math]::Round(((Get-Date) - $startedAt).TotalSeconds, 1)
	$output = ($standardOutput.GetAwaiter().GetResult() + $standardError.GetAwaiter().GetResult()).Trim()
	$tail = if ($output.Length -gt 3000) { $output.Substring($output.Length - 3000) } else { $output }
	$status = if (-not $completed) { 'TimedOut' } elseif ($process.ExitCode -eq 0) { 'Passed' } else { 'Failed' }
	[pscustomobject]@{
		Project = $relativeProject
		Status = $status
		ExitCode = if ($completed) { $process.ExitCode } else { $null }
		DurationSeconds = $duration
		Output = $tail
	}
} -ThrottleLimit $ThrottleLimit

$results | Export-Csv -NoTypeInformation -Path (Join-Path $root 'console-sample-run-results.csv')
$summary = $results | Group-Object Status | Sort-Object Name
$markdown = @(
	'# Console Sample Run Report',
	'',
	"Generated: $timestamp",
	"Timeout per project: $TimeoutSeconds seconds",
	"Projects run: $($results.Count)",
	'',
	'## Summary',
	'',
	'| Status | Count |',
	'| --- | ---: |'
)
$markdown += $summary | ForEach-Object { "| $($_.Name) | $($_.Count) |" }
$markdown += '', '## Per-project results', '', '| Project | Status | Exit code | Duration (s) | Output tail |', '| --- | --- | ---: | ---: | --- |'
$markdown += $results | ForEach-Object {
	$output = $_.Output -replace '\|', '\|' -replace "`r?`n", '<br>'
	"| $($_.Project) | $($_.Status) | $($_.ExitCode) | $($_.DurationSeconds) | $output |"
}
$markdown | Set-Content -Path (Join-Path $root 'console-sample-run-report.md') -Encoding utf8
Write-Host "Completed $($results.Count) projects. Report: console-sample-run-report.md"
