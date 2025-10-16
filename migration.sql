IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

CREATE TABLE [Funcionarios] (
    [Id] int NOT NULL IDENTITY,
    [Nome] nvarchar(max) NULL,
    [Endereco] nvarchar(max) NULL,
    [Ramal] nvarchar(max) NULL,
    [EmailProfissional] nvarchar(max) NULL,
    [Departamento] nvarchar(max) NULL,
    [Salario] decimal(18,2) NOT NULL,
    [DataAdmissao] datetimeoffset NULL,
    CONSTRAINT [PK_Funcionarios] PRIMARY KEY ([Id])
);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20220623031755_Initial', N'8.0.18');
GO

COMMIT;
GO