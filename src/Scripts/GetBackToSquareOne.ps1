########################
# GetBackSquareOne.ps1 #
########################
CLS

################
# RUN ELEVATED #
################

$delete = $true;

$filename = "C:\Program Files (x86)\Chem4Word V3\Chem4Word.V3.vsto";

$exists = Test-Path -path $filename
if ($exists -eq $true)
{
    Write-Host "Chem4Word V3 is installed."
}
else
{
    Write-Host "Chem4Word V3 is NOT installed."
}

# HKEY_CURRENT_USER\Software\Chem4Word V3

$Key = "HKCU:\SOFTWARE\Chem4Word V3";
$k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue

if ($k -ne $null)
{
    Write-Host "Registry Key '$($Key)' found ..."
    Write-Host "  Last Update Check: $($k.'Last Update Check')"
    Write-Host "  Versions Behind: $($k.'Versions Behind')"

    if ($exists -eq $false -and $delete -eq $true)
    {
        Write-Host "  Deleting '$($Key)' ..." -ForegroundColor Cyan
        Remove-Item -Path $Key -Recurse
    }
}
else
{
    Write-Host "Registry Key '$($Key)' not found."
}

# HKEY_CURRENT_USER\SOFTWARE\Microsoft\Office\Word\Addins

$Key = "HKCU:\SOFTWARE\Microsoft\Office\Word\Addins\Chem4Word.V3";
$k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue

if ($k -ne $null)
{
    Write-Host "Registry Key '$($Key)' found ..."
    Write-Host "  FriendlyName: $($k.'FriendlyName')"
    Write-Host "  LoadBehavior: $($k.'LoadBehavior')"
    Write-Host "  Manifest: $($k.'Manifest')"

    if ($exists -eq $false -and $delete -eq $true)
    {
        Write-Host "  Deleting '$($Key)' ..." -ForegroundColor Cyan
        Remove-Item -Path $Key -Recurse
    }
}
else
{
    Write-Host "Registry Key '$($Key)' not found."
}

# HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Office\Word\Addins

$Key = "HKLM:\SOFTWARE\Microsoft\Office\Word\Addins\Chem4Word V3";
$k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue

if ($k -ne $null)
{
    Write-Host "Registry Key '$($Key)' found ..."
    Write-Host "  FriendlyName: $($k.'FriendlyName')"
    Write-Host "  LoadBehavior: $($k.'LoadBehavior')"
    Write-Host "  Manifest: $($k.'Manifest')"

    if ($exists -eq $false -and $delete -eq $true)
    {
        Write-Host "  Deleting '$($Key)' ..." -ForegroundColor Cyan
        Remove-Item -Path $Key -Recurse
    }
}
else
{
    Write-Host "Registry Key '$($Key)' not found."
}

# HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Office\Word\Addins

$Key = "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Office\Word\Addins\Chem4Word V3";
$k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue

if ($k -ne $null)
{
    Write-Host "Registry Key '$($Key)' found ..."
    Write-Host "  FriendlyName: $($k.'FriendlyName')"
    Write-Host "  LoadBehavior: $($k.'LoadBehavior')"
    Write-Host "  Manifest: $($k.'Manifest')"

    if ($exists -eq $false -and $delete -eq $true)
    {
        Write-Host "  Deleting '$($Key)' ..." -ForegroundColor Cyan
        Remove-Item -Path $Key -Recurse
    }
}
else
{
    Write-Host "Registry Key '$($Key)' not found."
}

# HKEY_CURRENT_USER\SOFTWARE\Microsoft\Office\14.0\Word\Resiliency\DisabledItems
# HKEY_CURRENT_USER\SOFTWARE\Microsoft\Office\15.0\Word\Resiliency\DisabledItems
# HKEY_CURRENT_USER\SOFTWARE\Microsoft\Office\16.0\Word\Resiliency\DisabledItems

for ($i=14; $i -le 16; $i++)
{
    $key = "HKCU:\SOFTWARE\Microsoft\Office\$($i).0\Word\Resiliency\DisabledItems";
    $k = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
    if ($k -ne $null)
    {
        Write-Host "Clearing $($Key)"
        foreach ($kvp in $k.PSObject.Properties)
        {
            if (!$kvp.Name.StartsWith("PS"))
            {
                if ($exists -eq $false -and $delete -eq $true)
                {
                    Write-Host "  Removing $($kvp.Name)" -ForegroundColor Cyan
                    Remove-ItemProperty -Path $Key -Name $kvp.Name
                }
            }
        }
    }
}

## Folders
## $env:ProgramData\Chem4Word.V3
## $env:USERPROFILE\AppData\Local\Chem4Word.V3
## $env:USERPROFILE\AppData\Local\assembly\dl3

if ($exists -eq $false -and $delete -eq $true)
{
    $folder = "$($env:ProgramData)\Chem4Word.V3"
    if (Test-Path $folder)
    {
        Write-Host "Deleting folder tree '$($folder)'"
        Get-ChildItem -Path $folder -Recurse | Remove-Item -force -recurse
        Remove-Item $folder
    }

    $folder = "$($env:USERPROFILE)\AppData\Local\Chem4Word.V3"
    if (Test-Path $folder)
    {
        Write-Host "Deleting folder tree '$($folder)'"
        Get-ChildItem -Path $folder -Recurse | Remove-Item -force -recurse
        Remove-Item $folder
    }

    $folder = "$($env:USERPROFILE)\AppData\Local\assembly\dl3"
    if (Test-Path $folder)
    {
        Write-Host "Deleting children of '$($folder)'"
        Get-ChildItem -Path $folder -Recurse | Remove-Item -force -recurse
    }
}
