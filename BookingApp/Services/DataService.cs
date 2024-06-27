using BookingApp.Models;
using BookingApp.Views.Booking;
using Microsoft.AspNetCore.Identity;
using System.Net.Mail;
using System.Net;
using BookingApp.Helper;
using System.Globalization;
using Microsoft.Extensions.Options;

namespace BookingApp.Services
{
    public class DataService : IDataService
    {
        private readonly ILogger<DataService> _logger;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly StpetrusSeDb1Context _bookingContext;
        private readonly ISqlRepository _sqlRepository;
        private readonly MailSettings _mailSettings;

        public DataService(ILogger<DataService> logger, SignInManager<IdentityUser> signInManager, StpetrusSeDb1Context bookingContext, ISqlRepository sqlRepository, IOptions<MailSettings> mailSettings)
        {
            _logger = logger;
            _signInManager = signInManager;
            _bookingContext = bookingContext;
            _sqlRepository = sqlRepository;
            _mailSettings = mailSettings.Value;
        }

        public async Task<string> SignupAsync(SignupVM signupVM)
        {
            var user = await _signInManager.UserManager.FindByNameAsync(signupVM.Username);
            if (user != null) _logger.LogInformation($"username already exists in database");
            var newUser = new IdentityUser()
            {
                UserName = signupVM.Username,
            };
            var createUser = await _signInManager.UserManager.CreateAsync(newUser, signupVM.Password);
            var result = createUser.Succeeded ? null : createUser.Errors.First().Description;
            return result;
        }

        public async Task<bool> LoginAsync(LoginVM loginVM)
        {
            var result = await _signInManager.PasswordSignInAsync(loginVM.Username, loginVM.Password, false, false);
            return result.Succeeded;
        }

        public async Task<bool> LogoutAsync()
        {
            await _signInManager.SignOutAsync();
            return true;
        }

