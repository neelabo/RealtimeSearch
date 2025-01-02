# マニフェストデザイナーで作られたタイルアイコン素材をコピーする

Param(
    [string]$inputDir = "..\..\..\_RealtimeSearchWinUI3\Assets",
    [string]$outputDir = "TilePngs"
)


function Copy-TilePng($src, $dst)
{
    $options = @("scale-100", "scale-125", "scale-150", "scale-200", "scale-400")
    foreach($option in $options)
    {
        $srcName = "$src.$option.png"
        $dstName = "$dst.$option.png"
        Copy-Item $inputDir\$srcName -Destination $outputDir\$dstName
    }
}


Copy-TilePng "LargeTile" "LargeTile"
Copy-TilePng "Square150x150Logo" "MedTile"
Copy-TilePng "SmallTile" "SmallTile"
Copy-TilePng "Wide310x150Logo" "WideTile"



