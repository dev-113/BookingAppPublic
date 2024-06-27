using BookingApp.Models;
using BookingApp.Views.Booking;

namespace BookingApp.Services
{
    public interface IDataService
    {
        Task<string> SignupAsync(SignupVM signupVM);
        Task<bool> LoginAsync(LoginVM loginVM);
        Task<bool> LogoutAsync();
        Task<bool> AddBooking(EventBookingModel eventBookingModel);
        Task<bool> SendBookingNotification();
        Task<bool> ConfirmBooking(int id);
        Task<bool> ConfirmSerialBooking(string serialId);
        Task<IEnumerable<object>> GetEventsForRoom(int roomId);
    }
}
