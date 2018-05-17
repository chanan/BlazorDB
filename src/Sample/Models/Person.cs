using System.Collections.Generic;

namespace Sample.Models
{
    public class Person
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Address HomeAddress { get; set; }
        public List<Address> OtherAddresses { get; set; }
    }
}