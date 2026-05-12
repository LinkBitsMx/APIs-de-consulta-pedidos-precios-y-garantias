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
{
  "pedidoId": "78701",
  "paqueteria": "PAQUETEXPRESS",
  "guia": "MEX14PP0067946003003",
  "trackingUrl": "https://www.paquetexpress.com.mx/rastreo/MEX14PP0067946003003",
  "estatusEnvio": "activo",
  "fechaPedido": "2026-04-03T10:51:46.06"
}
```

## API 4: Precios de Producto

```
GET /api/precios/productos/{codigo|sku}
```

**Respuesta (200):**

```json
{
  "productoId": "555302",
  "codigo": "000001",
  "nombre": "FREIDORA DE AIRE FDA07",
  "sku": "FDA07",
  "precioMayoreo": 560.0,
  "precioCaja": 560.0,
  "moneda": "MXN",
  "incluyeIva": true
}
```

## API 5: Consulta de Garantía

```
GET /api/garantias/{folioTicket}
```

**Respuesta (200):**

```json
{
  "folioTicket": "CSLP2A101255-D20260506-ID8599",
  "producto": "PANTALLA LED Magnatron 3.9",
  "fechaIngreso": "2026-05-06T17:31:55.35",
  "resultado": "No reparado",
  "estatus": "rechazada"
}
```

**Valores posibles de `estatus`:**

- `pendiente`
- `en_revision`
- `en_proceso`
- `aprobada`
- `rechazada`
- `nota_de_credito`

## Testing

```powershell
.\test-apis.ps1
```

**Resultado:** 25/25 pruebas pasando

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
