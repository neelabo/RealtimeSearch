# パッケージ生成スクリプト
#
# 使用ツール：
#   - Wix Toolset
#   - pandoc

Param(
	[ValidateSet("All", "Zip", "Installer", "Appx", "Canary", "Beta", "Dev")]$Target = "All",

	# ビルドをスキップする
	[switch]$continue,

	# ログ出力のあるパッケージ作成
	[switch]$trace,

	# ターゲットx86
	[switch]$x86,

	# MSI作成時にMainComponents.wsxを更新する
	[switch]$updateComponent,

	# Postfix. Canary や Beta での番号重複回避用
	[string]$versionPostfix = ""
)

# error to break
trap { break }

$ErrorActionPreference = "stop"
	
Write-Host "[Properties] ..." -fore Cyan
Write-Host "Target: $Target"
Write-Host "Continue: $continue"
Write-Host "Trace: $trace"
Write-Host "X86: $x86"
Write-Host "UpdateComponent: $updateComponent" 
Write-Host "VersionPostfix: $versionPostfix" 
Write-Host
Read-Host "Press Enter to continue"

#
$product = 'RealtimeSearch'
$Win10SDK = "C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64"

# sync current directory
[System.IO.Directory]::SetCurrentDirectory((Get-Location -PSProvider FileSystem).Path)

#---------------------
# get fileversion
function Get-FileVersion($fileName) {
	throw "not supported."

	$major = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($fileName).FileMajorPart
	$minor = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($fileName).FileMinorPart

	"$major.$minor"
}


#---------------------
# get version from _Version.props
function Get-Version($projectFile) {
	$xml = [xml](Get-Content $projectFile)
	
	$version = [String]$xml.Project.PropertyGroup.VersionPrefix
	
	if ($version -match '\d+\.\d+\.\d+') {
		return $version
	}
	
	throw "Cannot get version."
}

#---------------------
# create display version (MajorVersion.MinorVersion) from raw version
function Get-AppVersion($version) {
	$tokens = $version.Split(".")
	if ($tokens.Length -ne 3) {
		throw "Wrong version format."
	}
	$tokens = $version.Split(".")
	$majorVersion = [int]$tokens[0]
	$minorVersion = [int]$tokens[1]
	return "$majorVersion.$minorVersion"
}

