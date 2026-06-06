# ApisConsulta - APIs de Consulta BambooERP

API REST para consultar información del sistema BambooERP desde canales externos.

## APIs Disponibles

- **API 1:** Consulta de Pedidos
- **API 2:** Estatus de Pedidos
- **API 3:** Consulta de Envíos
- **API 4:** Precios de Productos
- **API 5:** Garantías

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
    "pedidoId": "78701",
    "paqueteria": "PAQUETEXPRESS",
    "guias": [
      {
        "guia": "MEX14PP0067946003003",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX14PP0067946003003"
      }
    ],
    "estatusEnvio": "activo",
    "fechaPedido": "2026-04-03T10:51:46.06"
  },
  {
    "pedidoId": "78702",
    "paqueteria": "PAQUETEXPRESS",
    "guias": [
      {
        "guia": "MEX14PP0067946003004",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX14PP0067946003004"
      },
      {
        "guia": "MEX14PP0067946003005",
        "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX14PP0067946003005"
      }
    ],
    "estatusEnvio": "activo",
    "fechaPedido": "2026-04-03T10:52:10.12"
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
