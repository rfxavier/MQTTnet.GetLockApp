USE [getlock]
GO

/****** Object:  Table [dbo].[movimento]    Script Date: 14/01/2022 15:53:40 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[movimento](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[data_type] [nvarchar](50) NOT NULL,
	[nome] [nvarchar](50) NOT NULL,
	[tipo] [nvarchar](50) NULL,
	[trackLastWriteTime] [datetime] NULL,
	[trackCreationTime] [datetime] NULL,
 CONSTRAINT [PK_movimento] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[movimento] ADD  CONSTRAINT [DF_movimento_tlwt]  DEFAULT (getdate()) FOR [trackLastWriteTime]
GO

ALTER TABLE [dbo].[movimento] ADD  CONSTRAINT [DF_movimento_tlct]  DEFAULT (getdate()) FOR [trackCreationTime]
GO


