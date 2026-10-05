using Microsoft.Extensions.Configuration;
using DataAccessLibrary;
using DataAccessLibrary.Interfaces;
using DataAccessLibrary.Models;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContactManagmentAPITests;

public class ContactCRUDTest
{
    [Fact]
    public void GetContacts_ReturnsAllContacts()
    {
        var mockConfig = new Mock<IConfiguration>();

        // Mock the underlying call that GetConnectionString uses internally
        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");
        //Arrange
        var mockDb = new Mock<ISqlDataAccess>();

        var fakeContacts = new List<ContactModel>
            {
                new ContactModel{ID = 1, FirstName = "Naga", LastName = "K", Email = "Abc@Abc.com", Phone = "1234567890", CreatedDate = DateTime.Now}
            };

        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                 It.IsAny<string>(), It.IsAny<object>(), It.IsAny<string>()))
               .Returns(fakeContacts);

        ContactRepository contactRepository = new ContactRepository(mockDb.Object, mockConfig.Object);

        List<ContactModel> result = contactRepository.GetAllContacts();

        Assert.Single(result);
        Assert.Equal("Naga", result[0].FirstName);
    }


    [Fact]
    public void GetContactById_ContactExists_Returns200WithContact()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");

        var fakeContact = new ContactModel
        {
            ID = 1,
            FirstName = "Naga",
            LastName = "K",
            Email = "naga@k.com",
            Phone = "1234567890"
        };

        // LoadData returns list — FirstOrDefault picks the one
        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(new List<ContactModel> { fakeContact });

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        // Act
        var result = repo.GetContactById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Naga", result.FirstName);
        Assert.Equal(1, result.ID);
    }

    [Fact]
    public void GetContactById_ContactNotFound_ReturnsNull()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");

        // Empty list — FirstOrDefault returns null
        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(new List<ContactModel>());

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        // Act
        var result = repo.GetContactById(999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetContactByEmail_EmailNotFound_ReturnsNull()
    {
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");

        // Empty list — FirstOrDefault returns null
        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(new List<ContactModel>());

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        // Act
        var result = repo.GetContactByEmail("naga@k.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetContactsByLastName_ReturnsMatchingContacts()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");
        var fakeContact = new ContactModel
        {
            ID = 1,
            FirstName = "Naga",
            LastName = "K",
            Email = "naga@k.com",
            Phone = "1234567890"
        };
        // Empty list — FirstOrDefault returns null
        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(new List<ContactModel> { fakeContact });

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        // Act
        var result = repo.GetContactsByLastName("K");

        // Assert
        // Assert
        Assert.Single(result);
        Assert.Equal("K", result[0].LastName);
        
       
    }

    [Fact]
    public void GetContactByEmail_EmailExists_ReturnsContact()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");
        var fakeContact = new ContactModel
        {
            ID = 1,
            FirstName = "Naga",
            LastName = "K",
            Email = "naga@k.com",
            Phone = "1234567890"
        };
      
        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(new List<ContactModel> { fakeContact });

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        // Act
        var result = repo.GetContactByEmail("naga@k.com");

        // Assert
       
        Assert.Equal("naga@k.com", result.Email);

    }

    [Fact]
    public void CreateContact_ValidContact_CallsSaveDataOnce()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");

        mockDb.Setup(db => db.SaveData(
          It.IsAny<string>(),
          It.IsAny<object>(),
          It.IsAny<string>()));
      
        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        var newContact = new ContactModel 
        {
            ID = 5,
            FirstName = "JOHN",
            LastName = "K",
            Email = "John@k.com",
            Phone = "1234567890"
        };

        // Act
        repo.CreateContact(newContact);

        // Assert
        mockDb.Verify(db => db.SaveData<ContactModel>(
         It.IsAny<string>(),
         It.IsAny<ContactModel>(),
         It.IsAny<string>()),
         Times.Once);
    }


    [Fact]
    public void UpdateContact_ValidContact_CallsSaveDataOnce()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");

        mockDb.Setup(db => db.SaveData(
          It.IsAny<string>(),
          It.IsAny<object>(),
          It.IsAny<string>()));

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        var existingContact = new ContactModel
        {
            ID = 5,
            FirstName = "JOHNNY",
            LastName = "K",
            Email = "John@k.com",
            Phone = "1234567890"
        };

        // Act
        repo.UpdateContact(existingContact);

        // Assert
        mockDb.Verify(db => db.SaveData<ContactModel>(
         It.IsAny<string>(),
         It.IsAny<ContactModel>(),
         It.IsAny<string>()),
         Times.Once);
    }

    [Fact]
    public void DeleteContact_ValidId_CallsSaveDataOnce()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");

        // Mock LoadData — GetContactById needs this
        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(new List<ContactModel>
            {
            new ContactModel { ID = 1, FirstName = "Naga" }
            });

        mockDb.Setup(db => db.SaveData(
          It.IsAny<string>(),
          It.IsAny<object>(),
          It.IsAny<string>()));

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        // Act
        repo.DeleteContact(1);

        // Assert
        mockDb.Verify(db => db.SaveData<object>(
         It.IsAny<string>(),
         It.IsAny<object>(),
         It.IsAny<string>()),
         Times.Once);
    }

    [Fact]
    public void DeleteContact_ContactNotFound_SaveDataNeverCalled()
    {
        // Arrange
        var mockDb = new Mock<ISqlDataAccess>();
        var mockConfig = new Mock<IConfiguration>();

        mockConfig.Setup(c => c["ConnectionStrings:Default"])
                  .Returns("fake-connection-string");

        // Empty list — contact not found
        mockDb.Setup(db => db.LoadData<ContactModel, object>(
                It.IsAny<string>(),
                It.IsAny<object>(),
                It.IsAny<string>()))
            .Returns(new List<ContactModel>());

        var repo = new ContactRepository(mockDb.Object, mockConfig.Object);

        // Act
        repo.DeleteContact(999);

        // Assert — SaveData should NOT be called
        // because contact was not found
        mockDb.Verify(db => db.SaveData<object>(
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<string>()),
            Times.Never);
    }
}