#---------------------
# get git log
function Get-GitLog() {
	$branch = Invoke-Expression "git rev-parse --abbrev-ref HEAD"
	$descrive = Invoke-Expression "git describe --abbrev=0 --tags"
	$date = Invoke-Expression 'git log -1 --pretty=format:"%ad" --date=iso'
	$result = Invoke-Expression "git log $descrive..head --encoding=Shift_JIS --pretty=format:`"%ae %s`""
	$result = $result | Where-Object { $_ -match "^nee.laboratory" } | ForEach-Object { $_ -replace "^[\w\.@]+ ", "" }
	$result = $result | Where-Object { -not ($_ -match '^m.rge|^開発用|^作業中|\(dev\)|^-|^\.\.') } 

	return "[${branch}] $descrive to head", $date, $result
}

#---------------------
# get git log (markdown)
function Get-GitLogMarkdown($title) {
	$result = Get-GitLog
	$header = $result[0]
	$date = $result[1]
	$logs = $result[2]

	"## $title"
	"### $header"
	"Rev. $revision / $date"
	""
	$logs | ForEach-Object { "- $_" }
	""
	"This list of changes was auto generated."
}

#--------------------
# replace keyword
function Replace-Content {
	Param([string]$filepath, [string]$rep1, [string]$rep2)
	if ($(Test-Path $filepath) -ne $True) {
		Write-Error "file not found"
		return
	}
	if ($rep1 -eq "@HEAD") {
		$file_contents = $(Get-Content -Encoding UTF8 $filepath)
		$file_contents = @($rep2) + $file_contents
	}
	else {
		$file_contents = $(Get-Content -Encoding UTF8 $filepath) -replace $rep1, $rep2
	}
	$file_contents | Out-File -Encoding UTF8 $filepath
}


#-----------------------
# variables
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$solutionDir = Convert-Path "$scriptPath\.."
$solution = "$solutionDir\$product.sln"
$projectDir = "$solutionDir\$product"
$project = "$projectDir\$product.csproj"
$versionProps = "$solutionDir\_Version.props"

#----------------------
# build
function Build-Project($platform, $outputDir, $options) {
	$defaultOptions = @(
		"-p:PublishProfile=FolderProfile-$platform.pubxml"
		"-c", "Release"
	)

	& dotnet publish $project $defaultOptions $options -o Publish\$outputDir
	if ($? -ne $true) {
		throw "build error"
	}
}

function Build-ProjectSelfContained($platform) {
	$options = @(
		"--self-contained", "true"
	)

	Build-Project $platform "$product-$platform" $options
}

function Build-ProjectFrameworkDependent($platform) {
	$options = @(
		"-p:PublishTrimmed=false"
		"--self-contained", "false"
	)

	Build-Project $platform "$product-$platform-fd" $options
}

#----------------------
# package section
function New-Package($platform, $productName, $productDir, $packageDir) {
	$temp = New-Item $packageDir -ItemType Directory

	Copy-Item $productDir\* $packageDir -Recurse -Exclude ("*.pdb", "$product.config.json")

	# fix native dll
	#if ($platform -eq "x86")
	#{
	#	Remove-Item $packageDir\x64 -Recurse
	#}
	#if ($platform -eq "x64")
	#{
	#	Remove-Item $packageDir\x86 -Recurse
	#}

	# custom config
	New-ConfigForZip $productDir "$productName.config.json" $packageDir

	if ($platform.Contains("-fd")) {
		$target = ".zip-fd"
	}
	else {
		$target = ".zip"
	}

	# generate README.html
	New-Readme $packageDir "en-us" $target
	New-Readme $packageDir "ja-jp" $target
}

#----------------------
# generate ChangeLog

function Get-ChangeLog {
	param (
		[string]$Path = "Readme\ja-jp\ChangeLog.md",
		[int]$Version = 0
	)

	function Get-IndentLine($line) {
		# increment section depth
		if ($line.StartsWith("#")) {
			$line = "#" + $line
		}
        
		return $line
	}

	$versions = @{ header = @() }
	$current = "header"
	$latestVersion = 1
    
	$lines = Get-Content $Path
	foreach ($line in $lines) {
		if ($line.StartsWith("#")) {
			if ($line -match "^## (\d+)\.(\d+)") {
				$current = $Matches[1] + '.' + $Matches[2]
				$versions.add($current, @())
				$number = [int]$Matches[1]
				if ($number -gt $latestVersion) {
					$latestVersion = $number
				}
			}
		}
		$fixLine = Get-IndentLine $line
		$versions[$current] += $fixLine
	}

	if ($Version -eq 0) {
		$Version = $latestVersion
	}

	Write-Output $versions.header

	foreach ($item in $versions.GetEnumerator()) {
		if ($item.key -match "^$Version\.") {
			Write-Output $item.value
		}
	}
}

#----------------------
# generate README.html
function New-Readme($packageDir, $culture, $target) {
	$readmeSource = "$solutionDir\docs\$culture"

	$readmeDir = $packageDir + "\readme.$culture"

	$temp = New-Item $readmeDir -ItemType Directory 

	Copy-Item "$readmeSource\Overview.md" $readmeDir
	Copy-Item "$readmeSource\Canary.md" $readmeDir
	Copy-Item "$readmeSource\Environment.md" $readmeDir
	Copy-Item "$readmeSource\Package-zip.md" $readmeDir
	Copy-Item "$readmeSource\Package-zip-fd.md" $readmeDir
	Copy-Item "$readmeSource\Package-msi.md" $readmeDir
	Copy-Item "$readmeSource\Contact.md" $readmeDir
	Copy-Item "$readmeSource\SearchOptions.md" $readmeDir

	if (Test-Path "$readmeSource\LicenseAppendix.md") {
		Copy-Item "$readmeSource\LicenseAppendix.md" $readmeDir
	}

	Copy-Item "$solutionDir\LICENSE.md" $readmeDir
	Copy-Item "$solutionDir\THIRDPARTY_LICENSES.md" $readmeDir
	Copy-Item "$solutionDir\NeeLaboratory.IO.Search\THIRDPARTY_LICENSES.md" "$readmeDir\NeeLaboratory.IO.Search_THIRDPARTY_LICENSES.md"

	if ($target -eq ".canary") {
		Get-GitLogMarkdown "$product <VERSION/> - ChangeLog" | Set-Content -Encoding UTF8 "$readmeDir\ChangeLog.md"
	}
	else {
		Get-ChangeLog -Path "$readmeSource\ChangeLog.md" | Set-Content -Path "$readmeDir\ChangeLog.md"
	}

	$postfix = $appVersion
	$announce = ""
	if ($target -eq ".canary") {
		$postfix = "Canary ${dateVersion}"
		$announce = "Rev. ${revision}`r`n`r`n" + (Get-Content -Path "$readmeDir/Canary.md" -Raw -Encoding UTF8)
	}

	# edit README.md
	Replace-Content "$readmeDir\Overview.md" "<VERSION/>" "$postfix"
	Replace-Content "$readmeDir\Overview.md" "<ANNOUNCE/>" "$announce"
	Replace-Content "$readmeDir\Environment.md" "<VERSION/>" "$postfix"
	Replace-Content "$readmeDir\Contact.md" "<VERSION/>" "$postfix"
	Replace-Content "$readmeDir\ChangeLog.md" "<VERSION/>" "$postfix"
	Replace-Content "$readmeDir\LICENSE.md" "@HEAD" "## License"

	$readmeHtml = "README.html"

	if (-not ($culture -eq "en-us")) {
		$readmeHtml = "README.$culture.html"
	}

	$inputs = @()
	$inputs += "$readmeDir\Overview.md"

	if ($target -ne ".appx") {
		$inputs += "$readmeDir\Environment.md"
	}
	
	if (($target -eq ".zip") -or ($target -eq ".beta")) {
		$inputs += "$readmeDir\Package-zip.md"
	}
	elseif (($target -eq ".zip-fd") -or ($target -eq ".canary")) {
		$inputs += "$readmeDir\Package-zip-fd.md"
	}

	$inputs += "$readmeDir\Contact.md"

	$inputs += "$readmeDir\LICENSE.md"

	if (Test-Path "$readmeDir\LicenseAppendix.md") {
		$inputs += "$readmeDir\LicenseAppendix.md"
	}

	$inputs += "$readmeDir\THIRDPARTY_LICENSES.md"
	$inputs += "$readmeDir\NeeLaboratory.IO.Search_THIRDPARTY_LICENSES.md"
	$inputs += "$readmeDir\ChangeLog.md"

	$output = "$packageDir\$readmeHtml"
	$css = "Style.html"
	
	# markdown to html by pandoc
	pandoc -s -t html5 -o $output --metadata title="$product $postfix" -H $css $inputs
	if ($? -ne $true) {
		throw "pandoc error"
	}


	$searchOptionHtml = "SearchOptions.html"
	$searchOptionTitle = "$product Search Options"
	if (-not ($culture -eq "en-us")) {
		$searchOptionHtml = "SearchOptions.$culture.html"
		$searchOptionTitle = "$product 検索オプション"
	}

	$searchOptionInputs = @()
	$searchOptionInputs += "$readmeDir\SearchOptions.md"
	$searchOptionOutput = "$packageDir\$searchOptionHtml"

	pandoc -s -t html5 -o $searchOptionOutput --metadata title="$searchOptionTitle" -H $css $searchOptionInputs
	if ($? -ne $true) {
		throw "pandoc error"
	}

	Remove-Item $readmeDir -Recurse
}

#--------------------------
# remove ZIP
function Remove-Zip($packageZip) {
	if (Test-Path $packageZip) {
		Remove-Item $packageZip
	}
}

#--------------------------
# archive to ZIP
function New-Zip($referenceDir, $packageDir, $packageZip) {
	Copy-Item $referenceDir $packageDir -Recurse
	Optimize-Package $packageDir
	Compress-Archive $packageDir -DestinationPath $packageZip
}


#--------------------------
function Get-CulturesFromConfig($inputDir, $config) {
	[xml]$xml = Get-Content "$inputDir\$config"

	$add = $xml.configuration.appSettings.add | Where { $_.key -eq 'Cultures' } | Select -First 1
	return $add.value.Split(",")
}

#--------------------------
#
function New-ConfigForZip($inputDir, $config, $outputDir) {
	$jsonObject = (Get-Content "$inputDir\$config" | ConvertFrom-Json)

	$jsonObject.PackageType = ".zip"
	$jsonObject.SelfContained = Test-Path("$inputDir\hostfxr.dll")
	$jsonObject.Watermark = $false
	$jsonObject.UseLocalApplicationData = $false
	$jsonObject.Revision = $revision
	$jsonObject.DateVersion = $dateVersion
	#$jsonObject.PathProcessGroup = $true
	$jsonObject.LogFile = $trace ? "TraceLog.txt" : $null

	$outputFile = Join-Path (Convert-Path $outputDir) $config
	ConvertTo-Json $jsonObject | Out-File $outputFile
}

#--------------------------
#
function New-ConfigForMsi($inputDir, $config, $outputDir) {
	$jsonObject = (Get-Content "$inputDir\$config" | ConvertFrom-Json)

	$jsonObject.PackageType = ".msi"
	$jsonObject.SelfContained = $true
	$jsonObject.Watermark = $false
	$jsonObject.UseLocalApplicationData = $true
	$jsonObject.Revision = $revision
	$jsonObject.DateVersion = $dateVersion
	#$jsonObject.PathProcessGroup = $true
	$jsonObject.LogFile = $trace ? "TraceLog.txt" : $null

	$outputFile = Join-Path (Convert-Path $outputDir) $config
	ConvertTo-Json $jsonObject | Out-File $outputFile
}

#--------------------------
#
function New-ConfigForAppx($inputDir, $config, $outputDir) {
	$jsonObject = (Get-Content "$inputDir\$config" | ConvertFrom-Json)

	$jsonObject.PackageType = ".appx"
	$jsonObject.SelfContained = $true
	$jsonObject.Watermark = $false
	$jsonObject.UseLocalApplicationData = $true
	$jsonObject.Revision = $revision
	$jsonObject.DateVersion = $dateVersion
	#$jsonObject.PathProcessGroup = $true
	$jsonObject.LogFile = $trace ? "TraceLog.txt" : $null

	$outputFile = Join-Path (Convert-Path $outputDir) $config
	ConvertTo-Json $jsonObject | Out-File $outputFile
}

#--------------------------
#
function New-ConfigForDevPackage($inputDir, $config, $target, $outputDir) {
	$jsonObject = (Get-Content "$inputDir\$config" | ConvertFrom-Json)

	$jsonObject.PackageType = $target
	$jsonObject.SelfContained = Test-Path("$inputDir\hostfxr.dll")
	$jsonObject.Watermark = $true
	$jsonObject.UseLocalApplicationData = $false
	$jsonObject.Revision = $revision
	$jsonObject.DateVersion = $dateVersion
	#$jsonObject.PathProcessGroup = $true
	$jsonObject.LogFile = $trace ? "TraceLog.txt" : $null

	$outputFile = Join-Path (Convert-Path $outputDir) $config
	ConvertTo-Json $jsonObject | Out-File $outputFile
}

#---------------------------
#
function New-EmptyFolder($dir) {
	# remove folder
	if (Test-Path $dir) {
		Remove-Item $dir -Recurse
		Start-Sleep -m 100
	}

	# make folder
	$temp = New-Item $dir -ItemType Directory
}

#---------------------------
#
function New-PackageAppend($packageDir, $packageAppendDir) {
	New-EmptyFolder $packageAppendDir

	# configure customize
	New-ConfigForMsi $packageDir "${product}.config.json" $packageAppendDir

	# generate README.html
	New-Readme $packageAppendDir "en-us" ".msi"
	New-Readme $packageAppendDir "ja-jp" ".msi"

	# icons
	Copy-Item "$projectDir\Resources\App.ico" $packageAppendDir
}



#--------------------------
# remove Msi
function Remove-Msi($packageAppendDir, $packageMsi) {
	if (Test-Path $packageMsi) {
		Remove-Item $packageMsi
	}

	if (Test-Path $packageAppxDir_x64) {
		Remove-Item $packageAppxDir_x64 -Recurse
	}
}

#--------------------------
# Msi
function New-Msi($arch, $packageDir, $packageAppendDir, $packageMsi) {
	$candle = $env:WIX + 'bin\candle.exe'
	$light = $env:WIX + 'bin\light.exe'
	$heat = $env:WIX + 'bin\heat.exe'
	$torch = $env:WIX + 'bin\torch.exe'
	$wisubstg = "$Win10SDK\wisubstg.vbs"
	$wilangid = "$Win10SDK\wilangid.vbs"

	$1041Msi = "$packageAppendDir\1041.msi"
	$1041Mst = "$packageAppendDir\1041.mst"

	#-------------------------
	# WiX
	#-------------------------

	$ErrorActionPreference = "stop"

	function New-MainComponents {
		$wxs = Convert-Path "WixSource\$arch\MainComponents.wxs"
		& $heat dir "$packageDir" -cg MainComponents -ag -pog:Binaries -sfrag -srd -sreg -var var.ContentDir -dr INSTALLFOLDER -out $wxs
		if ($? -ne $true) {
			throw "heat error"
		}

		[xml]$xml = Get-Content $wxs

		Remove-WixComponentNode $xml "$product.exe"
		Remove-WixComponentNode $xml "$product.config.json"
		Remove-WixComponentNode $xml "README.html"
		Remove-WixComponentNode $xml "README.ja-jp.html"

		$xml.Save($wxs)
	}

	function Remove-WixComponentNode($xml, $name) {
		$node = $xml.Wix.Fragment[0].DirectoryRef.Component | Where-Object { (Split-Path $_.File.Source -Leaf) -eq $name }
		if ($null -ne $node) {
			$componentId = $node.Id
			$xml.Wix.Fragment[0].DirectoryRef.RemoveChild($node)

			$node = $xml.Wix.Fragment[1].ComponentGroup.ComponentRef | Where-Object { $_.Id -eq $componentId }
			$xml.Wix.Fragment[1].ComponentGroup.RemoveChild($node)
		}
	}

	function New-MsiSub($packageMsi, $culture) {
		Write-Host "$packageMsi : $culture" -fore Cyan
		
		$wixObjDir = "$packageAppendDir\obj.$culture"
		New-EmptyFolder $wixObjDir

		& $candle -arch $arch -d"Platform=$arch" -d"AppVersion=$appVersion" -d"ProductVersion=$version" -d"ContentDir=$packageDir\\" -d"AppendDir=$packageDir.append\\" -d"LibrariesDir=$packageDir\\Libraries" -d"culture=$culture" -ext WixNetFxExtension -out "$wixObjDir\\"  WixSource\*.wxs .\WixSource\$arch\*.wxs
		if ($? -ne $true) {
			throw "candle error"
		}

		& $light -out "$packageMsi" -ext WixUIExtension -ext WixNetFxExtension -cultures:$culture -loc WixSource\Language-$culture.wxl  "$wixObjDir\*.wixobj"
		if ($? -ne $true) {
			throw "light error" 
		}
	}

	## Create MainComponents.wxs
	if ($updateComponent) {
		Write-Host "Create MainComponents.wsx`n" -fore Cyan
		New-MainComponents
	}

	New-MsiSub $packageMsi "en-us"
	New-MsiSub $1041Msi "ja-jp"

	& $torch -p -t language $packageMsi $1041Msi -out $1041Mst
	if ($? -ne $true) {
		throw "torch error"
	}

	#-------------------------
	# WinSDK
	#-------------------------

	& cscript "$wisubstg" "$packageMsi" $1041Mst 1041
	if ($? -ne $true) {
		throw "wisubstg.vbs error"
	}

	& cscript "$wilangid" "$packageMsi" Package 1033, 1041
	if ($? -ne $true) {
		throw "wilangid.vbs error"
	}
}


