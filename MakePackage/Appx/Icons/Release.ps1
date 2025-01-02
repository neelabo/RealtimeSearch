$outputDir = "..\Resources\Assets"
$postfix = ""

# error to break
trap { break }

$ErrorActionPreference = "stop"


Get-ChildItem Pngs$postfix\*.png | Copy-Item -Destination $outputDir
Get-ChildItem TilePngs$postfix\*.png | Copy-Item -Destination $outputDir


