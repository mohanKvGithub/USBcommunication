using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendingJsonData.Model
{
    internal class WorkPermit
    {
        public int Id { get; set; }
        public int WorkPermitTypeId { get; set; }
        public string WorkPermitType { get; set; }
        public string Compliance { get; set; }
        public int ReceiverId { get; set; }
        public int IssuerId { get; set; }
        public string IssuerName { get; set; }
        public string ReceiverName { get; set; }
        public DateTime PlannedStartDate { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
        public int DeviceId { get; set; }
        public string DeviceName { get; set; }
        public int LocationId {  get; set; }
        public string LocationName { get; set; }
        public int WorkId {  get; set; }
        public List<Worker> Workers { get; set; }
    }
    class Worker
    {
        public string Name { get; set; }
        public int EmployeeId {  get; set; }
    }
}
