# ApisConsulta - APIs de Consulta BambooERP

API REST para consultar información del sistema BambooERP desde canales externos.

## APIs Disponibles

- **API 1:** Consulta de Pedidos
- **API 2:** Estatus de Pedidos
- **API 3:** Consulta de Envíos
- **API 4:** Precios de Productos
- **API 5:** Garantías
- **API 6:** Pre-órdenes (alta de órdenes sin confirmar)

## Requisitos

- .NET 8.0 SDK
- SQL Server con BD BambooERP

## Ejecución

```bash
cd ApisConsulta.Api
dotnet run
```

Servidor: `http://localhost:5200`  
Swagger: `http://localhost:5200/swagger`

## Autenticación

Todas las APIs requieren uno de estos métodos:

**API Key:**

```
X-API-Key: <tu-api-key>
```

**JWT (Login):**

```
POST /api/auth/login
{
  "username": "usuario",
  "password": "contraseña"
}
```

## API 1: Consulta de Pedido

```
GET /api/pedidos/{folio}
```

**Respuesta (200):**

```json
{
  "pedidoId": 78832,
  "folio": "2605-00005",
  "cliente": "JESUS ADIEL DOMINGO MONSIVAIS",
  "fecha": "2026-05-08T16:27:21.947",
  "total": 450.0,
  "estatus": "ACTIVO"
}
```

## API 2: Estatus de Pedido

```
GET /api/pedidos/{folio}/estatus
```

**Respuesta (200):**

```json
{
  "pedidoId": 78832,
  "estatus": "En proceso de surtido",
  "fechaEstatus": "2026-05-08T16:27:54.58"
}
```

**Valores posibles de `estatus`:**

- `En proceso de cotizacion`
- `Se mando a surtir al CEDIS`
- `En proceso de surtido`
- `En proceso de empacado o revision`
- `En proceso de guias de envio`
- `Entregado`
- `Cancelado`
- `Desconocido`

## API 3: Consulta de Envío

```
GET /api/envios/{folio}
```

**Respuesta (200):**

```json
[
  {
    "pedidoId": "25086",
    "paqueteria": "FLETERA",
    "guias": [
      {
        "guia": "Sin guia",
        "trackingUrl": null
      }
    ],
    "estatusEnvio": "activo",
    "fechaPedido": "2025-09-18T12:35:39.553"
  },
  {
    "pedidoId": "25087",
    "paqueteria": "PAQUETEXPRESS",
    "guias": [
      {
        "guia": "MEX01PP3469501006006",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX01PP3469501006006"
      },
      {
        "guia": "MEX01PP3469501006005",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX01PP3469501006005"
      },
      {
        "guia": "MEX01PP3469501006004",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX01PP3469501006004"
      },
      {
        "guia": "MEX01PP3469501006003",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX01PP3469501006003"
      },
      {
        "guia": "MEX01PP3469501006002",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX01PP3469501006002"
      },
      {
        "guia": "MEX01PP3469501006001",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX01PP3469501006001"
      }
    ],
    "estatusEnvio": "activo",
    "fechaPedido": "2025-09-18T12:35:39.557"
  }
]
```

## API 4: Precios de Producto

```
GET /api/precios/productos/{codigo|sku}
```

**Respuesta (200):**

La respuesta incluye los precios del producto separados por sucursal.

```json
{
  "productoId": "555302",
  "codigo": "000001",
  "nombre": "FREIDORA DE AIRE FDA07",
  "sku": "FDA07",
  "preciosPorSucursal": [
    {
      "sucursal": "México",
      "precioMayoreo": 560.0,
      "precioCaja": 560.0,
      "moneda": "MXN",
      "incluyeIva": true
    },
    {
      "sucursal": "Sucursal Monterrey",
      "precioMayoreo": 560.0,
      "precioCaja": 560.0,
      "moneda": "MXN",
      "incluyeIva": true
    }
  ]
}
```

## API 5: Consulta de Garantía

```
GET /api/garantias/{folioTicket}
```

**Respuesta (200):**

La respuesta incluye todos los productos asociados al folio de garantía.

