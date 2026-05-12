# Script de Pruebas - ApisConsulta v1
# Muestra informacion completa de cada API
# Ejecutar: .\test-apis.ps1

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "        PRUEBAS DE APIS - ApisConsulta" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""

$baseUrl = "http://localhost:5200"
$apiKey = "<tu-api-key>"   # Reemplazar con la API Key del appsettings.json
$headers = @{ "X-API-Key" = $apiKey }

$totalTests = 0
$passedTests = 0
$failedTests = 0

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Method,
        [string]$Uri,
        [int]$ExpectedStatus,
        [hashtable]$Headers = @{},
        [string]$Description = ""
    )

    $global:totalTests++
    Write-Host "[$($global:totalTests)] $Name" -ForegroundColor Yellow
    if ($Description) { Write-Host "     $Description" -ForegroundColor Gray }
    Write-Host "     Request: $Method $Uri" -ForegroundColor Gray

    $statusCode = 0
    $body = ""

    try {
        $response = Invoke-WebRequest -Uri $Uri -Method $Method -Headers $Headers -UseBasicParsing
        $statusCode = $response.StatusCode
        $body = $response.Content
    }
    catch {
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode.value__
            try {
                $body = $_.Exception.Response.Content.ReadAsStream() | ForEach-Object { [System.IO.StreamReader]::new($_).ReadToEnd() }
            }
            catch {
                $body = ""
            }
        }
        else {
            $statusCode = 0
            $body = ""
        }
    }

    Write-Host "     Status: $statusCode" -ForegroundColor $(if ($statusCode -eq $ExpectedStatus) { "Green" } else { "Red" })

    if ($body) {
        try {
            $json = $body | ConvertFrom-Json
            Write-Host "     Response:"
            $json | ConvertTo-Json | ForEach-Object { Write-Host "       $_" -ForegroundColor White }
        }
        catch {
            Write-Host "     Response: $body" -ForegroundColor White
        }
    }

    if ($statusCode -eq $ExpectedStatus) {
        $global:passedTests++
    }
    else {
        Write-Host "     Error: esperado $ExpectedStatus" -ForegroundColor Red
        $global:failedTests++
    }
    Write-Host ""
}

# ==================== AUTENTICACION ====================
Write-Host ""
Write-Host "AUTENTICACION" -ForegroundColor Magenta
Write-Host "Usando API Key para autenticacion..." -ForegroundColor Gray
Write-Host "--------------------------------------------------" -ForegroundColor Gray
Write-Host "API Key configurada: $($apiKey.Substring(0, 15))..." -ForegroundColor Green
Write-Host "Header X-API-Key sera incluido en todas las pruebas" -ForegroundColor Green
Write-Host ""

# ==================== API 1: PEDIDOS ====================
Write-Host ""
Write-Host "API 1: CONSULTA DE PEDIDOS" -ForegroundColor Magenta
Write-Host "Endpoint: GET /api/pedidos/{folio}" -ForegroundColor Gray
Write-Host "--------------------------------------------------" -ForegroundColor Gray

Test-Endpoint -Name "Pedido existente" `
    -Method GET -Uri "$baseUrl/api/pedidos/2605-00005" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Prueba con folio valido en BD"

Test-Endpoint -Name "Pedido no encontrado" `
    -Method GET -Uri "$baseUrl/api/pedidos/9999-99999" `
    -ExpectedStatus 404 -Headers $headers `
    -Description "Folio inexistente"

Test-Endpoint -Name "Sin autenticacion" `
    -Method GET -Uri "$baseUrl/api/pedidos/2605-00005" `
    -ExpectedStatus 401 -Headers @{} `
    -Description "Debe rechazar sin API Key"

# ==================== API 2: ESTATUS ====================
Write-Host ""
Write-Host "API 2: ESTATUS DE PEDIDOS" -ForegroundColor Magenta
Write-Host "Endpoint: GET /api/pedidos/{folio}/estatus" -ForegroundColor Gray
Write-Host "--------------------------------------------------" -ForegroundColor Gray

Test-Endpoint -Name "Estatus: En proceso de surtido" `
    -Method GET -Uri "$baseUrl/api/pedidos/2605-00005/estatus" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Sin procesar -> En proceso de surtido"

Test-Endpoint -Name "Estatus: Se mando a surtir al CEDIS" `
    -Method GET -Uri "$baseUrl/api/pedidos/2604-00271/estatus" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Pago Validado -> Se mando a surtir al CEDIS"

Test-Endpoint -Name "Estatus: Cancelado" `
    -Method GET -Uri "$baseUrl/api/pedidos/2505-00021/estatus" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Cancelado -> Cancelado"

Test-Endpoint -Name "Estatus: Se mando a surtir (Pago no valido)" `
    -Method GET -Uri "$baseUrl/api/pedidos/2505-00056/estatus" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Pago no valido -> Se mando a surtir al CEDIS"

Test-Endpoint -Name "Estatus: Se mando a surtir (Pago sin proceso)" `
    -Method GET -Uri "$baseUrl/api/pedidos/2512-02093/estatus" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Pago sin proceso -> Se mando a surtir al CEDIS"

Test-Endpoint -Name "Estatus pedido no encontrado" `
    -Method GET -Uri "$baseUrl/api/pedidos/9999-99999/estatus" `
    -ExpectedStatus 404 -Headers $headers `
    -Description "Folio inexistente"

Test-Endpoint -Name "Sin autenticacion" `
    -Method GET -Uri "$baseUrl/api/pedidos/2605-00005/estatus" `
    -ExpectedStatus 401 -Headers @{} `
    -Description "Debe rechazar sin API Key"

