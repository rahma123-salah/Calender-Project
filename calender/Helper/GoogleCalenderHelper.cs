using calender.DTO;
using calender.Models;
using calender.ViewModels;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Runtime;

namespace calender.Helper
{
    public class GoogleCalenderHelper
    {
        protected GoogleCalenderHelper()
        {

        }


        public static async Task<Event> CreateGoogleCalendar(GoogleCalender request)
        {
            string[] Scopes = { "https://www.googleapis.com/auth/calendar" };
            string ApplicationName = "Google Canlendar Api";
            UserCredential credential;
            using (var stream = new FileStream(Path.Combine(Directory.GetCurrentDirectory(),
                "calenderCredentials",
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
        
            string DrivecredPath = "D:\\rahma\\calender\\calender\\Drive\\Cred.json";
            GoogleCredential cred;
            using (var stream = new FileStream(DrivecredPath, FileMode.Open, FileAccess.Read))
            {
                cred = GoogleCredential.FromStream(stream).CreateScoped(new[]
                {
                    DriveService.ScopeConstants.DriveFile
                });

              
            }
            var driveService = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = cred,
                ApplicationName = "Google Canlendar Api"
            });
            string FolderId = "1fBeUMU2DL0KSnuwrjpyd8CRnuH-ljKhE";

            string FileToUpload = "D:\\rahma\\calender\\calender\\Attachments\\Rubber Duck vinyl decal.jfif";
            string fileId = UploadFile(DrivecredPath, FolderId, FileToUpload);
        
            string fileSharingLink = GetFileSharingLink(driveService, fileId);

            Event eventCalendar = new Event()
            {
                Summary = request.Summary,
                Location = request.Location,
                Description = request.Description,
                Attachments  = new EventAttachment[]
                {
                    new EventAttachment()
                    {
                        FileUrl = fileSharingLink
                    }
                },
               
                Start = new EventDateTime()
                {
                    DateTime = DateTime.Parse(request.StartDate_Time.ToString()),
                    TimeZone = "Africa/Cairo",
                },
                End = new EventDateTime()
                {
                    DateTime = DateTime.Parse(request.EndDate_Time.ToString()),
                    TimeZone = "Africa/Cairo",
                },
                Attendees = new EventAttendee[] {
                    new EventAttendee() { Email = "rahmameghed123@gmail.com" },

                },
                Reminders = new Event.RemindersData()
                {
                    UseDefault = false,
                    Overrides = new EventReminder[] {
                        new EventReminder() { Method = "email", Minutes = 10 },
                    }
                }
                

        };

            var eventRequest = services.Events.Insert(eventCalendar, "primary");
            var requestCreate = await eventRequest.ExecuteAsync();
            AttachFileToEvent(services, "primary", requestCreate.Id, fileId, fileSharingLink);

            return requestCreate;
        }
        static string GetFileSharingLink(DriveService service, string fileId)
        {
            var request = service.Files.Get(fileId);
            request.Fields = "webViewLink";
            var file = request.Execute();
            return file.WebViewLink;
        }
        static void AttachFileToEvent(CalendarService service, string calendarId, string eventId, string fileId , string url)
        {
            // Retrieve the existing event.
            Event existingEvent = service.Events.Get(calendarId, eventId).Execute();
            
            // Modify the event by adding the attachment.
            if (existingEvent.Attachments == null)
            {
                existingEvent.Attachments = new List<EventAttachment>();
            }

            // Add the file ID to the Attachments collection.
            existingEvent.Attachments.Add(new EventAttachment
            {
                FileId = fileId,
                FileUrl = url
            }) ;

            // Update the event with the modified data.
            var  updateRequest = service.Events.Update(existingEvent, calendarId, eventId);
            updateRequest.SupportsAttachments = true;
            updateRequest.Execute();
           
        }
        static string  UploadFile(string credPath, string FolderId, string FileToUpload)
        {
            GoogleCredential cred;
            using (var stream = new FileStream(credPath, FileMode.Open, FileAccess.Read))
            {
                cred = GoogleCredential.FromStream(stream).CreateScoped(new[]
                {
                    DriveService.ScopeConstants.DriveFile
                });

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer=cred,
                    ApplicationName = "Google Canlendar Api"
            });
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = Path.GetFileName(FileToUpload),
                    Parents = new List<string> { FolderId }
                };

                FilesResource.CreateMediaUpload request;
                using(var Stream = new FileStream(FileToUpload, FileMode.Open))
                {
                    request = service.Files.Create(fileMetadata, Stream, "");
                    request.Fields = "id";
                    request.Upload();
                }
                var file = request.ResponseBody;
                return file.Id;
            }






        }

    }



}


