```json
{
  "folioTicket": "CMIC1E101898-D20260401-ID8503",
  "productos": [
    {
      "producto": "BOCINA KTS-1853",
      "fechaIngreso": "2026-04-01T10:07:13.94",
      "resultado": "No reparado",
      "estatus": "finalizado"
    },
    {
      "producto": "FOCO LED S48W02",
      "fechaIngreso": "2026-04-01T10:07:13.94",
      "resultado": "No reparado",
      "estatus": "finalizado"
    },
    {
      "producto": "TIRA LED T2835RGB06",
      "fechaIngreso": "2026-04-01T10:07:13.94",
      "resultado": "No reparado",
      "estatus": "finalizado"
    }
  ]
}
```

**Valores posibles de `estatus`:**

- `pendiente`
- `revision`
- `activo`
- `finalizado`

**Valores posibles de `resultado`:**

- `Reparado`
- `No reparado`
- `No aplica`
- `Otros`
- `Nota de credito`

## API 6: Pre-órdenes

Permite que un sistema externo de clientes envíe **pre-órdenes** (órdenes sin
confirmar). Quedan en estatus `PENDIENTE` hasta que el vendedor las toma y las
convierte en una cotización.

> Requiere ejecutar una sola vez el script `database/request_quotation.sql` sobre
> la BD BambooERP para crear las tablas `request_quotation` y `request_quotation_items`.

### Crear pre-orden

```
POST /api/preordenes
```

**Body:**

```json
{
  "customerCode": "C00123",
  "email": "compras@cliente.com",
  "phone": "8112345678",
  "notes": "Entregar en horario matutino",
  "items": [
    { "productCode": "000001", "quantity": 2, "unitPrice": 560.0 },
    { "productCode": "FDA07", "quantity": 5, "unitPrice": 120.0 }
  ]
}
```

`customerCode` e `items` (mínimo uno) son obligatorios. El `total` se calcula en el
servidor a partir de `quantity * unitPrice` de cada item. La respuesta incluye un
`folio` legible (`customerCode` + id, ej. `C00123-00012`) generado por el servidor
para identificar la solicitud.

**Respuesta (201 Created):**

```json
{
  "id": 12,
  "folio": "C00123-00012",
  "customerCode": "C00123",
  "email": "compras@cliente.com",
  "phone": "8112345678",
  "notes": "Entregar en horario matutino",
  "status": "PENDIENTE",
  "isApproved": false,
  "total": 1720.0,
  "createdAt": "2026-06-26T10:15:00.000",
  "items": [
    { "id": 31, "productCode": "000001", "quantity": 2, "unitPrice": 560.0, "amount": 1120.0 },
    { "id": 32, "productCode": "FDA07", "quantity": 5, "unitPrice": 120.0, "amount": 600.0 }
  ]
}
```

### Listar pre-órdenes

```
GET /api/preordenes
GET /api/preordenes?status=PENDIENTE
```

**Respuesta (200):**

```json
[
  {
    "id": 12,
    "folio": "C00123-00012",
    "customerCode": "C00123",
    "status": "PENDIENTE",
    "isApproved": false,
    "total": 1720.0,
    "totalItems": 2,
    "createdAt": "2026-06-26T10:15:00.000"
  }
]
```

### Detalle de pre-orden

```
GET /api/preordenes/{id}
```

Devuelve la pre-orden con sus items (mismo formato que la respuesta de creación).

**Valores posibles de `status`:** `PENDIENTE`, `TOMADA`, `CONVERTIDA`, `CANCELADA`

## Testing

```powershell
.\test-apis.ps1
```

## Errores Comunes

- **401 Unauthorized:** Falta `X-API-Key` o JWT inválido
- **404 Not Found:** Folio/código no existe en BD
- **500 Internal Server:** Revisar logs en consola

## Configuración

Editar `ApisConsulta.Api/appsettings.json` con los siguientes valores:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=...;Database=BambooERP;User Id=...;Password=...;"
  },
  "ApiKey": {
    "ApiKey": "<api-key>"
  },
  "JwtSettings": {
    "SecretKey": "<secret-key-minimo-32-caracteres>",
    "Issuer": "ApisConsulta",
    "Audience": "ApisConsultaClients",
    "ExpirationMinutes": 60
  }
}
```

## Estructura

- **ApisConsulta.Api:** Controladores REST + Autenticación
- **ApisConsulta.Application:** CQRS con MediatR + DTOs
- **ApisConsulta.Infrastructure:** Acceso a datos (SQL queries contra BambooERP)
