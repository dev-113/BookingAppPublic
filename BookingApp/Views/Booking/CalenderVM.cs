#pragma warning disable CS8618
namespace BookingApp.Views.Booking
{
    public class CalenderVM
    {
        public DateTime Date { get; set; }
        public int RoomId { get; set; }
        public List<Models.Booking> Bookings { get; set; }
    }
}
