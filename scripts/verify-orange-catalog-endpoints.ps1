$api = "https://api-nanchesoft-prod-hgfhbbfeetesdmef.mexicocentral-01.azurewebsites.net"
$routes = @(
  "/api/products/colors",
  "/api/products/manufacturing-types",
  "/api/products/toe-caps",
  "/api/products/sole-colors",
  "/api/products/dies",
  "/api/products/quality-control-dies",
  "/api/products/folio-patterns"
)
foreach ($r in $routes) {
  try {
    $res = Invoke-WebRequest -Uri ($api + $r) -Method GET -UseBasicParsing
    Write-Host "$r => $($res.StatusCode) OK" -ForegroundColor Green
  } catch {
    Write-Host "$r => ERROR $($_.Exception.Message)" -ForegroundColor Red
  }
}
