using System.Net.Mail;

namespace calender.ViewModels
{
    public class CalenderViewModel
    {
        public string EventId { get; set; }
        public string Location { get; set; }
        public string Summary { get; set; }
        public string Description { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string HtmlLink { get; set; }


    }
}