#--------------------------
# Appx remove
function Remove-Appx($packageAppendDir, $appx) {
	if (Test-Path $appx) {
		Remove-Item $appx
	}

	if (Test-Path $packageAppxDir_x64) {
		Remove-Item $packageAppxDir_x64 -Recurse
	}
}

#--------------------------
# Appx 
function New-Appx($arch, $packageDir, $packageAppendDir, $appx) {
	$packgaeFilesDir = "$packageAppendDir/PackageFiles"
	$contentDir = "$packgaeFilesDir/$product"

	# copy package base files
	Copy-Item "Appx\Resources" $packgaeFilesDir -Recurse -Force

	# copy resources.pri
	Copy-Item "Appx\resources.pri" $packgaeFilesDir

	# update assembly
	Copy-Item $packageDir $contentDir -Recurse -Force
	New-ConfigForAppx $packageDir "${product}.config.json" $contentDir

	# generate README.html
	New-Readme $contentDir "en-us" ".appx"
	New-Readme $contentDir "ja-jp" ".appx"

	. $env:CersPath/_$product.Parameter.ps1
	$param = Get-AppxParameter
	$appxName = $param.name
	$appxPublisher = $param.publisher

	# generate AppManifest
	$content = Get-Content "Appx\AppxManifest.xml"
	$content = $content -replace "%NAME%", "$appxName"
	$content = $content -replace "%PUBLISHER%", "$appxPublisher"
	$content = $content -replace "%VERSION%", "$assemblyVersion"
	$content = $content -replace "%ARCH%", "$arch"
	$content | Out-File -Encoding UTF8 "$packgaeFilesDir\AppxManifest.xml"

	# re-package
	& "$Win10SDK\makeappx.exe" pack /l /d "$packgaeFilesDir" /p "$appx"
	if ($? -ne $true) {
		throw "makeappx.exe error"
	}

	# signing
	& "$Win10SDK\signtool.exe" sign -f "$env:CersPath/_NeeLaboratory.pfx" -fd SHA256 -v "$appx"
	if ($? -ne $true) {
		throw "signtool.exe error"
	}
}


