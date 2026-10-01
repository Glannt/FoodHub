# Đồng bộ các khối ```mermaid trong file .md với file .mmd.
# Dùng khi sửa diagram: sửa .mmd trước, rồi chạy lại script này.
# PowerShell: powershell -ExecutionPolicy Bypass -File sync-mermaid.ps1

$ErrorActionPreference = 'Stop'
$docs = Split-Path -Parent $MyInvocation.MyCommand.Path

# Thứ tự các khối mermaid trong từng file .md
$plan = @{
    'ARCHITECTURE.md' = @(
        '10-compile-time-flow', '11-config-precedence', '12-assembly-reference-graph',
        '13-runtime-startup', '14-request-lifecycle', '15-di-container-resolve',
        '01-create-update-menu-item', '02-get-menu-by-restaurant', '03-outbox-dispatcher'
    )
    'SEQ-DIAGRAMS.md' = @(
        '01-create-update-menu-item', '02-get-menu-by-restaurant', '03-outbox-dispatcher'
    )
}

$utf8NoBom = New-Object System.Text.UTF8Encoding $false

foreach ($mdName in $plan.Keys) {
    $mdPath = Join-Path $docs $mdName
    $md = [System.IO.File]::ReadAllLines($mdPath)

    $out = New-Object System.Collections.Generic.List[string]
    $block = 0
    $i = 0
    $expected = $plan[$mdName]

    while ($i -lt $md.Length) {
        if ($md[$i].Trim() -eq '```mermaid') {
            if ($block -ge $expected.Count) {
                throw "$mdName : thua so khối mermaid, khong co file .mmd tuong ung"
            }
            $mmdPath = Join-Path $docs "diagrams\$($expected[$block]).mmd"
            if (-not (Test-Path $mmdPath)) { throw "Khong tim thay $mmdPath" }

            $out.Add('```mermaid')
            foreach ($line in [System.IO.File]::ReadAllLines($mmdPath)) { $out.Add($line) }
            $out.Add('```')
            $block++

            $i++
            while ($i -lt $md.Length -and $md[$i].Trim() -ne '```') { $i++ }
            $i++
        } else {
            $out.Add($md[$i]); $i++
        }
    }

    if ($block -ne $expected.Count) {
        throw "$mdName : chi tim thay $block khoi mermaid, expected $($expected.Count)"
    }

    [System.IO.File]::WriteAllLines($mdPath, $out, $utf8NoBom)
    Write-Host "$mdName : da dong bo $block khoi mermaid" -ForegroundColor Green
}

# Kiem tra lai
Write-Host "`nKiem tra khop:" -ForegroundColor Cyan
foreach ($mdName in $plan.Keys) {
    $mdPath = Join-Path $docs $mdName
    $mdText = [System.IO.File]::ReadAllText($mdPath) -replace "`r`n", "`n"
    $n = 0
    foreach ($f in $plan[$mdName]) {
        $mmd = ([System.IO.File]::ReadAllText((Join-Path $docs "diagrams\$f.mmd")) -replace "`r`n", "`n").Trim()
        $ok = $mdText.Contains($mmd)
        $n++
        $color = if ($ok) { 'Green' } else { 'Red' }
        Write-Host ("  {0,-32} {1}" -f $f, $(if ($ok) { 'Khop' } else { 'KHOP' })) -ForegroundColor $color
    }
}
