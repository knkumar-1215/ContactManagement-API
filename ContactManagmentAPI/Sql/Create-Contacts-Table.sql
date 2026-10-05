CREATE TABLE Contacts
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    FirstName   NVARCHAR(50)  NOT NULL,
    LastName    NVARCHAR(50)  NOT NULL,
    Email       NVARCHAR(100) NOT NULL UNIQUE,
    Phone       NVARCHAR(20)  NOT NULL,
    CreatedDate DATETIME      NOT NULL DEFAULT GETDATE()
)