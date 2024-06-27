#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace BookingApp.Models
{
    public class EventBookingModel
    {
        public DateTime SelectedDate { get; set; }
        public string FromTime { get; set; }
        public string ToTime { get; set; }
        public string Topic { get; set; }
        public string EventInfo { get; set; }
        public string BookingType { get; set; }
        public string DayOfWeek { get; set; }
        public string TimeSpan { get; set; }
        public int TimeQuantity { get; set; }
        public int RoomId { get; set; }
        public bool IsAdmin { get; set; }
        public string User { get; set; }
    }
}
