$filePath = 'C:\Users\kalew\Downloads\Seralyth-Menu-4.8.5 (1)\Seralyth-Menu-4.8.5\Menu\Buttons.cs'
$lines = [System.IO.File]::ReadAllLines($filePath)
$corrupted = $lines[1604]
Write-Output "Corrupted line length: $($corrupted.Length)"
Write-Output "First 200 chars: $($corrupted.Substring(0, 200))"
$lines[1604] = '                new ButtonInfo { buttonText = "Super Infection Draw Gun", method = Overpowered.SuperInfectionDrawGun, toolTip = "Allows you to draw with entities in Super Infection."},'
[System.IO.File]::WriteAllLines($filePath, $lines)
Write-Output "Fixed!"
$fixed = [System.IO.File]::ReadAllLines($filePath)
Write-Output "New line: $($fixed[1604])"
