$assemblyFile = $null;
if (Test-Path ($PSScriptRoot + "\" + "MultiMonitorBrowser.exe")) {
    $Script:assemblyFile = Get-Item ($PSScriptRoot + "\" + "MultiMonitorBrowser.exe")
}
elseif (Test-Path ($PSScriptRoot + "\bin\x64\Debug\" + "MultiMonitorBrowser.exe")) {
    $Script:assemblyFile = Get-Item ($PSScriptRoot + "\bin\x64\Debug\" + "MultiMonitorBrowser.exe")
}

[System.Reflection.Assembly]::LoadFile($assemblyFile.FullName) > $null

Write-Host -NoNewline -ForegroundColor Cyan "Input the string to encrypt: "
$plainText = Read-Host
Write-Host -NoNewline -ForegroundColor Cyan "The encrypted string is: "
Write-Host ([MultiMonitorBrowser.App]::Encrypt($plainText))
Write-Host -ForegroundColor Green "Press any key to exit..."
Read-Host
