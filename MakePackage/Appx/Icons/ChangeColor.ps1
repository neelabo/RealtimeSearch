# SVGの色変更

$srcColor = "Src"
$dstColor = "Dst"

$inputDir = "Svgs"
$outputDir = "$inputDir-$dstColor"

if (!(Test-Path $outputDir)) {
    New-Item -Path . -Name $outputDir -ItemType Directory
}


# error to break
trap { break }

$ErrorActionPreference = "stop"

$colorTable = @(
    #[PSCustomObject]@{ Red = "#ffa500"; Blue = "#ff6347" }
    [PSCustomObject]@{ Src = "#ff6347"; Dst = "#cc2000" }
)

#foreach($item in $colorTable)
#{
#    $red = $item.Red
#    Write-Host "$red, $($item.Red) -> $($item.Blue)"
#}


function Convert-ColorCode($s) {
    foreach ($item in $colorTable) {
        $s = $s -replace $item.$srcColor, $item.$dstColor
    }
    return $s
}

# 特殊な変換：最後から検索して１つ目のみ変換
function Convert-SvgColorCodeEx($lines) {

    for($i = $lines.Count - 1; $i -ge 0 ; $i--) {
        $newLine = Convert-ColorCode $lines[$i]
        if ($lines[$i] -ne $newLine) {
            $lines[$i] = $newLine
            break
        }
    }
    return $lines
}

# color change (red -> blue)
function Convert-SvgColorCode($source, $output) {
    #(Get-Content $source) | ForEach-Object { Convert-ColorCode $_ } | Set-Content $output
    $lines = Get-Content $source
    $lines = Convert-SvgColorCodeEx $lines
    Set-Content -Path $output -Value $lines
}


$files = Get-ChildItem -Name $inputDir\*.svg
foreach ($file in $files) {
    Convert-SvgColorCode $inputDir\$file $outputDir\$file
}



