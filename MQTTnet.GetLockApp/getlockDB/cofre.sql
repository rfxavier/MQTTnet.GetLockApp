USE [getlock]
GO

/****** Object:  Table [dbo].[cofre]    Script Date: 14/01/2022 15:52:35 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[cofre](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[id_cofre] [nvarchar](50) NOT NULL,
	[nome] [nvarchar](50) NOT NULL,
	[serie] [nvarchar](50) NULL,
	[tipo] [nvarchar](50) NULL,
	[marca] [nvarchar](50) NULL,
	[modelo] [nvarchar](50) NULL,
	[tamanho_malote] [nvarchar](50) NULL,
	[cliente] [nvarchar](50) NULL,
	[loja] [nvarchar](50) NULL,
	[trackLastWriteTime] [datetime] NULL,
	[trackCreationTime] [datetime] NULL,
 CONSTRAINT [PK_cofre] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[cofre] ADD  CONSTRAINT [DF_cofre_tlwt]  DEFAULT (getdate()) FOR [trackLastWriteTime]
GO

ALTER TABLE [dbo].[cofre] ADD  CONSTRAINT [DF_cofre_tlct]  DEFAULT (getdate()) FOR [trackCreationTime]
GO


