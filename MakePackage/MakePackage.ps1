Param(
	[ValidateSet("All", "Clean")]$Target = "All"
)

# error to break
$ErrorActionPreference = "Stop"
trap { break }

# sync .NET current directory
[System.IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider FileSystem).Path)

#-----------------------
# variables
$product = 'RealtimeSearch'
$assembly = 'RealtimeSearch'
$solutionDir = Resolve-Path ".."
$projectDir = "$solutionDir\$assembly"
$project = "$projectDir\$product.csproj"
$publishDir = "Publish"
$productDir = "$publishDir\$product-x64"


#---------------------
# get fileversion
function Get-FileVersion($fileName)
{
	if (-Not (Test-Path $filename))
	{
		Write-Error "File not found: $filename"
		return
	}

	$fullpath = Convert-Path $filename
	
	$versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($fullpath)
	$major = $versionInfo.FileMajorPart
	$minor = $versionInfo.FileMinorPart

	"$major.$minor"
}


#--------------------
# replace keyword
function Replace-Content
{
	Param([string]$filepath, [string]$rep1, [string]$rep2)
	if (-Not (Test-Path $filepath))
	{
		Write-Error "file not found: $filename"
		return
	}

	# input UTF8, output UTF8
	$file_contents = $(Get-Content -Encoding UTF8 $filepath) -replace $rep1, $rep2
	$file_contents | Out-File -Encoding UTF8 $filepath
}


#----------------------
# clear
function Remove-Files
{
	$files = $args
	foreach ($file in $files.Where({-Not [string]::IsNullOrEmpty($_)}))
	{
		Write-Host "Remove $file" -fore DarkYellow

		if (Test-Path $file)
		{
			Remove-Item $file -Recurse -Force
		}
	}

	Start-Sleep -m 100
}

#-----------------------
# build
function Publish-Product($project, $output)
{
	#& dotnet publish $project -c Release -r win-x64 -p:PublishReadyToRun=true --no-self-contained -o publish/x64
	#& dotnet publish $project -c Release -r win-x86 -p:PublishReadyToRun=true --no-self-contained -o publish/x86
	#& dotnet publish $project -c Release -o $output
	& dotnet publish $project -p:PublishProfile=FolderProfile-x64.pubxml -c Release
}


#----------------------
# package section
function New-Package
{
	# make package folder
	$temp = New-Item $packageDir -ItemType Directory

	# copy
	Copy-Item "$productDir\$product.exe" $packageDir
	Copy-Item "$productDir\$product.runtimeconfig.json" $packageDir
	Copy-Item "$productDir\$product.deps.json" $packageDir
	Copy-Item "$productDir\*.dll" $packageDir
	Copy-Item "$productDir\*.dll.config" $packageDir


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


	$config = "$product.dll.config"
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

	# icons
	Copy-Item "$projectDir\App.ico" $packageAppendDir

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

# clean product
#Remove-Files $productDir
if (Test-Path $publishDir)
{
	Remove-Item $publishDir -Recurse
}

# build
Write-Host "`n[Build] ...`n" -fore Cyan
Publish-Product $project $productDir

# get assembly version
$version = Get-FileVersion "$productDir\$product.exe"

# auto increment build version
$xml = [xml](Get-Content "BuildCount.xml")
$buildCount = [int]$xml.build + 1

$buildVersion = (Get-FileVersion "$productDir\$product.exe") + ".$buildCount"

$packageDir = $product + $version
$packageZip = $packageDir + ".zip"
$packageMsi = $packageDir + ".msi"
$wixpdb = $packageDir + ".wixpdb"

# Clean
Remove-Files $packageDir $packageZip $packageMsi $wixpdb

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





