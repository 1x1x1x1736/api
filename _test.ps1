$content = Get-Content 'Menu\Buttons.cs' -Raw
$test = "`r`nnew ButtonInfo { buttonText = `"Player info hand gun <color=grey>[</color><color=green>V2</color><color=grey>]</color>`", method =() => {}, toolTip = `"Test`" },"
$content + $test | Set-Content 'Menu\Buttons.cs' -Encoding UTF8
Write-Host 'added'
