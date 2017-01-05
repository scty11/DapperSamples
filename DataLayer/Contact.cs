using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MicroOrmDemo.DataLayer
{
    public class Contact
    {
        public Contact()
        {
            this.Addresses = new List<Address>();
        }

        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string Title { get; set; }

        public List<Address> Addresses { get; private set; }

        public bool IsNew
        {
            get
            {
                return this.Id == default(int);
            }
        }
    }
}
