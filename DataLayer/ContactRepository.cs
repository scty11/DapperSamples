using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;

namespace MicroOrmDemo.DataLayer
{
    public class ContactRepository : IContactRepository
    {
        private readonly IDbConnection db  =new SqlConnection(ConfigurationManager.ConnectionStrings["contactsDB"].ConnectionString);

        public Contact Find(int id)
        {
            return db.Query<Contact>("SELECT * FROM contacts WHERE Id = @Id", new {Id = id}).SingleOrDefault();
        }

        public List<Contact> GetAll()
        {
            return db.Query<Contact>("SELECT * FROM Contacts").ToList();
        }

        public Contact Add(Contact contact)
        {
            var sql =
                "INSERT INTO Contacts (FirstName, LastName, Email, Company, Title)VALUES(@FirstName, @LastName, @Email, @Company, @Title); SELECT CAST(SCOPE_IDENTITY() AS int)";
            var id = this.db.Query<int>(sql, contact).Single();
            contact.Id = id;
            return contact;
        }

        public Contact Update(Contact contact)
        {
            var sql = "UPDATE Contacts SET FirstName = @FirstName," +
                      "LastName = @LastName," +
                      "Email = @Email," +
                      "Company = @Company," +
                      "Title = @Title WHERE Id = @id";
                 db.Execute(sql, contact);
            return contact;
        }

        public void Remove(int id)
        {
            var sql = "DELETE FROM Contacts Where Id = @id";
            db.Execute(sql, new {Id = id});
        }

        public Contact GetFullContact(int id)
        {
            //var sql = "SELECT * FROM contacts WHERE Id = @Id;" +
            //          "SELECT * FROM Addresses WHERE ContactID = @ID";
            //using (var results = db.QueryMultiple(sql, new {Id = id}))
            using (var results = db.QueryMultiple("GetContact", new { Id = id }, commandType: CommandType.StoredProcedure))
            {
                var contact = results.Read<Contact>().SingleOrDefault();
                var addresses = results.Read<Address>().ToList();

                if (contact != null && addresses != null)
                {
                    contact.Addresses.AddRange(addresses);
                }
                return contact;
            }
        }

        public void Save(Contact contact)
        {
            using (var txScope = new TransactionScope())
            {
                //if (contact.IsNew)
                //{
                //    this.Add(contact);
                //}
                //else
                //{
                //    this.Update(contact);
                //}
                var parameters = new DynamicParameters();
                parameters.Add("@Id", value: contact.Id, dbType:DbType.Int32, direction: ParameterDirection.InputOutput);
                parameters.Add("@FirstName", contact.FirstName);
                parameters.Add("@LastName", contact.LastName);
                parameters.Add("@Company", contact.Company);
                parameters.Add("@Title", contact.Title);
                parameters.Add("@Email", contact.Email);
                this.db.Execute("SaveContact", parameters, commandType: CommandType.StoredProcedure);

                contact.Id = parameters.Get<int>("@Id");
                foreach (var addr in contact.Addresses.Where(a => !a.IsDeleted))
                {
                    addr.ContactId = contact.Id;

                    //if (addr.IsNew)
                    //{
                    //    this.Add(addr);
                    //}
                    //else
                    //{
                    //    this.Update(addr);
                    //}

                    var addrParams = new DynamicParameters(new
                    {
                        ContactId = addr.ContactId,
                        AddressType = addr.AddressType,
                        StreetAddress = addr.StreetAddress,
                        City = addr.City,
                        StateId = addr.StateId,
                        PostalCode = addr.PostalCode
                    });

                    addrParams.Add("@Id", addr.Id, DbType.Int32, ParameterDirection.InputOutput);
                    this.db.Execute("SaveAddress", addrParams, commandType: CommandType.StoredProcedure);
                    addr.Id = addrParams.Get<int>("@Id");
                }
                foreach (var addr in contact.Addresses.Where(a => a.IsDeleted))
                {
                    //this.db.Execute("DELETE FROM Addresses WHERE Id = @Id", new {addr.Id});
                    db.Execute("DeleteAddress", new {Id = addr.Id}, commandType: CommandType.StoredProcedure);
                }

                txScope.Complete();
            }
        }

        public
            Address Add(Address address)
        {
            var sql =
                "INSERT INTO Addresses (ContactId, AddressType, StreetAddress, City, StateId, PostalCode) VALUES(@ContactId, @AddressType, @StreetAddress, @City, @StateId, @PostalCode); " +
                "SELECT CAST(SCOPE_IDENTITY() as int)";
            var id = this.db.Query<int>(sql, address).Single();
            address.Id = id;
            return address;
        }

        public Address Update(Address address)
        {
            this.db.Execute("UPDATE Addresses " +
                "SET AddressType = @AddressType, " +
                "    StreetAddress = @StreetAddress, " +
                "    City = @City, " +
                "    StateId = @StateId, " +
                "    PostalCode = @PostalCode " +
                "WHERE Id = @Id", address);
            return address;
        }

        public int BulkInsertContacts(List<Contact> contacts)
        {
            this.EnsureOpenConnection();
            var sql =
                "INSERT INTO Contacts (FirstName, LastName, Email, Company, Title) VALUES(@FirstName, @LastName, @Email, @Company, @Title); " +
                "SELECT CAST(SCOPE_IDENTITY() as int)";
            return this.db.Execute(sql, contacts);
        }

        public List<Contact> GetContactsById(params int[] ids)
        {
            return this.db.Query<Contact>("SELECT * FROM Contacts WHERE ID IN @Ids", new { Ids = ids }).ToList();
        }

        public List<dynamic> GetDynamicById(params int[] ids)
        {
            return this.db.Query("SELECT * FROM Contacts WHERE ID IN @Ids", new { Ids = ids }).ToList();
        }

        #region Private Methods

        private void EnsureOpenConnection()
        {
            if (this.db.State != ConnectionState.Open)
            {
                this.db.Open();
            }
        }

        #endregion
    }
}