#--------------------------
# archive to Canary.ZIP
function Remove-Canary() {
	if (Test-Path $packageCanary) {
		Remove-Item $packageCanary
	}

	if (Test-Path $packageCanaryDir) {
		Remove-Item $packageCanaryDir -Recurse
	}
}

function New-Canary($packageDir) {
	New-DevPackage $packageDir $packageCanaryDir $packageCanary ".canary"
}

function New-CanaryAnyCPU($packageDir) {
	New-DevPackage $packageDir $packageCanaryDir_AnyCPU $packageCanary_AnyCPU ".canary"
}

#--------------------------
# archive to Beta.ZIP
function Remove-Beta() {
	if (Test-Path $packageBeta) {
		Remove-Item $packageBeta
	}

	if (Test-Path $packageBetaDir) {
		Remove-Item $packageBetaDir -Recurse
	}
}

function New-Beta($packageDir) {
	New-DevPackage $packageDir $packageBetaDir $packageBeta ".beta"
}

#--------------------------
# archive to Canary/Beta.ZIP
function New-DevPackage($packageDir, $devPackageDir, $devPackage, $target) {
	# update assembly
	Copy-Item $packageDir $devPackageDir -Recurse
	New-ConfigForDevPackage $packageDir "${product}.config.json" $target $devPackageDir

	# generate README.html
	New-Readme $devPackageDir "en-us" $target
	New-Readme $devPackageDir "ja-jp" $target

	Optimize-Package $devPackageDir
	Compress-Archive $devPackageDir -DestinationPath $devPackage
}

