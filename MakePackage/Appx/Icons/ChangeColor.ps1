# SVGの色変更

$inputDir = "Svgs"
#$outputDir = "Svgs-Blue"
$fromColor = "Red"
$toColor = "Blue"
#$toColor = "Green"

$outputDir = "$inputDir-$toColor"
if (!(Test-Path $outputDir))
{
    New-Item -Path . -Name $outputDir -ItemType Directory
}


# error to break
trap { break }

$ErrorActionPreference = "stop"

$colorTable = @(
    [PSCustomObject]@{ Red = "#ffa500"; Blue = "#ff6347" }
)

#foreach($item in $colorTable)
#{
#    $red = $item.Red
#    Write-Host "$red, $($item.Red) -> $($item.Blue)"
#}


function Convert-ColorCode($s)
{
    foreach($item in $colorTable)
    {
        $s = $s -replace $item.$fromColor, $item.$toColor
    }
    return $s
}


# color change (red -> blue)
function Convert-SvgColorCode($source, $output)
{
    (Get-Content $source) | ForEach-Object { Convert-ColorCode $_ } | Set-Content $output
}



$files = Get-ChildItem -Name $inputDir-$fromColor\*.svg

foreach($file in $files)
{
    Convert-SvgColorCode $inputDir-$fromColor\$file $outputDir\$file
}



