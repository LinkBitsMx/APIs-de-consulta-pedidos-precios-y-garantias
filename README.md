# ApisConsulta - APIs de Consulta BambooERP

API REST para consultar información del sistema BambooERP desde canales externos.

## APIs Disponibles

- **API 1:** Consulta de Pedidos
- **API 2:** Estatus de Pedidos
- **API 3:** Consulta de Envíos
- **API 4:** Precios de Productos
- **API 5:** Garantías
- **API 6:** Pre-órdenes (alta de órdenes sin confirmar)
- **API 7:** Sales (detalle, totales y estatus por almacén — endpoints en inglés)
- **API 8:** Payments (alta y consulta de pagos, con filtro por estatus — endpoints en inglés)

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

### Detalle de todas las pre-órdenes (endpoint en inglés)

```
GET /api/preorders/detail
GET /api/preorders/detail?status=PENDING
```

> Este endpoint está **en inglés** (rutas, campos y valores de estado) porque lo revisa
> el equipo de China. El resto de endpoints de pre-órdenes sigue en español.

Devuelve **todas** las pre-órdenes (filtrables por estatus) con el mismo detalle
enriquecido por item que el endpoint de detalle individual (existencias por almacén,
cantidad cubierta/faltante, estado de surtido y reparto sugerido). El stock de todos los
items se resuelve en una sola consulta.

**Filtro `status`** (inglés): `PENDING`, `TAKEN`, `CONVERTED`, `CANCELLED`.

Cada item incluye:

- `availableStock`: stock entregable sumando todos los almacenes de venta.
- `coveredQuantity`: cuánto de lo pedido sí se cubre.
- `shortageQuantity`: cuánto **no** se cubre (faltante).
- `fulfillmentStatus`: `COVERED`, `DISTRIBUTE`, `PARTIALLY_COVERED` u `OUT_OF_STOCK`.
- `warehouses[]`: solo almacenes con stock, ordenados de mayor a menor, cada uno con
  `availableStock` y `quantityToFulfill` (reparto sugerido).

**Respuesta (200):**

```json
[
  {
    "id": 21,
    "folio": "NLN2A101766-00021",
    "customerCode": "NLN2A101766",
    "status": "PENDING",
    "isApproved": false,
    "total": 153190.0,
    "createdAt": "2026-07-27T09:40:00.000",
    "items": [
      {
        "id": 84,
        "productCode": "001135",
        "quantity": 40,
        "unitPrice": 929.0,
        "amount": 37160.0,
        "availableStock": 1710,
        "coveredQuantity": 40,
        "shortageQuantity": 0,
        "fulfillmentStatus": "COVERED",
        "warehouses": [
          { "warehouseId": 1540424, "warehouse": "Almacen San Martin", "availableStock": 819, "quantityToFulfill": 40 },
          { "warehouseId": 1540418, "warehouse": "Cedis Vallejo",       "availableStock": 283, "quantityToFulfill": 0 }
        ]
      }
    ]
  }
]
```

### Detalle de una pre-orden

```
GET /api/preordenes/{id}
```

Devuelve la pre-orden con sus items. Además de los datos base, **cada item incluye
el desglose de existencias por almacén de venta** (`sales_enabled = 1`), tomando el
stock entregable (`deliverable_qty`):

- `stockDisponible`: piezas entregables sumando todos los almacenes de venta.
- `cantidadCubierta`: cuánto de lo pedido sí alcanza a cubrirse.
- `cantidadAgotada`: cuánto **no** alcanza (faltante/agotado).
- `estadoSurtido`: `CUBIERTA` (un solo almacén cubre todo), `DISTRIBUIR` (hay stock
  suficiente pero repartido en varios almacenes), `AGOTADO_PARCIAL` (falta stock) o
  `SIN_STOCK` (ningún almacén tiene existencia).
- `almacenes[]`: solo almacenes con stock, ordenados de mayor a menor. Cada uno trae
  `stockDisponible` y `cantidadSurtir` (reparto sugerido, greedy del que más tiene al
  que menos).