#--------------------------
# Optimizing file placement with NetBeauty
# https://github.com/nulastudio/NetBeauty2
function Optimize-Package($packageDir) {
	Write-Host "NetBeauty2" -fore Cyan
	nbeauty2 --usepatch --loglevel Detail $packageDir Libraries

	$unusedFile = "$packageDir\hostfxr.dll.bak"
	if (Test-Path $unusedFile) {
		Remove-Item $unusedFile
	}
}


$build_x64 = $false
$build_x86 = $false
$build_x64_fd = $false

#--------------------------
# remove build objects
function Remove-BuildObjects {
	Get-ChildItem -Directory "$packagePrefix*" | Remove-Item -Recurse

	Get-ChildItem -File "$packagePrefix*.*" | Remove-Item

	if (Test-Path $publishDir) {
		Remove-Item $publishDir -Recurse
	}
	if (Test-Path $packageCanaryDir) {
		Remove-Item $packageCanaryDir -Recurse -Force
	}
	if (Test-Path $packageBetaDir) {
		Remove-Item $packageBetaDir -Recurse -Force
	}
	if (Test-Path $packageCanaryWild) {
		Remove-Item $packageCanaryWild
	}
	if (Test-Path $packageBetaWild) {
		Remove-Item $packageBetaWild
	}

	Start-Sleep -m 100
}

