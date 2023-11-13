using Google.Apis.Calendar.v3.Data;
using System.ComponentModel.DataAnnotations;

namespace calender.Models
{
    public class GoogleCalender
    {
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }

        public DateTime StartDate_Time { get; set; }
        public DateTime EndDate_Time { get; set; }
    }
}
