using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace MicroOrmDemo.DataLayer.Tests
{
    [TestFixture]
    public class ContactRepositoryTests
    {
        private  IContactRepository _contactRepository;
        static int id;
        [SetUp]
        public void Setup()
        {
            _contactRepository = new ContactRepository();
        }

        [Test, Order(1)]
        public void Get_All_Should_Return_6_Results()
        {
            var result = _contactRepository.GetAll();

            result.Should().NotBeNull();

            result.Count.Should().Be(6);
        }

        [Test, Order(2)]
        public void Insert_Should_assign_identity_to_new_entity()
        {
            //arrange
            var contact = new Contact
            {
                FirstName = "Joe",
                LastName = "Blow",
                Email = "joe.blow@gmail.com",
                Company = "Microsoft",
                Title = "Developer"
            };
            Address address = new Address
            {
                AddressType = "Home",
                StreetAddress = "123 Main Street",
                City = "Baltimore",
                StateId = 1,
                PostalCode = "22222"
            };
            contact.Addresses.Add(address);

            // act
            //_contactRepository.Add(contact);
            _contactRepository.Save(contact);

            // assert
            contact.Id.Should().NotBe(0, "because Identity should have been assigned by database.");
            id = contact.Id;
        }

        [Test, Order(3)]
        public void Find_should_retrieve_existing_entity()
        {       
            // act
            //var contact = _contactRepository.Find(id);
            var contact = _contactRepository.GetFullContact(id);

            // assert
            contact.Should().NotBeNull();
            contact.Id.Should().Be(id);
            contact.FirstName.Should().Be("Joe");
            contact.LastName.Should().Be("Blow");
            contact.Email.Should().Be("joe.blow@gmail.com");
            contact.Company.Should().Be("Microsoft");
            contact.Title.Should().Be("Developer");

            contact.Addresses.Count.Should().Be(1);
            contact.Addresses.First().StreetAddress.Should().Be("123 Main Street");
        }
        [Test, Order(4)]
        public void Modify_should_update_existing_entity()
        {     
            // act
            //var contact = _contactRepository.Find(id);
            var contact = _contactRepository.GetFullContact(id);
            contact.FirstName = "Bob";
            contact.Addresses[0].StreetAddress = "456 Main Street";
            //_contactRepository.Update(contact);
            _contactRepository.Save(contact);

            // create a new repository for verification purposes
            IContactRepository repository2 = new ContactRepository();
            //var modifiedContact = repository2.Find(id);
            var modifiedContact = repository2.GetFullContact(id);

            // assert
            modifiedContact.FirstName.Should().Be("Bob");
            modifiedContact.Addresses.First().StreetAddress.Should().Be("456 Main Street");
        }

        [Test, Order(5)]
        public void Delete_should_remove_entity()
        {
          
            // act
            _contactRepository.Remove(id);

            // create a new repository for verification purposes
            IContactRepository repository2 = new ContactRepository();
            var deletedEntity = repository2.Find(id);

            // assert
            deletedEntity.Should().BeNull();
        }

        [Test]
        [NUnit.Framework.Ignore("no need wach time")]
        public void Bulk_insert_should_insert_4_rows()
        {
            var contacts = new List<Contact> {
                new Contact { FirstName = "Charles", LastName = "Barkley" },
                new Contact { FirstName = "Scottie", LastName = "Pippen" },
                new Contact { FirstName = "Tim", LastName = "Duncan" },
                new Contact { FirstName = "Patrick", LastName = "Ewing" }
            };
            //act
            var rowsAffected = _contactRepository.BulkInsertContacts(contacts);

            //assert
            rowsAffected.Should().Be(4);
        }

        [Test]
        public void List_support_should_produce_corerect_results()
        {
            //act 
            var contacts = _contactRepository.GetContactsById(1, 2, 3);

            //assert
            contacts.Count.Should().Be(3);
        }

        [Test]
        public void Dynamic_support_should_produce_corerect_results()
        {
            //act 
            var contacts = _contactRepository.GetDynamicById(1, 2, 3);

            //assert
            contacts.Count.Should().Be(3);
            Assert.AreEqual("Jordan", contacts.First().LastName);
        }
    }
}
