$path = $args[0]
if (-not (Test-Path $path)) { Write-Error "File not found: $path"; exit 2 }
$fs = [System.IO.File]::OpenRead($path)
$br = New-Object System.IO.BinaryReader($fs)
try {
    $fs.Position = 0x3c
    $peOffset = $br.ReadInt32()
    $fs.Position = $peOffset + 4
    $machine = $br.ReadUInt16()
    switch ($machine) {
        332 { Write-Output "x86" }
        34404 { Write-Output "x64" }
        447 { Write-Output "ARM" }
        43620 { Write-Output "ARM64" }
        default { Write-Output ("Unknown (0x{0:X})" -f $machine) }
    }
}
finally {
    $br.Close()
    $fs.Close()
}
