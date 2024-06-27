using BookingApp.Models;

namespace BookingApp.Services
{
    public interface ISqlRepository
    {
        Task<bool> AddBooking(EventBookingModel eventBookingModel);
        Task<bool> AddSerialBooking(List<Booking> bookings);
        Task<List<Booking>> GetConfirmedBookingsByRoomId(int roomId);
        Task<bool> DeleteBooking(int bookingId);
        Task<bool> DeleteSerialBooking(string serialId);
        Task<List<Booking>> GetPendingBookings();
        Task<Booking> GetBookingById(int id);
        Task<List<Booking>> GetBookingsBySerialId(string serialId);
        Task<List<Booking>> GetAllBookingsByRoomId(int roomId);
    }
}