**Respuesta (200):**

```json
{
  "id": 16,
  "folio": "VER1B100210-00016",
  "customerCode": "VER1B100210",
  "status": "PENDIENTE",
  "isApproved": false,
  "total": 28572.0,
  "createdAt": "2026-07-20T17:57:55.87",
  "items": [
    {
      "id": 66,
      "productCode": "001104",
      "quantity": 36,
      "unitPrice": 182.0,
      "amount": 6552.0,
      "stockDisponible": 2244,
      "cantidadCubierta": 36,
      "cantidadAgotada": 0,
      "estadoSurtido": "CUBIERTA",
      "almacenes": [
        { "almacenId": 1540424, "almacen": "Almacen San Martin",    "stockDisponible": 1236, "cantidadSurtir": 36 },
        { "almacenId": 1540421, "almacen": "Almacen Gonzalez Gallo", "stockDisponible": 900,  "cantidadSurtir": 0 },
        { "almacenId": 1540457, "almacen": "Almacen Ramon Corona",   "stockDisponible": 108,  "cantidadSurtir": 0 }
      ]
    },
    {
      "id": 65,
      "productCode": "000411",
      "quantity": 60,
      "unitPrice": 83.0,
      "amount": 4980.0,
      "stockDisponible": 0,
      "cantidadCubierta": 0,
      "cantidadAgotada": 60,
      "estadoSurtido": "SIN_STOCK",
      "almacenes": []
    }
  ]
}
```

**Valores posibles de `status`:** `PENDIENTE`, `TOMADA`, `CONVERTIDA`, `CANCELADA`

## API 7: Sales

Detailed query of the sales in the system: header, customer, totals, invoicing, the
line-by-line detail and —the important part— **the status of each warehouse**.

> This API is **in English** (routes, fields and status values) because it is handled by
> the China team, same as `GET /api/preorders/detail`. The rest of the endpoints stay in
> Spanish.

### How a sale is modelled in BambooERP

- The sale lives in `quotation` (header, totals and **overall status**) and its detail in
  `quotation_detail`, where every line holds the warehouse fulfilling it.
- While the sale is a **quotation**, that overall status is the only one there is:
  `isQuotation: true` and `warehouses[]` comes back empty.
- Once the quotation is validated **the order is split and each warehouse keeps its own
  status** (`startnet_sales_orders_picking_assignment`, one row per sale and warehouse).
  At that point `isQuotation` turns `false` and `warehouses[]` carries the current status
  of each one.

In practice the sales not split yet sit in status `Sin procesar` and the split ones in
`Pago Validado`, but the `isQuotation` flag is computed from the actual existence of
per-warehouse orders, not from the status name.

Every status is returned twice: `statusRaw` is the name exactly as stored in BambooERP
(in Spanish) and `status` is that same value normalized to the English stages below.

**Possible values of `status`:**

- `IN_QUOTATION`
- `SENT_TO_CEDIS`
- `IN_PICKING`
- `IN_PACKING_OR_REVIEW`
- `IN_SHIPPING_LABEL`
- `DELIVERED`
- `CANCELLED`
- `UNKNOWN`

### List sales

```
GET /api/sales
GET /api/sales?startDate=2026-07-01&endDate=2026-07-31
GET /api/sales?customerCode=SLP2A101255&page=1&pageSize=50
```

**Filters (all optional):**

| Parameter | Description |
| --- | --- |
| `startDate` / `endDate` | Range over the sale date. If `endDate` comes without a time part, the whole day is taken. |
| `customerCode` | Exact customer code. |
| `folio` | Partial match on the folio. |
| `statusId` | Estatus **general** de la venta (`quotation.status_id`). Solo toma 5 valores: `1` Sin procesar, `27` Pago Validado, `23` Cancelado, `28` Pago no valido, `29` Pago sin proceso. |
| `warehouseStatusId` | Estatus de las **órdenes por almacén**: deja las ventas donde al menos un almacén está hoy en ese estatus. Ej. `21` (Recolectado). |
| `warehouseId` | Only sales with lines fulfilled by that warehouse. |
| `includePayments` | `true` agrega `payments[]` y `paymentsTotal` a cada venta del listado. Default `false`. |
| `branchCode` | Branch code (`starnet_branches.code`). Example: `801.10.02` |
| `onlyQuotations` | `true` keeps only the ones still in quotation. |
| `page` / `pageSize` | Paging. Default 50, maximum 200. |

