using DataAccessLibrary.Interfaces;
using DataAccessLibrary.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Protocols.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLibrary
{
    public class ContactRepository : IContactRepository
    {
        private readonly ISqlDataAccess _sqlDataAccess;
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public ContactRepository(ISqlDataAccess sqlDataAccess, IConfiguration configuration )
        {
            _sqlDataAccess = sqlDataAccess;
            _configuration = configuration;
            _connectionString = configuration["ConnectionStrings:Default"];
        }

       
        public void CreateContact(ContactModel contact)
        {

            string sql = @"INSERT INTO Contacts
                    (FirstName, LastName, Email,
                        Phone, CreatedDate)
                    VALUES
                    (@FirstName, @LastName, @Email,
                        @Phone, @CreatedDate);";
            _sqlDataAccess.SaveData<ContactModel>(sql, contact, _connectionString);


        }

        public void DeleteContact(int id)
        {
            var contact =  GetContactById(id);
            if(contact != null)
            {
                string sql = "DELETE FROM Contacts  WHERE Id = @Id";
                _sqlDataAccess.SaveData(sql, new { Id = id }, _connectionString);
            }
            
        }

        public List<ContactModel> GetAllContacts()
        {
            string sql = @"SELECT Id, FirstName, LastName,
         Email, Phone, CreatedDate
  FROM Contacts;
";

            return _sqlDataAccess.LoadData<ContactModel, dynamic>(sql, new { }, _connectionString);
        }

        public ContactModel GetContactByEmail(string email)
        {
            string sql = @"SELECT Id, FirstName, LastName,
                    Email, Phone, CreatedDate
                     FROM Contacts
                    WHERE Email = @Email;";
            return _sqlDataAccess.LoadData<ContactModel, dynamic>(sql, new { Email = email }, _connectionString).FirstOrDefault();

        }

        public ContactModel GetContactById(int id)
        {
            string sql = @"SELECT Id, FirstName, LastName,
                            Email, Phone, CreatedDate
                            FROM Contacts
                            WHERE Id = @Id;";
            return _sqlDataAccess.LoadData<ContactModel, dynamic>(sql, new { Id = id }, _connectionString).FirstOrDefault();

        }

        public List<ContactModel> GetContactsByLastName(string lastName)
        {
            string sql = @"SELECT Id, FirstName, LastName,
                            Email, Phone, CreatedDate
                            FROM Contacts
                            WHERE LastName = @LastName;";
            return _sqlDataAccess.LoadData<ContactModel, dynamic>(sql, new { LastName = lastName }, _connectionString);
        }

        public void UpdateContact(ContactModel contact)
        {
            string sql = @"UPDATE Contacts
         SET FirstName   = @FirstName,
         LastName    = @LastName,
         Email       = @Email,
         Phone       = @Phone
         WHERE Id = @Id;";

            _sqlDataAccess.SaveData<ContactModel>(sql, contact, _connectionString);


        }
    }
}
