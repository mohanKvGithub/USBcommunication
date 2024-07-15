using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendingJsonData.Model
{
    internal class WorkerSync
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Fn { get; set; }
        public string Mn { get; set; }
        public string Ln { get; set; }
        public string Status { get; set; }
        public string Gndr { get; set; }
        public string Email { get; set; }
        public string Dep { get; set; }
        public string BadgNo { get; set; }
        public string Company { get; set; }
        public string Role { get; set; }
        public int WpId { get; set; }
        public int MobileNo {  get; set; }
    }
}