**Response (200):**

```json
{
  "page": 1,
  "pageSize": 50,
  "totalRecords": 46,
  "totalPages": 1,
  "sales": [
    {
      "saleId": 78989,
      "folio": "2607-00037",
      "date": "2026-07-21T15:58:26.343",
      "customerCode": "SLP2A101255",
      "customer": "JESUS ADIEL DOMINGO MONSIVAIS",
      "branchCode": "801.10.02",
      "branch": "Sucursal Florida",
      "sellerId": 167,
      "seller": "Fernando Dominguez Garcia",
      "statusId": 27,
      "statusRaw": "Pago Validado",
      "status": "SENT_TO_CEDIS",
      "isQuotation": false,
      "units": 7600,
      "totalLines": 7,
      "total": 130997.0,
      "warehouses": [
        {
          "warehouseId": 1540420,
          "warehouse": "Cedis Motevideo",
          "statusId": 17,
          "statusRaw": "Guia en proceso",
          "status": "IN_SHIPPING_LABEL"
        },
        {
          "warehouseId": 1540418,
          "warehouse": "Cedis Vallejo",
          "statusId": 11,
          "statusRaw": "Empacado sin procesar",
          "status": "IN_PACKING_OR_REVIEW"
        }
      ]
    }
  ]
}
```

### Sale detail

```
GET /api/sales/{folio}
```

On top of the header and the full detail, `warehouses[]` summarizes each per-warehouse
order (status, units, lines and amount it accounts for) and every item states the
warehouse fulfilling it along with the status of that order.

**Sucursal y departamento:** la venta llega a su sucursal a través del departamento —
`quotation.DepartamentoId` → `departments.branchId` → `starnet_branches.id`. El filtro
`branchCode` usa `starnet_branches.code` (ej. `801.10.02`). El listado devuelve
`branchCode` y `branch`; el detalle agrega el objeto `branch` (id, código, nombre) y el
`department` del que proviene (id, código, nombre, zona).

De 59,324 ventas, 109 no resuelven sucursal (20 sin departamento y 89 con un
`DepartamentoId` que ya no existe en `departments`); en esas, `branch` y `department`
regresan `null`.

> **`statusId` vs `warehouseStatusId`:** las etapas de surtido (`21` Recolectado, `18`
> Guia Generada, `15` Empacado Finalizado…) **nunca aparecen en `quotation.status_id`** —
> viven en las órdenes por almacén. Filtrar `statusId=21` devuelve siempre 0 ventas; para
> eso se usa `warehouseStatusId=21`.

**Vendedor:** `quotation.usuarioId` → `catUsers`. El listado devuelve `sellerId` y
`seller` (nombre completo); el detalle agrega código de vendedor, correo y usuario.

**Pagos:** el detalle incluye `payments[]` con los pagos registrados contra la venta y su
forma de pago. La relación es `quotation.id` → `rel_quotes_to_payments.Quote_id` y de ahí
**`VoucherId` → `Payments.Id`** (las filas con `VoucherId = 0` son placeholders y se
omiten). La forma de pago sale de `sat_FormaPago` — `paymentFormCode` es el código SAT
(`01` efectivo, `03` transferencia, `17` saldo a favor…) — y el estatus de `catEstatus`,
publicado como `statusRaw` (crudo) y `status`: `VALID`, `REJECTED`, `PENDING`,
`IN_PROCESS`, `CANCELLED` o `UNKNOWN`.

`totals.paymentsTotal` es la suma de `payments[].amount`. **Puede no coincidir con
`total`**: una venta puede quedar parcialmente pagada o traer pagos registrados por
encima de su total.