function Build-Clear {
	# clear
	Write-Host "`n[Clear] ...`n" -fore Cyan
	Remove-BuildObjects
}

function Build-UpdateState {
	$global:build_x64 = Test-Path $publishDir_x64
	$global:build_x86 = Test-Path $publishDir_x86
	$global:build_x64_fd = Test-Path $publishDir_x64_fd
}

function Build-PackageSource-x64 {
	if ($global:build_x64 -eq $true) { return }

	# build
	Write-Host "`n[Build] ...`n" -fore Cyan
	Build-ProjectSelfContained "x64"
	
	# create package source
	Write-Host "`n[Package] ...`n" -fore Cyan
	New-Package "x64" $product $publishDir_x64 $packageDir_x64

	$global:build_x64 = $true
}

function Build-PackageSource-x86 {
	if ($global:build_x86 -eq $true) { return }

	# build
	Write-Host "`n[Build x86] ...`n" -fore Cyan
	Build-ProjectSelfContained "x86"
	
	# create package source
	Write-Host "`n[Package x86] ...`n" -fore Cyan
	New-Package "x86" $product $publishDir_x86 $packageDir_x86

	$global:build_x86 = $true
}

function Build-PackageSource-x64-fd {
	if ($global:build_x64_fd -eq $true) { return }

	# build
	Write-Host "`n[Build frameword dependent] ...`n" -fore Cyan
	Build-ProjectFrameworkDependent "x64"
	
	# create package source
	Write-Host "`n[Package frameword dependent] ...`n" -fore Cyan
	New-Package "x64-fd" $product $publishDir_x64_fd $packageDir_x64_fd

	$global:build_x64_fd = $true
}