# ==================== API 3: ENVIOS ====================
Write-Host ""
Write-Host "API 3: CONSULTA DE ENVIOS" -ForegroundColor Magenta
Write-Host "Endpoint: GET /api/envios/{folio}" -ForegroundColor Gray
Write-Host "--------------------------------------------------" -ForegroundColor Gray

Test-Endpoint -Name "Envio PAQUETEXPRESS" `
    -Method GET -Uri "$baseUrl/api/envios/2604-00271" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Pedido con paqueteria PAQUETEXPRESS (guia: MEX14PP0067946003003)"

Test-Endpoint -Name "Envio SENDEX" `
    -Method GET -Uri "$baseUrl/api/envios/2602-01185" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Pedido con paqueteria SENDEX (guia: 411995900015)"

Test-Endpoint -Name "Envio no encontrado" `
    -Method GET -Uri "$baseUrl/api/envios/9999-99999" `
    -ExpectedStatus 404 -Headers $headers `
    -Description "Folio inexistente"

Test-Endpoint -Name "Sin autenticacion" `
    -Method GET -Uri "$baseUrl/api/envios/2604-00271" `
    -ExpectedStatus 401 -Headers @{} `
    -Description "Debe rechazar sin API Key"

# ==================== API 4: PRECIOS ====================
Write-Host ""
Write-Host "API 4: CONSULTA DE PRECIOS" -ForegroundColor Magenta
Write-Host "Endpoint: GET /api/precios/productos/{codigo o sku}" -ForegroundColor Gray
Write-Host "--------------------------------------------------" -ForegroundColor Gray

Test-Endpoint -Name "Precio por codigo" `
    -Method GET -Uri "$baseUrl/api/precios/productos/000001" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Buscar producto por codigo"

Test-Endpoint -Name "Precio por sku" `
    -Method GET -Uri "$baseUrl/api/precios/productos/FDA07" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "Buscar producto por sku (spec)"

Test-Endpoint -Name "Producto no encontrado" `
    -Method GET -Uri "$baseUrl/api/precios/productos/NOTEXIST" `
    -ExpectedStatus 404 -Headers $headers `
    -Description "Codigo/Barcode inexistente"

Test-Endpoint -Name "Sin autenticacion" `
    -Method GET -Uri "$baseUrl/api/precios/productos/000001" `
    -ExpectedStatus 401 -Headers @{} `
    -Description "Debe rechazar sin API Key"

# ==================== API 5: GARANTIAS ====================
Write-Host ""
Write-Host "API 5: CONSULTA DE GARANTIAS" -ForegroundColor Magenta
Write-Host "Endpoint: GET /api/garantias/{folioTicket}" -ForegroundColor Gray
Write-Host "--------------------------------------------------" -ForegroundColor Gray

Test-Endpoint -Name "Garantia: rechazada (No reparado)" `
    -Method GET -Uri "$baseUrl/api/garantias/CSLP2A101255-D20260506-ID8599" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "FINALIZADO + No reparado -> rechazada"

Test-Endpoint -Name "Garantia: pendiente" `
    -Method GET -Uri "$baseUrl/api/garantias/CAAA1H100504-D20251021-ID2731" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "PENDIENTE -> pendiente"

Test-Endpoint -Name "Garantia: en_revision" `
    -Method GET -Uri "$baseUrl/api/garantias/CCMX1B101582-D20250906-ID1032" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "REVISION -> en_revision"

Test-Endpoint -Name "Garantia: aprobada (Reparado)" `
    -Method GET -Uri "$baseUrl/api/garantias/CVER1B100387-D20250806-ID2" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "FINALIZADO + Reparado -> aprobada"

Test-Endpoint -Name "Garantia: nota_de_credito" `
    -Method GET -Uri "$baseUrl/api/garantias/CNLN2A101407-D20250806-ID27" `
    -ExpectedStatus 200 -Headers $headers `
    -Description "FINALIZADO + Nota de credito -> nota_de_credito"

Test-Endpoint -Name "Garantia no encontrada" `
    -Method GET -Uri "$baseUrl/api/garantias/FALSO-FALSO-FALSO" `
    -ExpectedStatus 404 -Headers $headers `
    -Description "FolioTicket inexistente"

Test-Endpoint -Name "Sin autenticacion" `
    -Method GET -Uri "$baseUrl/api/garantias/CSLP2A101255-D20260506-ID8599" `
    -ExpectedStatus 401 -Headers @{} `
    -Description "Debe rechazar sin API Key"

Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "Fin de pruebas" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "RESUMEN DE PRUEBAS" -ForegroundColor Cyan
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host "Total de pruebas:   $($global:totalTests)" -ForegroundColor White
Write-Host "Pruebas exitosas:   $($global:passedTests)" -ForegroundColor Green
Write-Host "Pruebas fallidas:   $(if ($null -eq $global:failedTests) { 0 } else { $global:failedTests })" -ForegroundColor $(if ($global:failedTests -eq 0 -or $null -eq $global:failedTests) { "Green" } else { "Red" })
Write-Host "=====================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Servidor ejecutandose en: $baseUrl" -ForegroundColor Cyan
Write-Host ""
