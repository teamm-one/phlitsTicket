using DataAccess.IRepos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using Models.ViewModels;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;
using Utility;
//sk_test_51QXVljFKQdroNBxjROWk3ehl9ocBy6ISz3reSQDLt6c66A1Z5XVjiVEMzm4pj8Kw2PfLkbhK4w75KVGCAABpt2oU00dTqGWyTJ
//sk_test_51QXVpbIHVyjpqHREPbN4lpmU8cXos8dFRnaT2IZPjo3vHCiL4koSv1YvRu238WtXenCb2sLM2lVgkDAW4LK6YM9500pkFdl2lt
namespace PhlitsTicket.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TripIRepo _trip;
        private readonly SeatIRepo _seat;
        private readonly BookingIRepo _book;
        public HomeController(ILogger<HomeController> logger, TripIRepo trip, SeatIRepo seat,BookingIRepo book)
        {
            _logger = logger;
            _trip = trip;
            _seat = seat;
            _book = book;
        }

        public IActionResult Index(string from = null, string to = null)
        {
            if (from == null && to == null)
            {
                var trips = _trip.GetAll(additionalIncludes: e => e.Include(e => e.Airline)
                .ThenInclude(e => e.AirPortLeave)
                .Include(e => e.Airline)
                .ThenInclude(e => e.AirPortArrive)
                .Include(e => e.Flight)
                .ThenInclude(e => e.Seats), expression: e => e.Status != Status.Done).ToList();
                foreach (var i in trips)
                {
                    if (i.LeaveAt <= DateTime.Now)
                    {
                        i.Status = Status.Done;
                        _trip.Edit(i);
                        _trip.commit();
                    }
                }
                return View(trips);
            }
            else
            {
                var trips = _trip.GetAll(additionalIncludes: e => e.Include(e => e.Airline)
                .ThenInclude(e => e.AirPortLeave)
                .Include(e => e.Airline)
                .ThenInclude(e => e.AirPortArrive)
                .Include(e => e.Flight)
                .ThenInclude(e => e.Seats), expression: e => (e.Airline.AirPortLeave.City.Contains(from) || e.Airline.AirPortArrive.City.Contains(to)) && e.Status != Status.Done).ToList();
                foreach (var i in trips)
                {
                    if (i.LeaveAt <= DateTime.Now)
                    {
                        i.Status = Status.Done;
                        _trip.Edit(i);
                        _trip.commit();
                    }
                }
                return View(trips);
            }
        }
        [Authorize]
        public IActionResult Book(int id)
        {
            var trip = _trip.GetOne(e => e.TripId == id, additionalIncludes:
    e => e.Include(e => e.Airline)
        .ThenInclude(e => e.AirPortLeave)
        .Include(e => e.Airline)
        .ThenInclude(e => e.AirPortArrive)
        .Include(e => e.Flight)
        .ThenInclude(e => e.Seats)
    );
            if (trip != null)
            {
                return View(trip);
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        public IActionResult Pay(int tripId,string seat)
        {
            var trip = _trip.GetOne(e => e.TripId == tripId, additionalIncludes:
                e => e.Include(e => e.Airline)
                    .ThenInclude(e => e.AirPortLeave)
                    .Include(e => e.Airline)
                    .ThenInclude(e => e.AirPortArrive)
                    .Include(e => e.Flight)
                    .ThenInclude (e => e.Seats)
                );
            var chekSeat = _seat.GetOne(e => e.FlightId == trip.FlightId && e.Class == seat&&e.Availability==true);
            if (trip != null)
            {
               if(chekSeat != null)
                {
                    var options = new SessionCreateOptions
                    {
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<SessionLineItemOptions>(),
                        Mode = "payment",
                        SuccessUrl = $"{Request.Scheme}://{Request.Host}/Home/CreateBook?tripId={tripId}&seat={chekSeat.SeatID}",
                        CancelUrl = $"{Request.Scheme}://{Request.Host}/Home/Index",
                    };
                    options.LineItems.Add(
                        new SessionLineItemOptions
                        {
                            PriceData = new SessionLineItemPriceDataOptions
                            {
                                Currency = "Egp",
                                ProductData = new SessionLineItemPriceDataProductDataOptions
                                {
                                    Name = $"Tiket From Airport {trip.Airline.AirPortLeave.Name} To Airport {trip.Airline.AirPortArrive.Name}",
                                    Description = trip.Description
                                },
                                UnitAmount = trip.Price * 100
                            },
                            Quantity = 1,
                        }
                        );
                    var service = new SessionService();
                    var session = service.Create(options);
                    return Redirect(session.Url);
                }
                return Redirect($"{Request.Scheme}://{Request.Host}/Home/Book?id={tripId}");
            }
            return RedirectToAction("Index");
        }
        [Authorize]
        public IActionResult CreateBook(int tripId,int seat)
        {
            var getSeat=_seat.GetOne(e=>e.Availability==true&&e.SeatID==seat);
            if (getSeat != null)
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    Booking book = new Booking
                    {
                        BookingDate = DateTime.Now,
                        SeatId = getSeat.SeatID,
                        ApplicationUserId = userId,
                        TripId= tripId
                    };
                    _book.Create(book);
                    _book.commit();
                    getSeat.Availability= false;
                    _seat.Edit(getSeat);
                    _seat.commit();
                    return Redirect($"{Request.Scheme}://{Request.Host}/Booking/Index");
                }
                catch
                {
                    return Redirect($"{Request.Scheme}://{Request.Host}/Home/Book?id={tripId}");
                }
            }
            return Redirect($"{Request.Scheme}://{Request.Host}/Home/Book?id={tripId}");
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
