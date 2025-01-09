# 不足している AltForm 素材をコピーして作成する

Param(
    [string]$workDir = "_Pngs"
)

# error to break
trap { break }

$ErrorActionPreference = "stop"

function Copy-AlfFormUnplated($baseName) {
    $baseNameEscaped = [Regex]::Escape($baseName)

    $items = Get-ChildItem -Path $workDir -Filter "$baseName*.png"
    foreach ($item in $items) {
        if ($item.Name -match "$baseNameEscaped(?<size>\d+)\.png") {
            $size = $matches["size"]
            Copy-Item $item "$workDir\$baseName${size}_altform-unplated.png"
        }
    }
}

function Copy-AlfFormLightUnplated($baseName) {
    $baseNameEscaped = [Regex]::Escape($baseName)

    $items = Get-ChildItem -Path $workDir -Filter "$baseName*_altform-unplated.png"
    foreach ($item in $items) {
        if ($item.Name -match "$baseNameEscaped(?<size>\d+)_altform-unplated\.png") {
            $size = $matches["size"]
            Copy-Item $item "$workDir\$baseName${size}_altform-lightunplated.png"
        }
    }
}

Copy-AlfFormUnplated "AppList.targetsize-"
Copy-AlfFormLightUnplated "AppList.targetsize-"