**Totals:** `productsSubtotal`, `servicesTotal`, `units` and `lineDiscount` are computed
by summing the detail (lines with `isService: true` are shipping, freight, etc.);
`total`, `deliveryTotal`, `assuredTotal`, `freightCarrierTotal` and `initialTotal` are the
`total`, `total_deliver`, `total_of_assured`, `total_fletera` and `total_initial` columns
of `quotation` exactly as BambooERP stores them.

**Response (200):**

```json
{
  "saleId": 78989,
  "folio": "2607-00037",
  "date": "2026-07-21T15:58:26.343",
  "customer": {
    "customerCode": "SLP2A101255",
    "name": "JESUS ADIEL DOMINGO MONSIVAIS",
    "email": "",
    "phone": "",
    "branch": null
  },
  "seller": {
    "sellerId": 167,
    "name": "Fernando Dominguez Garcia",
    "code": "testLuisilloPillo",
    "email": "Fernando.DominguezGarcia@gmail.com",
    "username": "1369"
  },
  "branch": {
    "branchId": 1148512,
    "code": "801.10.02",
    "name": "Sucursal Florida"
  },
  "department": {
    "departmentId": 10,
    "code": "801010302",
    "name": "Sucursal Florida",
    "zone": "CDMX"
  },
  "status": {
    "statusId": 27,
    "statusRaw": "Pago Validado",
    "status": "SENT_TO_CEDIS",
    "isQuotation": false,
    "statusDate": "2026-07-21T16:37:14.49"
  },
  "totals": {
    "units": 7600,
    "totalLines": 7,
    "productsSubtotal": 129700.0,
    "servicesTotal": 6127.0,
    "lineDiscount": 0.0,
    "paymentsTotal": 135827.0,
    "deliveryTotal": 0.0,
    "assuredTotal": 1297.0,
    "freightCarrierTotal": 0.0,
    "total": 130997.0,
    "initialTotal": null,
    "hasDiscount": false
  },
  "invoicing": {
    "requiresInvoice": false,
    "invoiced": false,
    "isCredit": false,
    "isDirectSale": false
  },
  "warehouses": [
    {
      "warehouseId": 1540418,
      "warehouse": "Cedis Vallejo",
      "statusId": 11,
      "statusRaw": "Empacado sin procesar",
      "status": "IN_PACKING_OR_REVIEW",
      "assignedAt": "2026-07-21T16:26:10.67",
      "units": 5400,
      "totalLines": 3,
      "amount": 34900.0
    }
  ],
  "items": [
    {
      "itemId": 711380,
      "productCode": "000449",
      "sku": "B03W10",
      "product": "FOCO LED B03W10",
      "quantity": 5000,
      "unitPrice": 5.0,
      "discount": 0.0,
      "amount": 25000.0,
      "isService": false,
      "warehouseId": 1540418,
      "warehouse": "Cedis Vallejo",
      "warehouseStatus": "IN_PACKING_OR_REVIEW",
      "notes": null
    },
    {
      "itemId": 711385,
      "productCode": "LB00001",
      "sku": null,
      "product": "ENVIO %",
      "quantity": 1,
      "unitPrice": 1297.0,
      "discount": 0.0,
      "amount": 1297.0,
      "isService": true,
      "warehouseId": null,
      "warehouse": null,
      "warehouseStatus": null,
      "notes": null
    }
  ],
  "payments": [
    {
      "paymentId": 34049,
      "folio": "PAY-0726-000010",
      "paymentDate": "2026-07-21T00:00:00",
      "amount": 135827.0,
      "paymentFormCode": "01",
      "paymentForm": "Efectivo",
      "statusId": 29,
      "statusRaw": "Valido",
      "status": "VALID",
      "reference": null,
      "paymentType": "payment",
      "createdAt": "2026-07-21T16:36:06.54"
    }
  ]
}
```

## API 8: Payments

Registra pagos en la tabla `Payments` de BambooERP, con el mismo flujo que usa el ERP
hoy: el pago se crea **pendiente de validación** (`statusId = 4`) y el `folio` lo genera
la base de datos.

