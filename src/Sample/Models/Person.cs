using System.Collections.Generic;
using BlazorDB.DataAnnotations;

namespace Sample.Models
{
    public class Person
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [MaxLength(20)]
        public string LastName { get; set; }
        public Address HomeAddress { get; set; }
        public List<Address> OtherAddresses { get; set; }
    }
}