using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendingJsonData.Model
{
    internal class WorkPermitSync
    {
        public int Id { get; set; }
        public DateTime Dat { get; set; }
        public string Isuer { get; set; }
        public string Recver { get; set; }
        public string Plnt { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }
        public string Typ { get; set; }
        public string CS { get; set; }
        public int WC { get; set; }
        public string Adress { get; set; }
        public string DevicId { get; set; }
    }
}
