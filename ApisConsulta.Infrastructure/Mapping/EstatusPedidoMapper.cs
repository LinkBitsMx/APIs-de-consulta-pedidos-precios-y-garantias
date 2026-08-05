namespace ApisConsulta.Infrastructure.Mapping;

/// <summary>
/// Traduce los estatus internos de BambooERP (<c>startnet_sales_orders_status</c>) a las
/// etapas públicas que exponen las APIs. Lo comparten la consulta de estatus de pedido
/// (API 2) y la de ventas (API 7), que además publica el nombre original.
/// </summary>
public static class EstatusPedidoMapper
{
    public static string Mapear(string? nombre)
    {
        return nombre?.ToLower() switch
        {
            "cotizacion"              => "En proceso de cotizacion",

            "pago sin proceso"        => "Se mando a surtir al CEDIS",
            "pago no valido"          => "Se mando a surtir al CEDIS",
            "pago validado"           => "Se mando a surtir al CEDIS",

            "sin procesar"            => "En proceso de surtido",
            "surtido en proceso"      => "En proceso de surtido",
            "productos por llegar"    => "En proceso de surtido",
            "agotados"                => "En proceso de surtido",
            "agotados y por llegar"   => "En proceso de surtido",
            "pendiente por cerrar"    => "En proceso de surtido",
            "surtido"                 => "En proceso de surtido",
            "surtido sin proceso"     => "En proceso de surtido",
            "sin enviar"              => "En proceso de surtido",
            "enviado"                 => "En proceso de surtido",
            "recibido"                => "En proceso de surtido",

            "empacado sin procesar"   => "En proceso de empacado o revision",
            "empacado incidencia"     => "En proceso de empacado o revision",
            "empacado en proceso"     => "En proceso de empacado o revision",
            "empacado finalizado"     => "En proceso de empacado o revision",
            "validar sin proceso"     => "En proceso de empacado o revision",
            "validar incidencia"      => "En proceso de empacado o revision",
            "validar proceso"         => "En proceso de empacado o revision",
            "validado por seguridad"  => "En proceso de empacado o revision",
            "orden local"             => "En proceso de empacado o revision",

            "guía sin proceso"        => "En proceso de guias de envio",
            "guia sin proceso"        => "En proceso de guias de envio",
            "guia en proceso"         => "En proceso de guias de envio",
            "guia generada"           => "En proceso de guias de envio",
            "pegar guia en proceso"   => "En proceso de guias de envio",
            "guia pegada"             => "En proceso de guias de envio",

            "recolectado"             => "Entregado",
            "recolectado local"       => "Entregado",
            "cobrado local"           => "Entregado",

            "cancelado"               => "Cancelado",

            _ => "Desconocido"
        };
    }
}