        public async Task<bool> AddBooking(EventBookingModel eventBookingModel)
        {
            try
            {
                var confirmedBookings = await _sqlRepository.GetConfirmedBookingsByRoomId(eventBookingModel.RoomId);
                var pendingBookings = await _sqlRepository.GetPendingBookings();

                if (confirmedBookings == null || pendingBookings == null)
                {
                    _logger.LogInformation("Failed to fetch confirmed or pending bookings.");
                    return false;
                }

                if (!IsBookingSlotAvailable(eventBookingModel, confirmedBookings, pendingBookings))
                {
                    _logger.LogInformation("Booking slot is not available.");
                    return false;
                }

                return eventBookingModel.BookingType == Constants.BookingType.Serie
                    ? await AddRecurringBookings(eventBookingModel, confirmedBookings, pendingBookings)
                    : await AddSingleBooking(eventBookingModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding booking.");
                return false;
            }
        }

        public async Task<bool> SendBookingNotification()
        {
            try
            {
                using var message = new MailMessage()
                {
                    From = new MailAddress(_mailSettings.FromAddress),
                    Subject = "Boknings notifiering",
                    Body = "Var god och kontrollera nya bokningar"
                };
                message.To.Add(_mailSettings.ToAddress);

                using var smtpClient = new SmtpClient(_mailSettings.Cluster)
                {
                    Port = 587,
                    Credentials = new NetworkCredential(_mailSettings.FromAddress, _mailSettings.Password),
                    EnableSsl = true
                };

                await smtpClient.SendMailAsync(message);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send booking notification.");
                return false;
            }
        }

        public async Task<bool> ConfirmBooking(int id)
        {
            try
            {
                var booking = await _sqlRepository.GetBookingById(id);
                if (booking == null || booking.Status == Constants.Status.Confirmed)
                    return false;

                booking.Status = Constants.Status.Confirmed;
                _bookingContext.Update(booking);
                await _bookingContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while confirming booking.");
                return false;
            }
        }

        public async Task<bool> ConfirmSerialBooking(string serialId)
        {
            try
            {
                var bookings = await _sqlRepository.GetBookingsBySerialId(serialId);
                if (bookings == null || bookings.All(x => x.Status == Constants.Status.Confirmed))
                    return false;

                foreach (var booking in bookings)
                {
                    booking.Status = Constants.Status.Confirmed;
                }

                _bookingContext.UpdateRange(bookings);
                await _bookingContext.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while confirming serial booking.");
                return false;
            }
        }

        public async Task<IEnumerable<object>> GetEventsForRoom(int roomId)
        {
            var bookings = await _sqlRepository.GetAllBookingsByRoomId(roomId);

            var events = bookings.Select(booking => new
            {
                id = booking.Id,
                topic = booking.Topic,
                info = booking.Info,
                fromTime = booking.FromTime,
                toTime = booking.ToTime,
                start = booking.Date.ToDateTime(TimeOnly.Parse($"{booking.FromTime}")).ToString(),
                end = booking.Date.ToDateTime(TimeOnly.Parse($"{booking.ToTime}")).ToString(),
                status = booking.Status,
                bookingType = booking.BookingType,
                serialId = booking.SerialId,
                user = booking.User,
            }).ToList();

            return events;
        }


        private async Task<bool> AddSingleBooking(EventBookingModel eventBookingModel)
        {
            try
            {
                var result = await _sqlRepository.AddBooking(eventBookingModel);
                if (!result)
                    return false;

                await _bookingContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding single booking.");
                return false;
            }
        }

        private async Task<bool> AddRecurringBookings(EventBookingModel eventBookingModel, IEnumerable<Booking> confirmedBookings, IEnumerable<Booking> pendingBookings)
        {
            try
            {
                var endDate = eventBookingModel.TimeSpan.Equals("Months", StringComparison.OrdinalIgnoreCase)
                    ? eventBookingModel.SelectedDate.AddMonths(eventBookingModel.TimeQuantity)
                    : eventBookingModel.SelectedDate.AddDays(eventBookingModel.TimeQuantity * 7);

                var serialId = Guid.NewGuid().ToString();
                var bookings = GenerateRecurringBookings(eventBookingModel, endDate, serialId);

                if (bookings.Any(booking => !IsBookingSlotAvailable(booking, confirmedBookings, pendingBookings)))
                {
                    _logger.LogInformation("One or more booking slots are not available.");
                    return false;
                }

                var serialResult = await _sqlRepository.AddSerialBooking(bookings);
                if (!serialResult)
                {
                    _logger.LogInformation("Failed to add serial bookings.");
                    return false;
                }

                await _bookingContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding recurring bookings.");
                return false;
            }
        }

        private List<Booking> GenerateRecurringBookings(EventBookingModel eventBookingModel, DateTime endDate, string serialId)
        {
            var bookings = new List<Booking>();

            do
            {
                if (eventBookingModel.DayOfWeek.Equals("EveryDay", StringComparison.OrdinalIgnoreCase) ||
                    eventBookingModel.SelectedDate.DayOfWeek.ToString().Equals(eventBookingModel.DayOfWeek, StringComparison.OrdinalIgnoreCase))
                {
                    bookings.Add(new Booking()
                    {
                        Topic = eventBookingModel.Topic,
                        Info = eventBookingModel.EventInfo,
                        Date = DateOnly.FromDateTime(eventBookingModel.SelectedDate),
                        FromTime = eventBookingModel.FromTime,
                        ToTime = eventBookingModel.ToTime,
                        Room = eventBookingModel.RoomId,
                        Status = eventBookingModel.IsAdmin ? Constants.Status.Confirmed : Constants.Status.Pending,
                        BookingType = eventBookingModel.BookingType,
                        SerialId = serialId,
                        User = eventBookingModel.User,
                    });
                }
                eventBookingModel.SelectedDate = eventBookingModel.SelectedDate.AddDays(1);
            } while (eventBookingModel.SelectedDate <= endDate);

            return bookings;
        }

        private bool IsBookingSlotAvailable(EventBookingModel eventBookingModel, IEnumerable<Booking> confirmedBookings, IEnumerable<Booking> pendingBookings)
        {
            var fromDateTime = DateTime.ParseExact(eventBookingModel.FromTime, "HH:mm", CultureInfo.InvariantCulture);
            var toDateTime = DateTime.ParseExact(eventBookingModel.ToTime, "HH:mm", CultureInfo.InvariantCulture);

            return !confirmedBookings.Any(x => IsOverlapping(x, eventBookingModel.SelectedDate, fromDateTime, toDateTime, eventBookingModel.RoomId)) &&
                   !pendingBookings.Any(x => IsOverlapping(x, eventBookingModel.SelectedDate, fromDateTime, toDateTime, eventBookingModel.RoomId));
        }

        private bool IsBookingSlotAvailable(Booking booking, IEnumerable<Booking> confirmedBookings, IEnumerable<Booking> pendingBookings)
        {
            var selectedDate = DateTime.Parse(booking.Date.ToString());
            var fromDateTime = DateTime.ParseExact(booking.FromTime, "HH:mm", CultureInfo.InvariantCulture);
            var toDateTime = DateTime.ParseExact(booking.ToTime, "HH:mm", CultureInfo.InvariantCulture);

            return !confirmedBookings.Any(x => IsOverlapping(x, selectedDate, fromDateTime, toDateTime, booking.Room)) &&
                   !pendingBookings.Any(x => IsOverlapping(x, selectedDate, fromDateTime, toDateTime, booking.Room));
        }

        private bool IsOverlapping(Booking booking, DateTime selectedDate, DateTime fromDateTime, DateTime toDateTime, int roomId)
        {
            return booking.Date == DateOnly.FromDateTime(selectedDate) &&
                   DateTime.ParseExact(booking.FromTime, "HH:mm", CultureInfo.InvariantCulture) < toDateTime &&
                   DateTime.ParseExact(booking.ToTime, "HH:mm", CultureInfo.InvariantCulture) > fromDateTime &&
                   booking.Room == roomId;
        }
    }
}