> Este endpoint está **en inglés** (rutas, campos y valores de estado) porque lo revisa
> el equipo de China.

Lo que hace la base de datos por sí sola (triggers ya existentes, la API no lo duplica):

- `trg_GenerarFolioPayments` asigna el folio con formato `PAY-MMYY-NNNNNN`
  (consecutivo por mes), por eso el `folio` no se envía en el body.
- `trg_AfterInsert_Payments_InsertRelation` relaciona el pago con la venta en
  `rel_quotes_to_payments` **solo si se envía `saleFolio`**, y en ese caso mueve la
  cotización a `status_id = 29`.

### Registrar pago

```
POST /api/payments
```

**Body:**

```json
{
  "customerCode": "SIN2A100652",
  "paymentDate": "2026-08-05",
  "bankId": 7,
  "paymentFormId": 3,
  "amount": 504.70,
  "reference": "0123456789",
  "paymentType": "payment",
  "paymentFilePath": "comprobante-1785955090.jpeg",
  "saleFolio": "2608-00012",
  "uploadedById": 426,
  "sellerId": 123,
  "comentary": "Transferencia recibida",

  "kingdeeBillNo": "SKCZD000123",
  "bizOrgId": 847244,
  "bizOrgCode": "801",
  "settleOrgId": 847244,
  "settleOrgCode": "801",
  "cashierId": 1772,
  "cashierCode": "GW000041",
  "kingdeeAccountId": 100012,
  "kingdeeAccountCode": "BANK001",
  "receiveTypeId": 5,
  "receiveTypeCode": "SKFS03",
  "settleCurrencyId": 1,
  "settleCurrencyCode": "MXN",
  "receiveCurrencyId": 1,
  "receiveCurrencyCode": "MXN",
  "exchangeRate": 1.0,
  "cardId": 5001,
  "cardNumber": "6234567890",
  "memberId": 8801,
  "memberCardNumber": "VIP00034",
  "rechargeAmount": 504.70
}
```

Obligatorios: `customerCode`, `bankId`, `paymentFormId` y `uploadedById`. El resto es
opcional:

- `paymentDate`: por defecto la fecha de hoy.
- `amount`: opcional. El ERP lo deja vacío hasta que el pago se valida, así que puede
  omitirse y capturarse en la validación.
- `paymentType`: `payment` (default), `credit` o `advance`.
- `saleFolio`: folio de la venta (`quotation.billCode`). Si se envía, el pago queda
  relacionado con esa venta y la venta pasa a estatus 29.
- `departmentId`: por defecto la sucursal del usuario de `uploadedById`
  (`catUsers.BranchID`).
- `statusId`: por defecto `4` (PENDIENTE).
- `accountId` no se envía: se deriva del `bankId` (`bancos.id_origen`), que es la empresa
  receptora del depósito.

Se validan contra la BD el cliente, la cuenta bancaria (que exista y no esté
deshabilitada), la forma de pago, el usuario, el vendedor, el departamento, el estatus y
el folio de la venta. Cualquiera que no exista devuelve `400` con el detalle.

#### Campos de Kingdee

El equipo de Kingdee necesita mandar los campos de su documento de recarga (充值单). Seis
de ellos ya salen de lo que el pago guarda hoy y **no se envían**; el resto son
identificadores propios de Kingdee que se guardan en `Payments` tal cual llegan (Bamboo no
tiene catálogo contra el cual validarlos).