function Build-Zip-x64 {
	Write-Host "`[Zip] ...`n" -fore Cyan

	Remove-Zip $packageZip_x64
	New-Zip $packageDir_x64 $packageName_x64 $packageZip_x64
	Write-Host "`nExport $packageZip_x64 successed.`n" -fore Green
}

function Build-Zip-x86 {
	Write-Host "`[Zip x86] ...`n" -fore Cyan

	Remove-Zip $packageZip_x86
	New-Zip $packageDir_x86 $packageName_x86 $packageZip_x86
	Write-Host "`nExport $packageZip_x86 successed.`n" -fore Green
}

function Build-Zip-x64-fd {
	Write-Host "`[Zip fd] ...`n" -fore Cyan

	Remove-Zip $packageZip_x64_fd
	New-Zip $packageDir_x64_fd $packageName_x64_fd $packageZip_x64_fd
	Write-Host "`nExport $packageZip_x64_fd successed.`n" -fore Green
}


function Build-Installer-x64 {
	Write-Host "`n[Installer] ...`n" -fore Cyan
	
	Remove-Msi $packageAppendDir_x64 $packageMsi_x64
	New-PackageAppend $packageDir_x64 $packageAppendDir_x64
	New-Msi "x64" $packageDir_x64 $packageAppendDir_x64 $packageMsi_x64
	Write-Host "`nExport $packageMsi_x64 successed.`n" -fore Green
}

function Build-Installer-x86 {
	Write-Host "`n[Installer x86] ...`n" -fore Cyan

	Remove-Msi $packageAppendDir_x86 $packageMsi_x86
	New-PackageAppend $packageDir_x86 $packageAppendDir_x86
	New-Msi "x86" $packageDir_x86 $packageAppendDir_x86 $packageMsi_x86
	Write-Host "`nExport $packageMsi_x86 successed.`n" -fore Green
}

function Build-Appx-x64 {
	Write-Host "`n[Appx] ...`n" -fore Cyan

	if (Test-Path "$env:CersPath\_Parameter.ps1") {
		Remove-Appx $packageAppxDir_x64 $packageX64Appx
		New-Appx "x64" $packageDir_x64 $packageAppxDir_x64 $packageX64Appx
		Write-Host "`nExport $packageX64Appx successed.`n" -fore Green
	}
	else {
		Write-Host "`nWarning: not exist make appx envionment. skip!`n" -fore Yellow
	}
}

function Build-Appx-x86 {
	Write-Host "`n[Appx x86] ...`n" -fore Cyan

	if (Test-Path "$env:CersPath\_Parameter.ps1") {
		Remove-Appx $packageAppxDir_x86 $packageX86Appx
		New-Appx "x86" $packageDir_x86 $packageAppxDir_x86 $packageX86Appx
		Write-Host "`nExport $packageX86Appx successed.`n" -fore Green
	}
	else {
		Write-Host "`nWarning: not exist make appx envionment. skip!`n" -fore Yellow
	}
}

function Build-Canary {
	Write-Host "`n[Canary] ...`n" -fore Cyan
	Remove-Canary
	New-Canary $packageDir_x64_fd
	Write-Host "`nExport $packageCanary successed.`n" -fore Green
}

function Build-Beta {
	Write-Host "`n[Beta] ...`n" -fore Cyan
	Remove-Beta
	New-Beta $packageDir_x64
	Write-Host "`nExport $packageBeta successed.`n" -fore Green
}


function Export-Current {
	Write-Host "`n[Current] ...`n" -fore Cyan
	if (Test-Path $packageDir_x64_fd) {
		if (-not (Test-Path $product)) {
			New-Item $product -ItemType Directory
		}
		Copy-Item "$packageDir_x64_fd\*" "$product\" -Recurse -Force
	}
	else {
		Write-Host "`nWarning: not exist $packageDir_x64_fd. skip!`n" -fore Yellow
	}
}

