using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PRTDoctorClient
{
    public class MeetingDisplay
    {
        public string Id { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
        public string PatientId { get; set; }
    }
}
