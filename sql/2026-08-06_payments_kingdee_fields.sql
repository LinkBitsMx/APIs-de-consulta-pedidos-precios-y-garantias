/*
    Campos del documento de Kingdee (充值单) sobre la tabla Payments.

    Las 21 columnas son NULL y sin default, para que todo lo que ya escribe en Payments
    siga funcionando sin cambios: son identificadores propios de Kingdee y BambooERP no
    tiene catalogo contra el cual resolverlos, asi que se guardan tal cual llegan.

    Sin este script, la API de pagos truena con "Invalid column name" (error 207) en:
      - GET  /api/payments        (listado por estatus)
      - GET  /api/payments/{id}   (consulta de pago)
      - POST /api/payments        (alta de pago)

    El script es idempotente: se puede correr varias veces sin efecto en las columnas
    que ya existan. Ejecutar sobre la base BambooERP del ambiente correspondiente.
*/

SET NOCOUNT ON;

IF COL_LENGTH('dbo.Payments', 'KingdeeBillNo') IS NULL
    ALTER TABLE dbo.Payments ADD KingdeeBillNo varchar(100) NULL;         -- FBillNo (单据编号)

IF COL_LENGTH('dbo.Payments', 'BizOrgId') IS NULL
    ALTER TABLE dbo.Payments ADD BizOrgId int NULL;                       -- FBizOrgId (业务组织)

IF COL_LENGTH('dbo.Payments', 'BizOrgCode') IS NULL
    ALTER TABLE dbo.Payments ADD BizOrgCode varchar(50) NULL;             -- FBizOrg

IF COL_LENGTH('dbo.Payments', 'SettleOrgId') IS NULL
    ALTER TABLE dbo.Payments ADD SettleOrgId int NULL;                    -- FSETTLEORGID (结算组织)

IF COL_LENGTH('dbo.Payments', 'SettleOrgCode') IS NULL
    ALTER TABLE dbo.Payments ADD SettleOrgCode varchar(50) NULL;          -- FSETTLEORG

IF COL_LENGTH('dbo.Payments', 'CashierId') IS NULL
    ALTER TABLE dbo.Payments ADD CashierId int NULL;                      -- FCashierID (收银员)

IF COL_LENGTH('dbo.Payments', 'CashierCode') IS NULL
    ALTER TABLE dbo.Payments ADD CashierCode varchar(50) NULL;            -- FCashier

IF COL_LENGTH('dbo.Payments', 'KingdeeAccountId') IS NULL
    ALTER TABLE dbo.Payments ADD KingdeeAccountId int NULL;               -- FAccountID (账户)

IF COL_LENGTH('dbo.Payments', 'KingdeeAccountCode') IS NULL
    ALTER TABLE dbo.Payments ADD KingdeeAccountCode varchar(50) NULL;     -- FAccount

IF COL_LENGTH('dbo.Payments', 'ReceiveTypeId') IS NULL
    ALTER TABLE dbo.Payments ADD ReceiveTypeId int NULL;                  -- FReceiveTypeID (收款方式)

IF COL_LENGTH('dbo.Payments', 'ReceiveTypeCode') IS NULL
    ALTER TABLE dbo.Payments ADD ReceiveTypeCode varchar(50) NULL;        -- FReceiveType

IF COL_LENGTH('dbo.Payments', 'SettleCurrencyId') IS NULL
    ALTER TABLE dbo.Payments ADD SettleCurrencyId int NULL;               -- FSETTLECURRENCYID (结算币别)

IF COL_LENGTH('dbo.Payments', 'SettleCurrencyCode') IS NULL
    ALTER TABLE dbo.Payments ADD SettleCurrencyCode varchar(10) NULL;     -- FSETTLECURRENCY

IF COL_LENGTH('dbo.Payments', 'ReceiveCurrencyId') IS NULL
    ALTER TABLE dbo.Payments ADD ReceiveCurrencyId int NULL;              -- FReceiveCurrencyID (收款币别)

IF COL_LENGTH('dbo.Payments', 'ReceiveCurrencyCode') IS NULL
    ALTER TABLE dbo.Payments ADD ReceiveCurrencyCode varchar(10) NULL;    -- FReceiveCurrency

IF COL_LENGTH('dbo.Payments', 'ExchangeRate') IS NULL
    ALTER TABLE dbo.Payments ADD ExchangeRate decimal(18, 6) NULL;        -- FExchangeRate (汇率)

IF COL_LENGTH('dbo.Payments', 'CardId') IS NULL
    ALTER TABLE dbo.Payments ADD CardId int NULL;                         -- FCardID (卡号)

IF COL_LENGTH('dbo.Payments', 'CardNumber') IS NULL
    ALTER TABLE dbo.Payments ADD CardNumber varchar(50) NULL;             -- FCard

IF COL_LENGTH('dbo.Payments', 'MemberId') IS NULL
    ALTER TABLE dbo.Payments ADD MemberId int NULL;                       -- FMemberID (会员卡号)

IF COL_LENGTH('dbo.Payments', 'MemberCardNumber') IS NULL
    ALTER TABLE dbo.Payments ADD MemberCardNumber varchar(50) NULL;       -- FMember

IF COL_LENGTH('dbo.Payments', 'RechargeAmount') IS NULL
    ALTER TABLE dbo.Payments ADD RechargeAmount decimal(18, 2) NULL;      -- FRechargeAmount (充值金额)

GO

/* Verificacion: deben salir las 21 columnas. */
SELECT c.name AS columna, t.name AS tipo, c.max_length, c.precision, c.scale, c.is_nullable
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Payments')
  AND c.name IN ('KingdeeBillNo','BizOrgId','BizOrgCode','SettleOrgId','SettleOrgCode',
                 'CashierId','CashierCode','KingdeeAccountId','KingdeeAccountCode',
                 'ReceiveTypeId','ReceiveTypeCode','SettleCurrencyId','SettleCurrencyCode',
                 'ReceiveCurrencyId','ReceiveCurrencyCode','ExchangeRate','CardId','CardNumber',
                 'MemberId','MemberCardNumber','RechargeAmount')
ORDER BY c.column_id;
