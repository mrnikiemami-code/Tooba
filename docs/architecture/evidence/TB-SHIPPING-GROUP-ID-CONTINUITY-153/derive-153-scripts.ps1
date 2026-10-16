# TB-SHIPPING-GROUP-ID-CONTINUITY-153 — derive the task-153 isolated scripts from the proven TB-152
# recipe WITHOUT touching the committed TB-152 evidence (read-only source, 153-owned copies written).
$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$src = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152'
$dst = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-GROUP-ID-CONTINUITY-153'
New-Item -ItemType Directory -Force -Path $dst | Out-Null

function Convert-Script([string]$name, [string]$outName) {
    $text = Get-Content -LiteralPath (Join-Path $src $name) -Raw
    $pairs = [ordered]@{
        'TB-SHIPPING-INDEPENDENT-GROUP-ACCEPTANCE-152' = 'TB-SHIPPING-GROUP-ID-CONTINUITY-153'
        '.tmp-152-run'            = '.tmp-153-run'
        '.tmp-152-host-out'       = '.tmp-153-host-out'
        'tenant-152-scratch'      = 'tenant-153-scratch'
        'messaging-152-scratch'   = 'messaging-153-scratch'
        'tooba_152_scratch'       = 'tooba_153_scratch'
        'tooba_152_messaging'     = 'tooba_153_messaging'
        'scratch152'              = 'scratch153'
        "'55471'"                 = "'55472'"
        'TOOBA_152_SCRATCH_PASSWORD' = 'TOOBA_153_SCRATCH_PASSWORD'
        'TOOBA_152_HOST_PORT'     = 'TOOBA_153_HOST_PORT'
        'TOOBA_152_WEB_PORT'      = 'TOOBA_153_WEB_PORT'
        'TOOBA_152_HOST'          = 'TOOBA_153_HOST'
        'tooba-152-isolated'      = 'tooba-153-isolated'
        'isolated-152'            = 'isolated-153'
        'isolated-host-152'       = 'isolated-host-153'
        '-152.json'               = '-153.json'
        'host-launch-152'         = 'host-launch-153'
        'journey-152'             = 'journey-153'
        '-Port 5413'              = '-Port 5414'
        '127.0.0.1:5413'          = '127.0.0.1:5414'
        '= 5413'                  = '= 5414'
        '55455|55458|55460|55461|55462|55463|55470' = '55455|55458|55460|55461|55462|55463|55470|55471'
    }
    foreach ($k in $pairs.Keys) { $text = $text.Replace($k, $pairs[$k]) }
    Set-Content -LiteralPath (Join-Path $dst $outName) -Value $text -Encoding utf8 -NoNewline
    Write-Output "WROTE $outName ($($text.Length) chars)"
}

Convert-Script 'prepare-isolated-target-152.js' 'prepare-isolated-target-153.js'
Convert-Script 'launch-isolated-host-152.ps1'  'launch-isolated-host-153.ps1'
Convert-Script 'journey-152.js'                'journey-153.js'

# seller-specific preparation days (same authoritative scratch overlay shape as TB-152: A=2, B=3)
New-Item -ItemType Directory -Force -Path (Join-Path $repo '.tmp-153-run') | Out-Null
$prep = [ordered]@{
    '01a1460a-49e3-7000-a074-73a5b67329cf' = 2
    '01a1460a-4c8d-7000-80df-fdc458534eea' = 3
}
$prep | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $repo '.tmp-153-run\seller-prep.json') -Encoding utf8
Write-Output 'WROTE seller-prep.json'
