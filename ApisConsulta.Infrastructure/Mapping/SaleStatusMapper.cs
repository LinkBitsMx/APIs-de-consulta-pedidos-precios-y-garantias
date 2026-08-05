namespace ApisConsulta.Infrastructure.Mapping;

/// <summary>
/// Maps the internal BambooERP statuses (<c>startnet_sales_orders_status</c>, stored in
/// Spanish) to the English stages published by the sales API. It mirrors the stages of
/// <see cref="EstatusPedidoMapper"/>, which serves the Spanish endpoints.
/// </summary>
public static class SaleStatusMapper
{
    public static string Map(string? name)
    {
        return name?.ToLower() switch
        {
            "cotizacion"              => "IN_QUOTATION",

            "pago sin proceso"        => "SENT_TO_CEDIS",
            "pago no valido"          => "SENT_TO_CEDIS",
            "pago validado"           => "SENT_TO_CEDIS",

            "sin procesar"            => "IN_PICKING",
            "surtido en proceso"      => "IN_PICKING",
            "productos por llegar"    => "IN_PICKING",
            "agotados"                => "IN_PICKING",
            "agotados y por llegar"   => "IN_PICKING",
            "pendiente por cerrar"    => "IN_PICKING",
            "surtido"                 => "IN_PICKING",
            "surtido sin proceso"     => "IN_PICKING",
            "sin enviar"              => "IN_PICKING",
            "enviado"                 => "IN_PICKING",
            "recibido"                => "IN_PICKING",

            "empacado sin procesar"   => "IN_PACKING_OR_REVIEW",
            "empacado incidencia"     => "IN_PACKING_OR_REVIEW",
            "empacado en proceso"     => "IN_PACKING_OR_REVIEW",
            "empacado finalizado"     => "IN_PACKING_OR_REVIEW",
            "validar sin proceso"     => "IN_PACKING_OR_REVIEW",
            "validar incidencia"      => "IN_PACKING_OR_REVIEW",
            "validar proceso"         => "IN_PACKING_OR_REVIEW",
            "validado por seguridad"  => "IN_PACKING_OR_REVIEW",
            "orden local"             => "IN_PACKING_OR_REVIEW",

            "guía sin proceso"        => "IN_SHIPPING_LABEL",
            "guia sin proceso"        => "IN_SHIPPING_LABEL",
            "guia en proceso"         => "IN_SHIPPING_LABEL",
            "guia generada"           => "IN_SHIPPING_LABEL",
            "pegar guia en proceso"   => "IN_SHIPPING_LABEL",
            "guia pegada"             => "IN_SHIPPING_LABEL",

            "recolectado"             => "DELIVERED",
            "recolectado local"       => "DELIVERED",
            "cobrado local"           => "DELIVERED",

            "cancelado"               => "CANCELLED",

            _ => "UNKNOWN"
        };
    }
}
