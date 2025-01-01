
Param(
    [parameter(mandatory)][string]$inputDir,
    [parameter(mandatory)][string]$outputDir
)

$inkscape = 'E:\Bin\inkscape-1.2.2\bin\inkscape.exe'
#$inputDir = "Svgs"
#$outputDir = "Pngs"

# error to break
trap { break }

$ErrorActionPreference = "stop"


function Export-Png($svg, $png, $size)
{
    & $inkscape $svg --export-filename=$png --export-overwrite --export-height=$size --export-width=$size
}


if (!(Test-Path $outputDir))
{
    New-Item -Path . -Name $outputDir -ItemType Directory
}

<#
$files = (Get-ChildItem $inputDir\*.svg).Name
foreach($file in $files)
{
    $png = [System.IO.Path]::ChangeExtension($file,".png")
    Export-Png $inputDir\$file $outputDir\$png
}
#>

Export-Png $inputDir\AppList.targetsize-16.svg $outputDir\AppList.targetsize-16.png 16
Export-Png $inputDir\AppList.targetsize-24.svg $outputDir\AppList.targetsize-24.png 24
Export-Png $inputDir\AppList.targetsize-32.svg $outputDir\AppList.targetsize-32.png 32
Export-Png $inputDir\AppList.targetsize-48.svg $outputDir\AppList.targetsize-48.png 48
Export-Png $inputDir\AppList.targetsize-256.svg $outputDir\AppList.targetsize-256.png 256
Export-Png $inputDir\AppList.targetsize-400.svg $outputDir\AppList.targetsize-400.png 400

Export-Png $inputDir\AppList.targetsize-44.svg $outputDir\AppList.scale-100.png 44
Export-Png $inputDir\AppList.targetsize-55.svg $outputDir\AppList.scale-125.png 55
Export-Png $inputDir\AppList.targetsize-44.svg $outputDir\AppList.scale-150.png 66
Export-Png $inputDir\AppList.targetsize-44.svg $outputDir\AppList.scale-200.png 88
Export-Png $inputDir\AppList.targetsize-44.svg $outputDir\AppList.scale-400.png 176

Export-Png $inputDir\AppList.targetsize-50.svg $outputDir\StoreLogo.scale-100.png 50
Export-Png $inputDir\AppList.targetsize-63.svg $outputDir\StoreLogo.scale-125.png 63
Export-Png $inputDir\AppList.targetsize-50.svg $outputDir\StoreLogo.scale-150.png 75
Export-Png $inputDir\AppList.targetsize-50.svg $outputDir\StoreLogo.scale-200.png 100
Export-Png $inputDir\AppList.targetsize-50.svg $outputDir\StoreLogo.scale-400.png 200