| Campo Kingdee | Campo del body | De dónde sale |
|---|---|---|
| `FBillNo` | `kingdeeBillNo` | Se envía. No sustituye a `folio`, que lo sigue generando el trigger. |
| `FDate` | — | `paymentDate` |
| `FBizOrgId` / `FBizOrg` | `bizOrgId` / `bizOrgCode` | Se envía |
| `FSETTLEORGID` / `FSETTLEORG` | `settleOrgId` / `settleOrgCode` | Se envía |
| `FBranchID` / `Fbranch` | — | `departmentId` → `departments.branchId` → `starnet_branches.id` / `.code` |
| `FSalerID` / `FSaler` | — | `sellerId` → `catUsers.code_seller` (`kingdeeId_kingdeeCode_branchId`) |
| `FCashierID` / `FCashier` | `cashierId` / `cashierCode` | Se envía. Es el cajero de Kingdee, distinto de `uploadedById`. |
| `FCustomerID` / `FCustomer` | — | `customerCode` → `customers.customer_id` / `customer_code` |
| `FSETTLECURRENCYID` / `FSETTLECURRENCY` | `settleCurrencyId` / `settleCurrencyCode` | Se envía |
| `FNote` | — | `comentary` |
| `FCardID` / `FCard` | `cardId` / `cardNumber` | Se envía |
| `FMemberID` / `FMember` | `memberId` / `memberCardNumber` | Se envía |
| `FAccountID` / `FAccount` | `kingdeeAccountId` / `kingdeeAccountCode` | Se envía. La cuenta de Kingdee, no el `accountId` de la respuesta (que es la empresa receptora). |
| `FRechargeAmount` | `rechargeAmount` | Se envía |
| `FReceiveTypeID` / `FReceiveType` | `receiveTypeId` / `receiveTypeCode` | Se envía. La forma de cobro de Kingdee, independiente de `paymentFormId` (SAT). |
| `FReceiveCurrencyID` / `FReceiveCurrency` | `receiveCurrencyId` / `receiveCurrencyCode` | Se envía. Por defecto toma la moneda de liquidación. |
| `FReceiveAmt` | — | `amount` |
| `FExchangeRate` | `exchangeRate` | Se envía. Por defecto `1` cuando ambas monedas coinciden; **obligatorio si difieren** (BambooERP no tiene tabla de tipo de cambio). |

Todos son opcionales: el body que ya usa el ERP sigue funcionando sin cambios. La
respuesta incluye el bloque `kingdee` con el documento armado y los nombres `F*` tal cual,
listo para empujarlo a Kingdee.

> Las columnas se agregan con `sql/2026-08-06_payments_kingdee_fields.sql`, que es
> idempotente y sólo agrega columnas nullable (no afecta al ERP ni a los triggers).

**Respuesta (201 Created):**

```json
{
  "paymentId": 34075,
  "folio": "PAY-0826-000023",
  "customerCode": "SIN2A100652",
  "customer": "BAUDELIO GONZALEZ VAZQUEZ",
  "paymentDate": "2026-08-05T00:00:00",
  "amount": 504.70,
  "paymentFormId": 3,
  "paymentFormCode": "03",
  "paymentForm": "Transferencia electrónica de fondos",
  "accountId": 3,
  "account": "XIAN INTERNATIONAL SA DE CV",
  "bankId": 7,
  "bank": "BBVA",
  "bankAccountNumber": "0124482190",
  "statusId": 4,
  "statusRaw": "PENDIENTE",
  "status": "PENDING",
  "reference": "0123456789",
  "paymentType": "payment",
  "saleId": 79021,
  "saleFolio": "2608-00012",
  "departmentId": 16,
  "department": "Sucursal Ramon Corona",
  "uploadedById": 426,
  "sellerId": 123,
  "paymentFilePath": "comprobante-1785955090.jpeg",
  "comentary": "Transferencia recibida",
  "observations": null,
  "createdAt": "2026-08-05T12:38:10.07",
  "kingdee": {
    "FBillNo": "SKCZD000123",
    "FDate": "2026-08-05T00:00:00",
    "FBizOrgId": 847244,
    "FBizOrg": "801",
    "FSETTLEORGID": 847244,
    "FSETTLEORG": "801",
    "FBranchID": 1148514,
    "Fbranch": "801.10.04",
    "FSalerID": 1772,
    "FSaler": "GW000041",
    "FCashierID": 1772,
    "FCashier": "GW000041",
    "FCustomerID": 5966485,
    "FCustomer": "SIN2A100652",
    "FSETTLECURRENCYID": 1,
    "FSETTLECURRENCY": "MXN",
    "FNote": "Transferencia recibida",
    "FCardID": 5001,
    "FCard": "6234567890",
    "FMemberID": 8801,
    "FMember": "VIP00034",
    "FAccountID": 100012,
    "FAccount": "BANK001",
    "FRechargeAmount": 504.70,
    "FReceiveTypeID": 5,
    "FReceiveType": "SKFS03",
    "FReceiveCurrencyID": 1,
    "FReceiveCurrency": "MXN",
    "FReceiveAmt": 504.70,
    "FExchangeRate": 1.0
  },
  "kingdeeSales": [
    {
      "saleId": 201363,
      "isPos": false,
      "folio": "XSCKD100949",
      "amountApplied": 5166.67,
      "appliedDate": "2026-05-18T17:29:44.433"
    }
  ]
}
```

