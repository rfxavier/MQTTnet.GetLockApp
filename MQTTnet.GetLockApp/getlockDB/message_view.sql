USE [getlock]
GO

/****** Object:  View [dbo].[message_view]    Script Date: 14/01/2022 16:09:53 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE view [dbo].[message_view] as 
SELECT
	m.id_cofre,
	c.nome AS cofre_nome,
	c.serie AS cofre_serie,
	c.tipo AS cofre_tipo,
	c.marca AS cofre_marca,
	c.modelo AS cofre_modelo,
	c.tamanho_malote AS cofre_tamanho_malote,
	m.id,
	m.info_id,
	m.info_ip,
	m.info_mac,
	m.info_json,
	m.data_hash,
	m.data_tmst_begin,
	m.data_tmst_end,
	m.data_user,
	cu.nome AS user_nome,
	m.data_type,
	mov.nome AS movimento_nome,
	mov.tipo AS movimento_tipo,
	m.data_currency_total,
	m.data_currency_bill_2,
	m.data_currency_bill_5,
	m.data_currency_bill_10,
	m.data_currency_bill_20,
	m.data_currency_bill_50,
	m.data_currency_bill_100,
	m.data_currency_bill_200,
	m.data_currency_bill_rejected,
	m.data_currency_envelope,
	m.data_currency_envelope_total,
	m.cod_loja,
	l.nome as nome_loja,
	l.razao_social as razao_social_loja,
	l.cnpj as cnpj_loja,
	l.endereco as endereco_loja,
	l.complemento as complemento_loja,
	l.bairro as bairro_loja,
	l.cidade as cidade_loja,
	l.uf as uf_loja,
	l.CEP as cep_loja,
	l.latitude as latitude_loja,
	l.longitude as longitude_loja,
	l.pessoa_contato as pessoa_contato_loja,
	l.email as email_loja,
	l.telefone as telefone_loja,
	l.cod_cliente,
	cli.nome as nome_cliente,
	cli.razao_social as razao_social_cliente,
	cli.cnpj as cnpj_cliente,
	cli.endereco as endereco_cliente,
	cli.complemento as complemento_cliente,
	cli.bairro as bairro_cliente,
	cli.cidade as cidade_cliente,
	cli.uf as uf_cliente,
	cli.CEP as cep_cliente,
	cli.latitude as latitude_cliente,
	cli.longitude as longitude_cliente,
	cli.pessoa_contato as pessoa_contato_cliente,
	cli.email as email_cliente,
	cli.telefone as telefone_cliente,
	cli.cod_rede,
	r.nome as nome_rede,
	m.trackLastWriteTime,
	m.trackCreationTime,
	m.data_tmst_begin_datetime,
	m.data_tmst_end_datetime,
	m.data_currency_bill,
	m.data_currency_bill_total,
	m.data_sensor
FROM
	dbo.message AS m
LEFT OUTER JOIN dbo.cofre AS c ON
	m.id_cofre = c.id_cofre
LEFT OUTER JOIN dbo.movimento AS mov ON
	m.data_type = mov.data_type
LEFT OUTER JOIN dbo.cofre_user AS cu ON
	m.id_cofre = cu.id_cofre
	AND m.data_user = cu.data_user
LEFT OUTER JOIN dbo.loja AS l ON
	m.cod_loja = l.cod_loja
LEFT OUTER JOIN dbo.cliente as cli ON
	l.cod_cliente = cli.cod_cliente
LEFT OUTER JOIN dbo.rede as r ON
	cli.cod_rede = r.cod_rede
GO

