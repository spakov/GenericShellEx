param (
  [string]$PackageName = "GenericShellEx",
  [string]$DllRelativePath = "GenericShellEx.dll",
  [switch]$Unregister
)

$Name = "Generic Shell Extensions ({0})"

$String = "String"
$Default = "(default)"
$InprocServer32 = "InprocServer32"
$GenericShellEx = "GenericShellEx"
$ThreadingModel = "ThreadingModel"
$Apartment = "Apartment"

$HKCR = "Registry::HKCR"
$HKCRClsid = "$HKCR\CLSID"
$HKCRClsidClsid = "$HKCR\CLSID\{0}"
$HKCRInprocServer32 = "$HKCRClsidClsid\$InprocServer32"
$HKCRContextMenuHandlers = "$HKCR\{0}\shellex\ContextMenuHandlers"
$HKCRGenericShellEx = "$HKCRContextMenuHandlers\$GenericShellEx"

$Handlers = @(
  @{
    Type = "*"
    Clsid = "{ff8b806e-83c6-4df1-9fb4-698133580803}"
  },
  @{
    Type = "Directory"
    Clsid = "{aeb1215c-84ff-43cc-aec7-e02c2b56e74c}"
  },
  @{
    Type = "Directory\Background"
    Clsid = "{92fd673f-d257-4ac8-8731-c7cde82fa49e}"
  }
)

function Get-DllPath {
  $package = Get-AppxPackage -Name $PackageName

  if (-Not $package) {
    Write-Error "Package '$PackageName' not found."
    exit 1
  }

  return (Join-Path $package.InstallLocation $DllRelativePath)
}

function New-Key {
  param (
    [string]$Path,
    [string]$Name
  )

  $Path = [wildcardpattern]::Escape($Path)
  $Name = [wildcardpattern]::Escape($Name)

  if (-Not (Test-Path -Path $(Join-Path -Path $Path -ChildPath $Name))) {
    New-Item -Path $Path -Name $Name
  }
}

function Set-RegSz {
  param (
    [string]$Path,
    [string]$Name,
    [string]$Value
  )

  $Path = [wildcardpattern]::Escape($Path)

  if (-Not (Get-ItemProperty -Path $Path -Name $Name)) {
    New-ItemProperty -Path $Path -Name $Name -PropertyType $String -Value $Value
  } else {
    if ((Get-ItemProperty -Path $Path -Name $Name).$Name -IsNot [string]) {
      Remove-ItemProperty -Path $Path -Name $Name
      New-ItemProperty -Path $Path -Name $Name -PropertyType $String -Value $Value
    }
  }
}

function Remove-Key {
  param(
    [string]$Path
  )

  $Path = [wildcardpattern]::Escape($Path)

  if (Test-Path -Path $Path | Out-Null) {
    Remove-Item -Path $Path -Recurse
  }
}

function Register-Handler {
  param(
    [System.Collections.Hashtable]$Handler,
    [string]$DllPath
  )

  Write-Host "Registering CLSID $($Handler.Clsid) => $($Name -f $Handler.Type)"

  $hkcrClsidClsid = $($HKCRClsidClsid -f $Handler.Clsid)

  New-Key -Path $HKCRClsid -Name $($Handler.Clsid)
  Set-RegSz -Path $hkcrClsidClsid -Name $Default -Value $($Name -f $Handler.Type)

  $hkcrInprocServer32 = $($HKCRInprocServer32 -f $Handler.Clsid)

  New-Key -Path $hkcrClsidClsid -Name $InprocServer32
  Set-RegSz -Path $hkcrInprocServer32 -Name $Default -Value $DllPath
  Set-RegSz -Path $hkcrInprocServer32 -Name $ThreadingModel -Value $Apartment

  $hkcrContextMenuHandlers = $($HKCRContextMenuHandlers -f $Handler.Type)
  $hkcrGenericShellEx = $($HKCRGenericShellEx -f $Handler.Type)

  New-Key -Path $hkcrContextMenuHandlers -Name $GenericShellEx
  Set-RegSz -Path $hkcrGenericShellEx -Name $Default -Value $($Handler.Clsid)
}

function Unregister-Handler {
  param(
    [System.Collections.Hashtable]$Handler
  )

  Write-Host "Unregistering CLSID $($Handler.Clsid) => $($Name -f $Handler.Type)"

  $hkcrClsidClsid = $($HKCRClsidClsid -f $Handler.Clsid)
  $hkcrGenericShellEx = $($HKCRGenericShellEx -f $Handler.Type)

  Remove-Key -Path $hkcrClsidClsid
  Remove-Key -Path $hkcrGenericShellEx
}

# Main

if (-Not ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
  $Command = Join-Path -Path $(Get-Location) -ChildPath $($MyInvocation.MyCommand)
  $Parameters = ""

  foreach ($parameter in $MyInvocation.BoundParameters.Keys) {
    $Parameters = "$Parameters -$parameter $($MyInvocation.BoundParameters.$parameter)"
  }

  Start-Process "$PSHOME/pwsh" -ArgumentList ("-NoProfile -ExecutionPolicy Bypass -Command `"{0}`"{1}" -f $Command, $Parameters) -Verb RunAs
  exit
}

if ($Unregister) {
  foreach ($handler in $Handlers) {
    Unregister-Handler -Handler $handler
  }

  Write-Host "All handlers unregistered."
} else {
  $dllPath = Get-DllPath

  if (-Not (Test-Path $dllPath)) {
    Write-Error "DLL not found: $dllPath"

    exit 1
  }

  foreach ($handler in $Handlers) {
    Register-Handler -Handler $handler -DllPath $dllPath
  }

  Write-Host "All handlers registered."
}
