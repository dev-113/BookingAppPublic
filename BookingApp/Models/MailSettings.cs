#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace BookingApp.Models
{
    public class MailSettings
    {
        public string FromAddress { get; set; }
        public string Password { get; set; }
        public string ToAddress { get; set; }
        public string Cluster { get; set; }
    }
}
