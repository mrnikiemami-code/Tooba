# TB-SHIPPING-MVP-DESTINATION-LIVE-067 -- one bounded live end-to-end destination proof.
# Canonical development customer flow only: register -> login -> guest cart -> real offer line -> projection.
# No order is placed and no payment is made.
$ErrorActionPreference = 'Continue'
$base = 'http://127.0.0.1:5088'
$stamp = (Get-Date -Format 'yyyyMMddHHmmss')
$identifier = "067.probe.$stamp@tooba.local"
$pwd = 'Tooba!067probe'
$offerId = '0442a1fa-1d95-4b24-b9d9-129fc19942d2'   # real dev catalog offer (نایک شلوار مردانه سری 1)

function Show($label, $block) {
  Write-Output ""
  Write-Output "--- $label ---"
  try { & $block } catch {
    $resp = $_.Exception.Response
    if ($resp) {
      $sr = New-Object System.IO.StreamReader($resp.GetResponseStream())
      Write-Output ("HTTP {0} {1}" -f [int]$resp.StatusCode, $sr.ReadToEnd())
    } else { Write-Output "ERR $($_.Exception.Message)" }
  }
}

$reg = '{"identifierKind":"email","identifier":"' + $identifier + '","password":"' + $pwd + '"}'
$r = Invoke-RestMethod -Uri "$base/v1/auth/register" -Method POST -ContentType 'application/json' -Body $reg -TimeoutSec 30
Write-Output "register 201 userId=$($r.userId)"
$l = Invoke-RestMethod -Uri "$base/v1/auth/login" -Method POST -ContentType 'application/json' -Body $reg -TimeoutSec 30
$token = $l.accessToken
Write-Output "login 200 sessionId=$($l.sessionId)"

$H = @{ 'Authorization' = "Bearer $token"; 'Accept-Language' = 'fa' }
$cart = Invoke-RestMethod -Uri "$base/v1/storefront/cart" -Method POST -Headers $H -ContentType 'application/json' -Body '{}' -TimeoutSec 30
$gs = $cart.guestSecret
$H2 = @{ 'Authorization' = "Bearer $token"; 'Accept-Language' = 'fa'; 'X-Tooba-Guest-Secret' = $gs }
Write-Output "cart $($cart.cartId) version=$($cart.version) currency=$($cart.defaultCurrency)"

Show 'add line' {
  $lineBody = '{"offerId":"' + $offerId + '","quantity":1,"currency":"IRR"}'
  $H3 = $H2.Clone(); $H3['X-Tooba-Cart-Version'] = [string]$cart.version
  $line = Invoke-RestMethod -Uri "$base/v1/storefront/cart/$($cart.cartId)/lines" -Method POST -Headers $H3 -ContentType 'application/json' -Body $lineBody -TimeoutSec 60
  Write-Output "added version=$($line.version) itemCount=$($line.itemCount) lines=$($line.lines.Count)"
  $script:cartVersion = $line.version
}
if (-not $script:cartVersion) {
  Show 're-read cart' {
    $again = Invoke-RestMethod -Uri "$base/v1/storefront/cart/$($cart.cartId)" -Headers $H2 -TimeoutSec 30
    Write-Output "cart version=$($again.version) itemCount=$($again.itemCount) lines=$($again.lines.Count)"
    $script:cartVersion = $again.version
  }
}

$before = '{"cartId":"' + $cart.cartId + '","provinceName":"البرز","methodCode":null,"language":"fa"}'
Show 'projection BEFORE (legacy province only, old-client shape)' {
  $p = Invoke-RestMethod -Uri "$base/v1/storefront/shipping/projection" -Method POST -Headers $H2 -ContentType 'application/json' -Body $before -TimeoutSec 60
  Write-Output "methods=$($p.methods.Count) codes=$((($p.methods | ForEach-Object { $_.methodCode + ':' + $_.priceAmount }) -join ','))"
  Write-Output "selected=$($p.selectedMethodCode) amount=$($p.selectedShippingAmount) min=$($p.minimumDeliveryDate) subtotal=$($p.subtotalExclusiveOfTax)"
  Write-Output "provinces=$($p.provinces.Count)"
  $script:beforeJson = $p | ConvertTo-Json -Depth 8 -Compress
}

$after = '{"cartId":"' + $cart.cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"e3451206-e503-51a8-91ad-afa9ab70e330","administrativeAreaId":"1f8050a8-c325-59fa-959f-82d76b5b2bc5","cityId":"68255340-c69f-5d20-bf33-e8e0b24edde9"},"methodCode":null,"language":"fa"}'
Show 'projection AFTER (validated country+area+city ids from the live public Geography read)' {
  $p = Invoke-RestMethod -Uri "$base/v1/storefront/shipping/projection" -Method POST -Headers $H2 -ContentType 'application/json' -Body $after -TimeoutSec 60
  Write-Output "methods=$($p.methods.Count) codes=$((($p.methods | ForEach-Object { $_.methodCode + ':' + $_.priceAmount }) -join ','))"
  Write-Output "selected=$($p.selectedMethodCode) amount=$($p.selectedShippingAmount) min=$($p.minimumDeliveryDate) subtotal=$($p.subtotalExclusiveOfTax)"
  Write-Output "provinces=$($p.provinces.Count)"
  $script:afterJson = $p | ConvertTo-Json -Depth 8 -Compress
}

$invalid = '{"cartId":"' + $cart.cartId + '","provinceName":"البرز","cityName":"کرج","destination":{"countryId":"e3451206-e503-51a8-91ad-afa9ab70e330","administrativeAreaId":"1f8050a8-c325-59fa-959f-82d76b5b2bc5","cityId":"00000000-0000-4000-8000-000000000999"},"methodCode":null,"language":"fa"}'
Show 'projection with MALFORMED city id (must not become a trusted identity)' {
  $p = Invoke-RestMethod -Uri "$base/v1/storefront/shipping/projection" -Method POST -Headers $H2 -ContentType 'application/json' -Body $invalid -TimeoutSec 60
  Write-Output "methods=$($p.methods.Count) selected=$($p.selectedMethodCode) amount=$($p.selectedShippingAmount)"
}

Write-Output ""
Write-Output "--- legacy quote invariance (BEFORE vs AFTER) ---"
if ($script:beforeJson -and $script:afterJson) {
  Write-Output ("identical projection payload: {0}" -f ($script:beforeJson -eq $script:afterJson))
  if ($script:beforeJson -ne $script:afterJson) {
    Write-Output "BEFORE=$($script:beforeJson.Substring(0,[Math]::Min(600,$script:beforeJson.Length)))"
    Write-Output "AFTER =$($script:afterJson.Substring(0,[Math]::Min(600,$script:afterJson.Length)))"
  }
}

Write-Output ""
Write-Output "--- task-owned artifacts ---"
Write-Output "disposable customer: $identifier (created for this proof only)"
Write-Output "disposable cart: $($cart.cartId)"
Write-Output "no order placed, no payment made"
