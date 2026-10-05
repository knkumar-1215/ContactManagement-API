using DataAccessLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLibrary.Interfaces
{
    public interface IContactRepository
    {
        List<ContactModel> GetAllContacts();
        List<ContactModel> GetContactsByLastName(string lastName);
        ContactModel GetContactById(int id);
        ContactModel GetContactByEmail(string email);
        void CreateContact(ContactModel contact);
        void UpdateContact(ContactModel contact);
        void DeleteContact(int id);

    }
}
