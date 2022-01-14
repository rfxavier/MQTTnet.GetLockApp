USE [getlock]
GO

/****** Object:  Table [dbo].[cofre_user]    Script Date: 14/01/2022 15:52:55 ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[cofre_user](
	[id] [bigint] IDENTITY(1,1) NOT NULL,
	[id_cofre] [nvarchar](50) NOT NULL,
	[data_user] [nvarchar](50) NOT NULL,
	[nome] [nvarchar](50) NULL,
	[trackLastWriteTime] [datetime] NULL,
	[trackCreationTime] [datetime] NULL,
 CONSTRAINT [PK_cofre_user] PRIMARY KEY CLUSTERED 
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO


