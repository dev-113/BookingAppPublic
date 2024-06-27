using BookingApp.Helper;
using BookingApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookingApp.Models
{
    public class SqlRepository : ISqlRepository
    {
        private readonly StpetrusSeDb1Context _bookingContext;

        public SqlRepository(StpetrusSeDb1Context bookingContext)
        {
            _bookingContext = bookingContext;
        }

        public async Task<bool> AddBooking(EventBookingModel eventBookingModel)
        {
            try
            {
                var booking = new Booking()
                {
                    Topic = eventBookingModel.Topic,
                    Info = eventBookingModel.EventInfo,
                    Date = DateOnly.FromDateTime(eventBookingModel.SelectedDate),
                    FromTime = eventBookingModel.FromTime,
                    ToTime = eventBookingModel.ToTime,
                    Room = eventBookingModel.RoomId,
                    Status = eventBookingModel.IsAdmin ? Constants.Status.Confirmed : Constants.Status.Pending,
                    BookingType = eventBookingModel.BookingType,
                    User = eventBookingModel.User,
                };
                await _bookingContext.Bookings.AddAsync(booking);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString(), "failed to add booking to db");
                return false;
            }
        }

        public async Task<bool> AddSerialBooking(List<Booking> bookings)
        {
            try
            {
                await _bookingContext.Bookings.AddRangeAsync(bookings);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString(), "failed to add booking to db");
                return false;
            }
        }

        public async Task<bool> DeleteBooking(int bookingId)
        {
            try
            {
                var booking = await _bookingContext.Bookings
                    .FirstOrDefaultAsync(x => x.Id == bookingId);

                if (booking == null) return false;

                _bookingContext.Bookings.Remove(booking);
                await _bookingContext.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("failed to delete booking from db", e.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteSerialBooking(string serialId)
        {
            try
            {
                var bookings = await _bookingContext.Bookings
                    .Where(x => x.SerialId == serialId)
                    .ToListAsync();

                if (!bookings.Any()) return false;

                _bookingContext.Bookings.RemoveRange(bookings);
                await _bookingContext.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("failed to delete serial from db", e.ToString());
                return false;
            }
        }

        public async Task<List<Booking>> GetAllBookingsByRoomId(int roomId)
        {
            var bookings = await _bookingContext.Bookings
                .Where(x => x.Room == roomId)
                .ToListAsync();

            return bookings;
        }

        public async Task<List<Booking>> GetConfirmedBookingsByRoomId(int roomId)
        {
            var bookings = await _bookingContext.Bookings
                .Where(x => x.Room == roomId && x.Status == Constants.Status.Confirmed)
                .ToListAsync();

            return bookings;
        }

        public async Task<List<Booking>> GetPendingBookings()
        {
            var bookings = await _bookingContext.Bookings
                .Where(x => x.Status == Constants.Status.Pending && x.BookingType == Constants.BookingType.Standard)
                .ToListAsync();

            var serialBookings = await _bookingContext.Bookings
                .Where(x => x.Status == Constants.Status.Pending && x.BookingType == Constants.BookingType.Serie)
                .GroupBy(x => x.SerialId)
                .Select(group => group.OrderBy(b => b.Date).First())
                .ToListAsync();

            bookings.AddRange(serialBookings);
            var sortedBookings = bookings.OrderBy(b => b.Date).ToList();
            return bookings;
        }

        public async Task<Booking> GetBookingById(int id)
        {
            var booking = await _bookingContext.Bookings
                .FirstOrDefaultAsync(x => x.Id == id);

            if (booking == null) return null;
            return booking;
        }

        public async Task<List<Booking>> GetBookingsBySerialId(string serialId)
        {
            var bookings = await _bookingContext.Bookings
                .Where(x => x.SerialId == serialId)
                .ToListAsync();

            if (bookings == null || !bookings.Any()) return null;
            return bookings;
        }
    }
}
