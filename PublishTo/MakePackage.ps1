Param(
	[ValidateSet("All", "Clean")]$Target = "All"
)

# error to break
trap { break }

$product = 'RealtimeSearch'
$assembly = 'NeeLaboratory.RealtimeSearch'

$config = 'Release'


#---------------------
# get fileversion
function Get-FileVersion($fileName)
{
	$versionInfo =  [System.Diagnostics.FileVersionInfo]::GetVersionInfo($fileName)
	$major = $versionInfo.FileMajorPart
	$minor = $versionInfo.FileMinorPart

	"$major.$minor"
}


#--------------------
# replace keyword
function Replace-Content
{
	Param([string]$filepath, [string]$rep1, [string]$rep2)
	if ( $(Test-Path $filepath) -ne $True )
	{
		Write-Error "file not found"
		return
	}
	# input UTF8, output UTF8
	$file_contents = $(Get-Content -Encoding UTF8 $filepath) -replace $rep1, $rep2
	$file_contents | Out-File -Encoding UTF8 $filepath
}


#--------------------
if ($Target -eq "Clean")
{
	Remove-Item "$product*" -Recurse -Force

	return
}



#-----------------------
# variables
$solutionDir = ".."
$solution = "$solutionDir\$product.sln"
$projectDir = "$solutionDir\$assembly"
$productDir = "$projectDir\bin\$config"

#----------------------
# build
$msbuild = 'C:\Program Files (x86)\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe'
& $msbuild $solution /p:Configuration=$config /t:Clean,Build
if ($? -ne $true)
{
	throw "build error"
}

# get assembly version
$version = Get-FileVersion "$productDir\$product.exe"

# auto increment build version
$xml = [xml](Get-Content "BuildCount.xml")
$buildCount = [int]$xml.build + 1

$buildVersion = (Get-FileVersion "$productDir\$product.exe") + ".$buildCount"

$packageDir = $product + $version
$packageZip = $packageDir + ".zip"
$packageMsi = $packageDir + ".msi"

# remove packageDir
if (Test-Path $packageDir)
{
	Remove-Item $packageDir -Recurse -Force
}
if (Test-Path $packageZip)
{
	Remove-Item $packageZip
}
if (Test-Path $packageMsi)
{
	Remove-Item $packageMsi
}

Start-Sleep -m 100


#----------------------
# package section
function New-Package
{
	# make package folder
	$temp = New-Item $packageDir -ItemType Directory

	# copy
	Copy-Item "$productDir\$product.exe" $packageDir
	Copy-Item "$productDir\$product.exe.config" $packageDir
	Copy-Item "$productDir\*.dll" $packageDir


	#------------------------
	# generate README.html

	$readmeDir = $packageDir + "\readme"
	$temp = New-Item $readmeDir -ItemType Directory 

	Copy-Item "$solutionDir\*.md" $readmeDir
	#Copy-Item "$solutionDir\Style.html" $readmeDir
	Copy-Item "$solutionDir\NeeLaboratory.IO.Search\THIRDPARTY_LICENSES.md" "$readmeDir\NeeLaboratory.IO.Search_THIRDPARTY_LICENSES.md"
	

	# edit README.md
	Replace-Content "$readmeDir\README.md" "# $product" "# $product $version"

	# markdown to html by pandoc
	& pandoc -s -t html5 -o "$packageDir\README.html" -H Style.html "$readmeDir\README.md" "$readmeDir\LICENSE.md" "$readmeDir\THIRDPARTY_LICENSES.md" "$readmeDir\NeeLaboratory.IO.Search_THIRDPARTY_LICENSES.md"
	if ($? -ne $true)
	{
		throw "pandoc error" 
	}

	Remove-Item $readmeDir -Recurse

	return $temp
}

#--------------------------
# archive to ZIP
function New-Zip
{
	Compress-Archive $packageDir -DestinationPath $packageZip
}


#--------------------------
# WiX
function New-Msi
{
	$candle = 'C:\Program Files (x86)\WiX Toolset v3.11\bin\candle.exe'
	$light = 'C:\Program Files (x86)\WiX Toolset v3.11\bin\light.exe'
	$heat = 'C:\Program Files (x86)\WiX Toolset v3.11\bin\heat.exe'


	$config = "$product.exe.config"
	$packageAppendDir = $packageDir + ".append"

	# remove append folder
	if (Test-Path $packageAppendDir)
	{
		Remove-Item $packageAppendDir -Recurse
		Start-Sleep -m 100
	}

	# make append folder
	$temp = New-Item $packageAppendDir -ItemType Directory

	# make config for installer
	[xml]$xml = Get-Content "$packageDir\$config"

	$add = $xml.configuration.appSettings.add | Where { $_.key -eq 'PackageType' } | Select -First 1
	$add.value = '.msi'

	$add = $xml.configuration.appSettings.add | Where { $_.key -eq 'UseLocalApplicationData' } | Select -First 1
	$add.value = 'True'

	$utf8WithoutBom = New-Object System.Text.UTF8Encoding($false)
	$sw = New-Object System.IO.StreamWriter("$packageAppendDir\$config", $false, $utf8WithoutBom)
	$xml.Save( $sw )
	$sw.Close()

	# make DllComponents.wxs
	#& $heat dir "$packageDir\Libraries" -cg DllComponents -ag -pog:Binaries -sfrag -var var.LibrariesDir -dr INSTALLFOLDER -out WixSource\DllComponents.wxs
	#if ($? -ne $true)
	#{
	#	throw "heat error"
	#}

	#-------------------------
	# WiX
	#-------------------------

	$ErrorActionPreference = "stop"

	& $candle -d"BuildVersion=$buildVersion" -d"ProductVersion=$version"  -d"ContentDir=$packageDir\\" -d"AppendDir=$packageDir.append\\" -ext WixNetFxExtension -out "$packageDir.append\\"  WixSource\*.wxs
	if ($? -ne $true)
	{
		throw "candle error"
	}

	& $light -out "$packageMsi" -ext WixUIExtension -ext WixNetFxExtension -cultures:ja-JP "$packageDir.append\*.wixobj"
	if ($? -ne $true)
	{
		throw "light error" 
	}
}

#--------------------------
# main

Write-Host "`n[Package] ...`n" -fore Cyan
New-Package

if ($Target -eq "All")
{
	Write-Host "`[Zip] ...`n" -fore Cyan
	New-Zip
	Write-Host "`nExport $packageZip successed.`n" -fore Green
}

if ($Target -eq "All") 
{
	Write-Host "`[Installer] ...`n" -fore Cyan
	New-Msi
	Write-Host "`nExport $packageMsi successed.`n" -fore Green
}


# current
if (-not (Test-Path $product))
{
	New-Item $product -ItemType Directory
}
Copy-Item "$packageDir\*" "$product\" -Recurse -Force


#--------------------------
# saev buid version
$xml.build = [string]$buildCount
$xml.Save("BuildCount.xml")

#-------------------------
# Finish.
Write-Host "`nAll done.`n" -fore Green