El bloque `kingdee` sale con los nombres `F*` exactos (respeta mayúsculas y minúsculas
tal cual los pidieron), no en `camelCase` como el resto de la respuesta.

### Folio de la venta en Kingdee (`kingdeeSales`)

Las ventas contra las que se aplicó el pago salen de `PaymentApplications`, y el folio del
documento que se generó en Kingdee vive en una de dos tablas, según `isPOS`:

| `isPOS` | Tabla | Se busca por | Folio |
|---|---|---|---|
| `0` | `kingdee_sales_invoices` | `id = SaleId` | `bill_code`. Ejemplo: `XSCKD100949` |
| `1` | `KingDeeSalesPOS` | `Id = SaleId` | `Folio`. Ejemplo: `10040002604109633` |

Es una **lista**, no un campo: un mismo pago se puede repartir entre varias ventas. El pago
`PAY-0526-000011`, por ejemplo, está aplicado a tres facturas (`XSCKD100949`,
`XSCKD100955` y `XSCKD100950`) con su propio `amountApplied` cada una.

> `saleId: 0` con `folio: null` significa que el pago ya está aplicado pero **la venta
> todavía no se genera en Kingdee**. La aplicación se publica igual, en lugar de
> desaparecer, para que se distinga de un pago sin aplicar (que trae `kingdeeSales` vacío).

### Consultar pago

```
GET /api/payments/{id}
```

Devuelve el pago con la misma estructura de la respuesta anterior. `404` si no existe.

### Listar pagos por estatus

```
GET /api/payments?status=REJECTED,IN_PROCESS
```

Listado paginado de pagos. **El estatus se filtra y se publica en inglés**, nunca con el
nombre en español que BambooERP guarda en `catEstatus`:

| `status` | `catEstatus` | Qué es |
|---|---|---|
| `VALID` | 29 `Valido` | Pago validado |
| `REJECTED` | 30 `Rechazado` | Pago rechazado |
| `PENDING` | 4 `PENDIENTE` | Registrado, todavía sin revisar (es el estatus con el que nace un pago) |
| `IN_PROCESS` | 17 `EN PROCESO` | En proceso de validación |
| `CANCELLED` | 8 `CANCELADO` | Cancelado |

Se pueden pedir varios separados por coma (`status=REJECTED,IN_PROCESS`). Si se omite,
entran todos. Un valor fuera de la lista devuelve `400` en vez de una página vacía, para
que un typo no se lea como "no hay ninguno".

**Filtros**

