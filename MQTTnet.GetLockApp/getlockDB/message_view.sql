USE [getlock]
GO

/****** Object:  View [dbo].[message_view]    Script Date: 14/01/2022 16:09:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE VIEW [dbo].[message_view]
AS
SELECT        m.id_cofre, c.nome AS cofre_nome, c.serie AS cofre_serie, c.tipo AS cofre_tipo, c.marca AS cofre_marca, c.modelo AS cofre_modelo, c.tamanho_malote AS cofre_tamanho_malote, c.cliente AS cofre_cliente, c.loja AS cofre_loja, 
                         m.id, m.info_id, m.info_ip, m.info_mac, m.info_json, m.data_hash, m.data_tmst_begin, m.data_tmst_end, m.data_user, cu.nome AS user_nome, m.data_type, mov.nome AS movimento_nome, mov.tipo AS movimento_tipo, 
                         m.data_currency_total, m.data_currency_bill_2, m.data_currency_bill_5, m.data_currency_bill_10, m.data_currency_bill_20, m.data_currency_bill_50, m.data_currency_bill_100, m.data_currency_bill_200, 
                         m.data_currency_bill_rejected, m.data_currency_envelope, m.data_currency_envelope_total, m.trackLastWriteTime, m.trackCreationTime, m.data_tmst_begin_datetime, m.data_tmst_end_datetime
FROM            dbo.message AS m LEFT OUTER JOIN
                         dbo.cofre AS c ON m.id_cofre = c.id_cofre LEFT OUTER JOIN
                         dbo.movimento AS mov ON m.data_type = mov.data_type LEFT OUTER JOIN
                         dbo.cofre_user AS cu ON m.id_cofre = cu.id_cofre AND m.data_user = cu.data_user
GO

