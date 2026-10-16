# TB-SHIPPING-GROUP-UX-ACCEPTANCE-154 — derive the task-154 isolated scripts from the proven TB-153
# recipe WITHOUT touching committed TB-152/TB-153 evidence (read-only sources, 154-owned copies).
$ErrorActionPreference = 'Stop'
$repo = 'D:\Users\User\source\repos\SarvNewVer'
$src = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-GROUP-ID-CONTINUITY-153'
$dst = Join-Path $repo 'docs\architecture\evidence\TB-SHIPPING-GROUP-UX-ACCEPTANCE-154'
New-Item -ItemType Directory -Force -Path $dst | Out-Null

function Convert-Script([string]$name, [string]$outName, [hashtable]$extra) {
    $text = Get-Content -LiteralPath (Join-Path $src $name) -Raw
    $pairs = [ordered]@{
        'TB-SHIPPING-GROUP-ID-CONTINUITY-153' = 'TB-SHIPPING-GROUP-UX-ACCEPTANCE-154'
        '.tmp-153-run'            = '.tmp-154-run'
        '.tmp-153-host-out'       = '.tmp-154-host-out'
        'tenant-153-scratch'      = 'tenant-154-scratch'
        'messaging-153-scratch'   = 'messaging-154-scratch'
        'tooba_153_scratch'       = 'tooba_154_scratch'
        'tooba_153_messaging'     = 'tooba_154_messaging'
        'scratch153'              = 'scratch154'
        "'55472'"                 = "'55473'"
        'TOOBA_153_SCRATCH_PASSWORD' = 'TOOBA_154_SCRATCH_PASSWORD'
        'TOOBA_153_HOST_PORT'     = 'TOOBA_154_HOST_PORT'
        'TOOBA_153_WEB_PORT'      = 'TOOBA_154_WEB_PORT'
        'TOOBA_153_HOST'          = 'TOOBA_154_HOST'
        'tooba-153-isolated'      = 'tooba-154-isolated'
        'isolated-153'            = 'isolated-154'
        'isolated-host-153'       = 'isolated-host-154'
        '-153.json'               = '-154.json'
        'host-launch-153'         = 'host-launch-154'
        'journey-153'             = 'journey-154'
        'TOOBA_153_SELLER_A'      = 'TOOBA_154_SELLER_A'
        'TOOBA_153_SELLER_B'      = 'TOOBA_154_SELLER_B'
        'TOOBA_153_OFFER_A'       = 'TOOBA_154_OFFER_A'
        'TOOBA_153_OFFER_A2'      = 'TOOBA_154_OFFER_A2'
        'TOOBA_153_OFFER_B'       = 'TOOBA_154_OFFER_B'
        'TOOBA_153_MOBILE'        = 'TOOBA_154_MOBILE'
        '-Port 5414'              = '-Port 5415'
        '127.0.0.1:5414'          = '127.0.0.1:5415'
        '= 5414'                  = '= 5415'
        "$\{HOST_PORT\} = '5413'"  = '${HOST_PORT} = ''5415'''
        'TOOBA_152_HOST_PORT'     = 'TOOBA_152_HOST_PORT'
        '55470|55471'             = '55470|55471|55472'
        '55462|55463|55470|55471' = '55462|55463|55470|55471|55472'
    }
    foreach ($k in $pairs.Keys) { $text = $text.Replace($k, $pairs[$k]) }
    if ($extra) { foreach ($k in $extra.Keys) { $text = $text.Replace($k, $extra[$k]) } }
    Set-Content -LiteralPath (Join-Path $dst $outName) -Value $text -Encoding utf8 -NoNewline
    Write-Output "WROTE $outName ($($text.Length) chars)"
}

Convert-Script 'prepare-isolated-target-153.js' 'prepare-isolated-target-154.js' $null
Convert-Script 'launch-isolated-host-153.ps1'   'launch-isolated-host-154.ps1'   $null
Convert-Script 'journey-153.js'                 'journey-154.js'                 $null

# seller-specific preparation days (same authoritative scratch overlay shape as TB-152/TB-153: A=2, B=3)
New-Item -ItemType Directory -Force -Path (Join-Path $repo '.tmp-154-run') | Out-Null
$prep = [ordered]@{
    '01a1460a-49e3-7000-a074-73a5b67329cf' = 2
    '01a1460a-4c8d-7000-80df-fdc458534eea' = 3
}
$prep | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $repo '.tmp-154-run\seller-prep.json') -Encoding utf8
Write-Output 'WROTE seller-prep.json'