| Parámetro | Descripción |
|---|---|
| `status` | Uno o varios estatus en inglés, separados por coma. |
| `statusId` | Estatus por id interno (`catEstatus.idEstatus`), para quien trabaje con el id. |
| `customerCode` | Cliente (`customers.customer_code`). |
| `folio` | Folio del pago, coincidencia parcial. Ej.: `PAY-0826`. |
| `saleFolio` | Folio de la venta de **BambooERP** a la que se aplicó el pago (`quotation.billCode`). Ej.: `2608-00022`. |
| `kingdeeSaleFolio` | Folio de la venta tal como se generó en **Kingdee**: `kingdee_sales_invoices.bill_code` (ej.: `XSCKD100949`) o `KingDeeSalesPOS.Folio` si es ticket POS (ej.: `10040002604109633`). |
| `branchCode` | Sucursal **del pago** (`starnet_branches.code`), vía `Payments.DepartmentId` → `departments.branchId`. Es la misma que sale en `kingdee.Fbranch`. Ej.: `801.10.02`. |
| `startDate` / `endDate` | Rango sobre la **fecha de pago** (`paymentDate`). |
| `saleStartDate` / `saleEndDate` | Rango sobre la **fecha de la venta de BambooERP** (`quotation.created_at`, la del `saleFolio`). Deja fuera los pagos sin venta relacionada. |
| `kingdeeSaleStartDate` / `kingdeeSaleEndDate` | Rango sobre la **fecha de la venta en Kingdee**: `kingdee_sales_invoices.bill_date` o `KingDeeSalesPOS.BillDate` según `isPOS`. Conserva el pago si al menos una de sus aplicaciones cae en el rango. |
| `paymentFormId` | Forma de pago SAT (`sat_FormaPago.ID`). |
| `bankId` | Cuenta bancaria (`bancos.id`). |
| `departmentId` | Sucursal/departamento del pago. |
| `sellerId` | Vendedor al que se acredita el pago. |
| `paymentType` | `payment`, `credit` o `advance`. |
| `page` / `pageSize` | Paginación. Default `1` / `50`, máximo `200`. |

**Respuesta**

Cada elemento de `payments[]` trae **el registro completo del pago**, idéntico al de
`GET /api/payments/{id}` (incluido el bloque `kingdee`). Además, `summary` desglosa
**todo lo que matcheó el filtro**, no solo la página, y `totalAmount` es la suma de esos
importes:

```json
{
  "page": 1,
  "pageSize": 50,
  "totalRecords": 1002,
  "totalPages": 21,
  "totalAmount": 6132011.20,
  "summary": [
    { "statusId": 17, "statusRaw": "EN PROCESO", "status": "IN_PROCESS", "count": 5, "amount": 0.00 },
    { "statusId": 30, "statusRaw": "Rechazado", "status": "REJECTED", "count": 997, "amount": 6132011.20 }
  ],
  "payments": [ ... ]
}
```

> `amount` viene vacío mientras el pago no se valida — el ERP lo llena al validarlo. Por
> eso `PENDING` e `IN_PROCESS` suman `0.00` aunque tengan pagos: es el dato real, no un
> error del cálculo.

Los tres rangos de fecha son **independientes y se combinan con AND**: se puede pedir, por
ejemplo, los pagos cobrados en agosto de ventas facturadas en Kingdee en mayo. `endDate`,
`saleEndDate` y `kingdeeSaleEndDate` incluyen el día completo cuando se mandan sin hora.

```
GET /api/payments?branchCode=801.10.02&status=VALID
GET /api/payments?saleFolio=2608-00022
GET /api/payments?kingdeeSaleFolio=XSCKD100949
GET /api/payments?saleStartDate=2026-07-01&saleEndDate=2026-07-31
GET /api/payments?kingdeeSaleStartDate=2026-05-01&kingdeeSaleEndDate=2026-05-31
```

Hay **dos folios de venta distintos** y cada uno tiene su filtro: `saleFolio` es la venta de
BambooERP (la que sale en la respuesta como `saleFolio`) y `kingdeeSaleFolio` es el
documento generado en Kingdee (el que sale en `kingdeeSales[].folio`). Ambos son
coincidencia exacta.

Cuando `kingdeeSaleFolio` se combina con `kingdeeSaleStartDate`/`kingdeeSaleEndDate`, las
condiciones se exigen sobre **la misma venta**: un pago repartido entre varias facturas no
entra por tener el folio en una y la fecha en otra.

> Los filtros `kingdeeSale*` consultan `PaymentApplications`, `kingdee_sales_invoices` y
> `KingDeeSalesPOS`. Si el ambiente no tiene esas tablas, esos filtros fallan; el resto del
> endpoint no las toca.

Los pagos salen del más nuevo al más viejo (`p.Id DESC`).

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
