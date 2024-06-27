using BookingApp.Models;
using BookingApp.Services;
using BookingApp.Views.Booking;
using Microsoft.AspNetCore.Mvc;

namespace BookingApp.Controllers
{
    public class BookingController : Controller
    {
        private readonly ILogger<BookingController> _logger;
        private readonly IDataService _dataService;
        private readonly ISqlRepository _sqlRepository;
        private readonly IConfiguration _configuration;

        public BookingController(ILogger<BookingController> logger, IDataService dataService, ISqlRepository sqlRepository, IConfiguration configuration)
        {
            _logger = logger;
            _dataService = dataService;
            _sqlRepository = sqlRepository;
            _configuration = configuration;
        }

        [HttpGet("")]
        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync(LoginVM loginVM)
        {
            if (!ModelState.IsValid)
                return View();

            var success = await _dataService.LoginAsync(loginVM);
            if (!success)
            {
                ModelState.AddModelError(string.Empty, "Fel användarnamn eller lösenord");
                return View();
            }

            return RedirectToAction(nameof(Rooms));
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _dataService.LogoutAsync();
            if (!result) return BadRequest();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet("signup")]
        public IActionResult Signup()
        {
            return View();
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignupAsync(SignupVM signupVM)
        {
            if (!ModelState.IsValid) return View();
            var isErrorMessage = await _dataService.SignupAsync(signupVM);
            if (!string.IsNullOrWhiteSpace(isErrorMessage))
            {
                ModelState.AddModelError(string.Empty, isErrorMessage);
                return View();
            }

            // Set success message in TempData
            TempData["SuccessMessage"] = "Användare har lagts till!";

            return View();
        }

        [HttpGet("rooms")]
        public IActionResult Rooms()
        {
            var rooms = _configuration.GetSection("Rooms").GetChildren();
            var dictionary = new Dictionary<string, string>();
            foreach (var room in rooms)
            {
                dictionary[room.Key] = room.Value;
            }

            ViewData["Rooms"] = dictionary;

            return View();
        }

        [HttpGet("room/{roomKey}")]
        public IActionResult Room(string roomKey)
        {
            var room = _configuration.GetSection("Rooms").GetValue<string>(roomKey);
            if (room == null)
            {
                return NotFound(); // Return a 404 Not Found if the room is not found
            }

            if (!int.TryParse(roomKey.Remove(0, roomKey.Length - 1), out var roomId)) return NotFound();
            ViewData["room"] = room;
            var model = new CalenderVM { RoomId = roomId };
            return View("_partialViewCalender", model);
        }

        [HttpGet("GetEvents/{roomId}")]
        public async Task<IActionResult> GetEvents(int roomId)
        {
            var events = await _dataService.GetEventsForRoom(roomId);
            return Json(events);
        }

        [HttpPost("bookevent")]
        public async Task<IActionResult> BookEventAsync(EventBookingModel eventBookingModel)
        {
            var result = await _dataService.AddBooking(eventBookingModel);
            if (!result) return Json(new { error = "Boknings förfrågan misslyckades, dag och tid existerar redan i en annan bokning" });

            if (!eventBookingModel.IsAdmin)
            {
                var emailSent = await _dataService.SendBookingNotification();
                if (!emailSent) Console.WriteLine("Misslyckades att skicka mejl notifiering");
            }

            return Json(new { success = "Bokning lyckades" });
        }

        [HttpDelete("deleteevent/{id}")]
        public async Task<IActionResult> DeleteEventAsync(int id)
        {
            var result = await _sqlRepository.DeleteBooking(id);
            if (!result) return Json(new { error = "Misslyckades att radera bokning" });

            return Json(new { success = "Radera bokning lyckades" });
        }

        [HttpDelete("deleteserialevent/{serialId}")]
        public async Task<IActionResult> DeleteEventAsync(string serialId)
        {
            var result = await _sqlRepository.DeleteSerialBooking(serialId);
            if (!result) return Json(new { error = "Misslyckades att radera serie" });

            return Json(new { success = "Radera serie lyckades" });
        }

        public async Task<IActionResult> PendingBookingsAsync()
        {
            var rooms = _configuration.GetSection("Rooms").GetChildren();
            var dictionary = new Dictionary<string, string>();
            foreach (var room in rooms)
            {
                dictionary[room.Key] = room.Value;
            }

            ViewData["Rooms"] = dictionary;

            // Get pending bookings
            var pendingBookings = await _sqlRepository.GetPendingBookings();

            // Pass the pending bookings to the view
            return View(pendingBookings);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmBookingAsync(int bookingId, string serialId)
        {
            if (!string.IsNullOrWhiteSpace(serialId))
            {
                var serialIsConfirmed = await _dataService.ConfirmSerialBooking(serialId);
                if (!serialIsConfirmed) return Json(new { error = "Bekräftelse misslyckades" });
                // Redirect back to the pending bookings page
                return RedirectToAction("PendingBookings");
            }

            var isConfirmed = await _dataService.ConfirmBooking(bookingId);
            if (!isConfirmed) return Json(new { error = "Bekräftelse misslyckades" });
            // Redirect back to the pending bookings page
            return RedirectToAction("PendingBookings");
        }
    }
}
