using calender.DTO;
using calender.Helper;
using calender.Models;
using calender.Service;
using calender.ViewModels;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace calender.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CalenderController : ControllerBase
    {
        private readonly GoogleCalendarService _googleCalendarService;
     
        public CalenderController(GoogleCalendarService googleCalendarService)
        {
            _googleCalendarService = googleCalendarService ?? throw new ArgumentNullException(nameof(googleCalendarService));

        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GoogleCalender request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                if (request.StartDate_Time.DayOfWeek != DayOfWeek.Friday && request.StartDate_Time.DayOfWeek != DayOfWeek.Saturday)
                {
                    if (request.StartDate_Time < DateTime.Now)
                    {
                        return BadRequest("cant create event in the past");

                    }
                    else if (request.EndDate_Time <= request.StartDate_Time)
                    {
                        return BadRequest("cant create end date before start date");

                    }
                    else
                    {
                        var calenderData = await GoogleCalenderHelper.CreateGoogleCalendar(request);
                        CalenderViewModel data = new()
                        {
                            Summary = calenderData.Summary,
                            Description = calenderData.Description,
                            Location = calenderData.Location,
                            Start = calenderData.Start.DateTime.ToString(),
                            End = calenderData.End.DateTime.ToString(),
                            HtmlLink = calenderData.HtmlLink,
                            EventId = calenderData.Id,

                        };
                        return Created(data.HtmlLink, data);
                    }


                }
                else
                {
                    return BadRequest("cant create event in friday or SATERDAY");
                }
            }
            catch (FormatException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpGet("ViewAll")]
        public async Task<IActionResult> View(
            [FromQuery] FilterParametersDto FilterParams 
            //[FromQuery] int pageSize = 2,
            //[FromQuery] string pageToken = null 
            )
        {
            try
            {
                CalendarService calendarService = _googleCalendarService.GetCalendarService();


                string StartDate = FilterParams.start_date;
                string Location = FilterParams.location;
                //string searchQuery = FilterParams.searchQuery;


                bool IsLocationNull = string.IsNullOrWhiteSpace(Location);
                bool IsStartdateNull = string.IsNullOrWhiteSpace(StartDate);
                //bool IssearchQueryNull = string.IsNullOrWhiteSpace(searchQuery);



                EventsResource.ListRequest events = calendarService.Events.List("primary");
                //events.PageToken = pageToken;
                //events.MaxResults = pageSize;

                Events eventsList = await events.ExecuteAsync();
                var FilteredList = eventsList.Items.Select(
                calenderData => new CalenderViewModel
                {
                    Summary = calenderData.Summary,
                    Description = calenderData.Description,
                    Location = calenderData.Location,
                    Start = calenderData.Start.DateTime.ToString(),
                    End = calenderData.End.DateTime.ToString(),
                    HtmlLink = calenderData.HtmlLink,
                    EventId = calenderData.Id,
                }) ; 
                 
                if (IsLocationNull && IsStartdateNull )
                {
                    var response = new
                    {
                        Items = FilteredList,
                        //NextPageToken = eventsList.NextPageToken
                    };
                    return Ok(response);
                }
                if (!IsStartdateNull && !IsLocationNull)
                {
                    StartDate = StartDate.Trim();
                    Location = Location.Trim();
                    var FilteredEvent = FilteredList.Where(s => s.Start.ToString().Contains(StartDate) && s.Location == Location);
                    var response = new
                    {
                        Items = FilteredEvent,
                        //NextPageToken = eventsList.NextPageToken
                    };
                    return Ok(response);
                }

                if (!IsLocationNull)
                {
                    Location = Location.Trim();
                    var FilteredSummary = FilteredList.Where(s => s.Location == Location);
                    FilteredList = FilteredSummary;
                }
                if (!IsStartdateNull)
                {
                    StartDate = StartDate.Trim();
                    var Filteredate = FilteredList.Where(s => s.Start.ToString().Contains(StartDate));
                    FilteredList = Filteredate;
                }
                //if (!IssearchQueryNull)
                //{
                //    searchQuery = searchQuery.Trim();
                //    var searchResault = FilteredList.Where(s => s.Start.ToString().Contains(searchQuery) || s.Summary.Contains(searchQuery) || s.Location.Contains(searchQuery));
                //    FilteredList = searchResault;
                //}
                var responsefilter = new
                {
                    Items = FilteredList,
                    //NextPageToken = eventsList.NextPageToken
                };
                return Ok(responsefilter);


            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }

        }




        [HttpGet]
        public async Task<IActionResult> GetEvents( [FromQuery] int pageSize = 2, [FromQuery] string pageToken = null)
        {
            try
            {


                string[] Scopes = { "https://www.googleapis.com/auth/calendar" };
                string ApplicationName = "Google Canlendar Api";
                UserCredential credential;
                using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(), "calenderCredentials",
                    "client_secret_887718584993-aokj0ahl4sdn3posbrfuq5dfe176m8do.apps.googleusercontent.com.json"), FileMode.Open, FileAccess.Read))
                {
                    string credPath = "token.json";
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(

                        GoogleClientSecrets.Load(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true)).Result;
                }
                var services = new CalendarService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                EventsResource.ListRequest request = services.Events.List("primary");
                request.PageToken = pageToken;
                request.MaxResults = pageSize;

                Events events = await request.ExecuteAsync();

                if (events != null && events.Items.Count > 0)
                {


                    var FilteredList = events.Items.Select(
                                       calenderData => new CalenderViewModel
                                       {
                                           Summary = calenderData.Summary,
                                           Description = calenderData.Description,
                                           Location = calenderData.Location,
                                           Start = calenderData.Start.DateTime.ToString(),
                                           End = calenderData.End.DateTime.ToString(),
                                           HtmlLink = calenderData.HtmlLink,
                                           EventId = calenderData.Id
                                       });


                    var response = new
                    {
                        Items = FilteredList,
                        NextPageToken = events.NextPageToken
                    };
                    return Ok(response);
                }

                else
                {
                    return BadRequest("No upcoming events found.");
                }

              
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }
    
        [HttpDelete("{evId}")]
        public IActionResult Detete(string evId)
        {
            try {


                CalendarService calendarService = _googleCalendarService.GetCalendarService();

                var checkEvent = calendarService.Events.Delete("primary", evId).Execute();
                return NoContent();


            }
            catch (Google.GoogleApiException apiException)
            {
                if (apiException.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                   return BadRequest("The requested calendar or event was not found.");
                }
                else
                {
                     return BadRequest($"An error occurred: {apiException.Message}");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"An unexpected error occurred: {ex.Message}");
            }


        }


    }
}



