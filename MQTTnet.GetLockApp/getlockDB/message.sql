USE [getlock]
GO

/****** Object:  Table [dbo].[message]    Script Date: 14/01/2022 15:51:25 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[message](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[info_id] [nvarchar](50) NULL,
	[info_ip] [nvarchar](50) NULL,
	[info_mac] [nvarchar](50) NULL,
	[info_json] [nvarchar](50) NULL,
	[data_hash] [nvarchar](50) NULL,
	[data_tmst_begin] [nvarchar](50) NULL,
	[data_tmst_end] [nvarchar](50) NULL,
	[data_user] [nvarchar](50) NULL,
	[data_type] [nvarchar](50) NULL,
	[data_currency_total] [numeric](18, 2) NULL,
	[data_currency_bill_2] [bigint] NULL,
	[data_currency_bill_5] [bigint] NULL,
	[data_currency_bill_10] [bigint] NULL,
	[data_currency_bill_20] [bigint] NULL,
	[data_currency_bill_50] [bigint] NULL,
	[data_currency_bill_100] [bigint] NULL,
	[data_currency_bill_200] [bigint] NULL,
	[data_currency_bill_rejected] [bigint] NULL,
	[data_currency_envelope] [bigint] NULL,
	[data_currency_envelope_total] [numeric](18, 2) NULL,
	[id_cofre] [nvarchar](50) NULL,
	[trackLastWriteTime] [datetime] NULL,
	[trackCreationTime] [datetime] NULL,
	[data_tmst_begin_datetime] [datetime] NULL,
	[data_tmst_end_datetime] [datetime] NULL,
 CONSTRAINT [PK_message] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[message] ADD  CONSTRAINT [DF_message_tlwt]  DEFAULT (getdate()) FOR [trackLastWriteTime]
GO

ALTER TABLE [dbo].[message] ADD  CONSTRAINT [DF_message_tlct]  DEFAULT (getdate()) FOR [trackCreationTime]
GO


