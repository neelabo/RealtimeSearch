# Png を Assets にリリースする

$outputDir = "..\Resources\Assets"
$postfix = ""

$inputPngs = "_Pngs"
$inputTilePngs = "_TilePngs"

# error to break
trap { break }

$ErrorActionPreference = "stop"


Get-ChildItem $inputPngs$postfix\*.png | Copy-Item -Destination $outputDir
Get-ChildItem $inputTilePngs$postfix\*.png | Copy-Item -Destination $outputDir