function Update-Version {
	Write-Host "`n`[Update NeeLaboratory Libraries Version] ...`n" -fore Cyan
	..\NeeLaboratory\CreateVersionProps.ps1
	
	Write-Host "`n`[Update NeeLaboratory.IO.Search Version] ...`n" -fore Cyan
	..\NeeLaboratory.IO.Search\CreateVersionProps.ps1
	
	Write-Host "`n`[Update RealtimeSearch Version] ...`n" -fore Cyan
	$versionSuffix = switch ( $Target ) {
		"Dev" { 'dev' }
		"Canary" { 'canary' }
		"Beta" { 'beta' }
		default { '' }
	}
	..\CreateVersionProps.ps1 -suffix $versionSuffix
}


#======================
# main
#======================

Update-Version

# versions
$version = Get-Version $versionProps
$appVersion = Get-AppVersion $version
$assemblyVersion = "$version.0"
$revision = (& git rev-parse --short HEAD).ToString()
$dateVersion = (Get-Date).ToString("MMdd") + $versionPostfix

$publishDir = "Publish"
$publishDir_x64 = "$publishDir\$product-x64"
$publishDir_x86 = "$publishDir\$product-x86"
$publishDir_x64_fd = "$publishDir\$product-x64-fd"
$packagePrefix = "$product$appVersion"
$packageDir_x64 = "$product$appVersion-x64"
$packageDir_x86 = "$product$appVersion-x86"
$packageDir_x64_fd = "$product$appVersion-x64-fd"
$packageAppendDir_x64 = "$packageDir_x64.append"
$packageAppendDir_x86 = "$packageDir_x86.append"
$packageName_x64 = "${product}${appVersion}"
$packageName_x86 = "${product}${appVersion}-x86"
$packageName_x64_fd = "${product}${appVersion}-fd"
$packageZip_x64 = "$packageName_x64.zip"
$packageZip_x86 = "$packageName_x86.zip"
$packageZip_x64_fd = "$packageName_x64_fd.zip"
$packageMsi_x64 = "$packageName_x64.msi"
$packageMsi_x86 = "$packageName_x86.msi"
$packageAppxDir_x64 = "${product}${appVersion}-appx-x64"
$packageAppxDir_x86 = "${product}${appVersion}-appx-x84"
$packageX86Appx = "${product}${appVersion}-x86.msix"
$packageX64Appx = "${product}${appVersion}.msix"

$packageNameCanary = "${product}${appVersion}-Canary${dateVersion}"
$packageCanaryDir = "$packageNameCanary"
$packageCanaryDir_AnyCPU = "$packageNameCanary-AnyCPU"
$packageCanary = "$packageNameCanary.zip"
$packageCanary_AnyCPU = "packageNameCanary_AnyCPU.zip"
$packageCanaryWild = "${product}${appVersion}-Canary*.zip"

$packageNameBeta = "${product}${appVersion}-Beta${dateVersion}"
$packageBetaDir = "$packageNameBeta"
$packageBeta = "$packageNameBeta.zip"
$packageBetaWild = "${product}${appVersion}-Beta*.zip"

if (-not $continue) {
	Build-Clear
}

Build-UpdateState


if (($Target -eq "All") -or ($Target -eq "Zip")) {
	if ($x86) {
		Build-PackageSource-x86
		Build-Zip-x86
	}
	else {
		Build-PackageSource-x64
		Build-Zip-x64

		Build-PackageSource-x64-fd
		Build-Zip-x64-fd
	}
}

if (($Target -eq "All") -or ($Target -eq "Installer")) {
	if ($x86) {
		Build-PackageSource-x86
		Build-Installer-x86
	}
	else {
		Build-PackageSource-x64
		Build-Installer-x64
	}
}

if (($Target -eq "All") -or ($Target -eq "Appx")) {
	if ($x86) {
		Build-PackageSource-x86
		Build-Appx-x86
	}
	else {
		Build-PackageSource-x64
		Build-Appx-x64
	}
}

if (-not $x86) {
	if ($Target -eq "Canary") {
		Build-PackageSource-x64-fd
		Build-Canary
	}

	if ($Target -eq "Beta") {
		Build-PackageSource-x64
		Build-Beta
	}

	if (-not $continue) {
		Build-PackageSource-x64-fd
		Export-Current
	}
}

#-------------------------
# Finish.
Write-Host "`nBuild $version All done.`n" -fore Green





