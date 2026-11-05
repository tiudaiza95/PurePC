$ErrorActionPreference = 'Stop'
$source = $PSScriptRoot
$destination = Split-Path $source -Parent
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$references = @('System.Windows.Forms.dll','System.Drawing.dll','System.Management.dll','System.Xaml.dll','Microsoft.CSharp.dll',(Join-Path $framework 'WPF\WindowsBase.dll'), (Join-Path $framework 'Microsoft.VisualBasic.dll'), (Join-Path $framework 'WPF\PresentationFramework.dll'), (Join-Path $framework 'WPF\PresentationCore.dll'))
$arguments = @('/nologo','/target:winexe','/platform:x64',('/out:' + (Join-Path $destination 'PurePC.exe')),('/win32icon:' + (Join-Path $destination 'DonDepPC.ico')),('/win32manifest:' + (Join-Path $source 'app.manifest')),('/resource:' + (Join-Path $source 'MainWindow.xaml') + ',SachGon.MainWindow.xaml'),('/resource:' + (Join-Path $destination 'DonDepPC.png') + ',SachGon.Icon.png'))
foreach ($reference in $references) { $arguments += '/reference:' + $reference }
$arguments += @(Get-ChildItem -LiteralPath $source -Filter '*.cs' | ForEach-Object FullName)
& (Join-Path $framework 'csc.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
Copy-Item (Join-Path $destination 'PurePC.exe') (Join-Path $destination 'DonDepPC.exe') -Force


& (Join-Path $framework "csc.exe") /nologo /target:exe /platform:x64 ("/out:" + (Join-Path $source "Fixtures\ProcessFixture.exe")) (Join-Path $source "Fixtures\ProcessFixture.cs")
if ($LASTEXITCODE -ne 0) { throw "Fixture compilation failed." }
