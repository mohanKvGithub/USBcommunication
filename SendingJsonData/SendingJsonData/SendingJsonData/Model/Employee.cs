using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendingJsonData.Model
{
    internal class Employee
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
        public int PhoneNo { get; set; }
        public string Email { get; set; }
        public string LastName { get; set; }
        public int DepartmentId { get; set; }
        public string JobTitle { get; set; }
        public int WorkId { get; set; }
        public string DepartmentName { get; set; }
        public string Gender {  get; set; }
        public string BadgeNo { get; set; }
        public string CompanyName { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
