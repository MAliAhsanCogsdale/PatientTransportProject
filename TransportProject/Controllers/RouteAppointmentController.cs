using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;
using TransportProject.ViewModels;
using UglyToad.PdfPig;
//using Route = TransportProject.Models.Route;
using Newtonsoft.Json;
using System.Text;

namespace TransportProject.Controllers
{
    //[Authorize(Roles = "Admin,Dispatcher")]
    public class RouteAppointmentController : Controller
    {
        //private readonly IRouteAppointmentRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RouteAppointmentController> _logger;
        private readonly string _googleApiKey;
        private readonly HttpClient _httpClient;

        public RouteAppointmentController(
            //IRouteAppointmentRepository repository,
            ApplicationDbContext context,
            ILogger<RouteAppointmentController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            //_repository = repository;
            _context = context;
            _logger = logger;
            _googleApiKey = configuration["GoogleMaps:ApiKey"] ?? throw new Exception("Google API Key missing");
            _httpClient = httpClientFactory.CreateClient();
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1, int pageSize = 100,
            string searchDriver = "", string searchPatient = "",
            DateTime? date = null, bool selectAll = false)
        {
            try
            {
                var query = _context.Appointments
                .Include(a => a.Patient)
                .Include(a => a.Hospital)
                .Include(a => a.Driver)
                .Where(a => a.IsActive);

                if (!string.IsNullOrEmpty(searchDriver))
                {
                    query = query.Where(a => a.Driver != null &&
                        (a.Driver.FirstName + " " + a.Driver.LastName).Contains(searchDriver));
                }

                if (!string.IsNullOrEmpty(searchPatient))
                {
                    query = query.Where(a => a.Patient != null &&
                        (a.Patient.FirstName + " " + a.Patient.LastName).Contains(searchPatient));
                }

                if (date.HasValue)
                {
                    query = query.Where(a => a.PickupTime.Date == date.Value.Date);
                }

                var totalRecords = await query.CountAsync();

                var appointments = await query
                    .OrderBy(a => a.PickupTime)
                    .ThenBy(a => a.SequenceOrder)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                //var viewModel = new AppointmentListVM
                //{
                //    Items = appointments.Select(a => new AppointmentItemVM
                //    {
                //        Id = a.Id,
                //        DriverName = a.Driver != null ? $"{a.Driver.FirstName} {a.Driver.LastName}" : "Unassigned",
                //        PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "",
                //        PickupTime = a.PickupTime,
                //        PickupAddress = a.PickupAddress,
                //        DropOffTime = a.DropOffTime,
                //        DropOffAddress = a.DropOffAddress ?? a.Hospital?.Name ?? "",
                //        HospitalName = a.Hospital?.Name ?? "",
                //        SequenceOrder = a.SequenceOrder ?? 0,
                //        LOS = a.LOS,
                //        CPay = a.CPay,
                //        Miles = a.Miles,
                //        Status = a.Status
                //    }).ToList(),
                //    CurrentPage = page,
                //    PageSize = pageSize,
                //    TotalRecords = totalRecords
                //};

                //return View(viewModel);
                var viewModel = new RouteAppointmentListVM
                {
                    Items = appointments.Select(a => new RouteAppointmentVM
                    {
                        Id = a.Id,
                        DriverName = a.Driver != null ? $"{a.Driver.FirstName} {a.Driver.LastName}" : "Unassigned",
                        PatientName = a.Patient != null ? $"{a.Patient.FirstName} {a.Patient.LastName}" : "",
                        PickupTime = a.PickupTime,
                        PickupAddress = a.PickupAddress,
                        HospitalName = a.Hospital?.Name ?? "",
                        SequenceOrder = a.SequenceOrder,
                        LOS = a.LOS ?? "",
                        CPay = a.CPay ?? 0,
                        PCA = a.PCA ?? 0,
                        AESC = a.AESC ?? 0,
                        CESC = a.CESC ?? 0,
                        Seats = a.Seats ?? 0,
                        Miles = a.Miles ?? 0,
                        Notes = a.Notes ?? ""
                    }).ToList(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords,
                    SearchDriver = searchDriver,
                    SearchPatient = searchPatient,
                    Date = date,
                    SelectAll = selectAll
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index failed");
                return StatusCode(500, "Error loading data");
            }
        }

        // ================= IMPORT =================
        public IActionResult ImportFile() => View();

        [HttpPost]
        public async Task<IActionResult> ImportFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                var text = ExtractPdfText(file);

                var trips = ParseTripsV2(text)
                    .Where(t => t.PickupTime != null)
                    .OrderBy(t => t.PickupTime)
                    .ToList();

                var drivers = await _context.Drivers
                    .Where(d => d.IsActive && d.IsAvailable)
                    .ToListAsync();

                var geoCache = new Dictionary<string, (double lat, double lng)>();
                var distanceCache = new Dictionary<string, double>();

                // Group trips by driver and date
                var tripsByDriverAndDate = new Dictionary<string, List<TripVm>>();

                //var appointments = new List<Appointment>();
                //var hospitalsDict = new Dictionary<string, Hospital>(StringComparer.OrdinalIgnoreCase);
                //var patientsDict = new Dictionary<string, Patient>();

                //var tripsByDriver = new Dictionary<string, List<(Appointment, (double, double), (double, double))>>();

                // ================= STEP 1: CREATE DATA =================
                foreach (var trip in trips)
                {
                    try
                    {
                        // Get or create hospital
                        var hospital = await _context.Hospitals
                            .FirstOrDefaultAsync(h => h.Name == trip.HospitalName)
                            ?? new Hospital
                            {
                                Name = Trim50(trip.HospitalName),
                                Address = Trim200(trip.HospitalAddress),
                                Phone = Trim30(trip.HospitalPhone),
                                IsActive = true
                            };

                        if (hospital.Id == 0)
                        {
                            _context.Hospitals.Add(hospital);
                            await _context.SaveChangesAsync();
                        }

                        // Get or create patient
                        var patient = await _context.Patients.FirstOrDefaultAsync(p =>
                            p.FirstName == trip.FirstName &&
                            p.LastName == trip.LastName &&
                            p.PhoneNumber == trip.PatientPhone);

                        if (patient == null)
                        {
                            patient = new Patient
                            {
                                FirstName = Trim50(trip.FirstName),
                                LastName = Trim50(trip.LastName),
                                PhoneNumber = Trim30(trip.PatientPhone),
                                Address = Trim200(trip.PickupAddress),
                                Hospital = hospital,
                                VisitTime = trip.DropTime,
                                IsActive = true
                            };

                            _context.Patients.Add(patient);
                            await _context.SaveChangesAsync();
                        }

                        // Geocode addresses
                        var pickupCoord = await GetCoordinatesCached(trip.PickupAddress, geoCache);
                        var hospitalCoord = await GetCoordinatesCached(trip.HospitalAddress, geoCache);

                        // Find best driver
                        var driver = await FindBestDriverSmart(drivers, trip, pickupCoord, hospitalCoord, distanceCache);

                        if (driver != null)
                        {
                            string key = $"{driver.Id}_{trip.PickupTime:yyyyMMdd}";
                            if (!tripsByDriverAndDate.ContainsKey(key))
                                tripsByDriverAndDate[key] = new List<TripVm>();

                            tripsByDriverAndDate[key].Add(trip);
                        }

                        // Create appointment with all data
                        var appointment = new Appointment
                        {
                            PatientId = patient.Id,
                            HospitalId = hospital.Id,
                            DriverId = driver?.Id,
                            PickupTime = trip.PickupTime.Value,
                            AppointmentTime = trip.DropTime,
                            DropOffTime = trip.DropTime,
                            PickupAddress = Trim200(trip.PickupAddress),
                            PickupLatitude = pickupCoord.lat,
                            PickupLongitude = pickupCoord.lng,
                            DropOffAddress = Trim200(trip.HospitalAddress),
                            DropOffLatitude = hospitalCoord.lat,
                            DropOffLongitude = hospitalCoord.lng,
                            Status = "Scheduled",
                            LOS = trip.LOS,
                            CPay = trip.CPay,
                            PCA = trip.PCA,
                            AESC = trip.AESC,
                            CESC = trip.CESC,
                            Seats = trip.Seats,
                            Miles = trip.Miles,
                            Notes = trip.Notes,
                            IsActive = true
                        };

                        _context.Appointments.Add(appointment);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Trip skipped: {trip.FirstName} {trip.LastName}");
                    }
                }

                // Optimize routes and update sequence orders
                foreach (var group in tripsByDriverAndDate)
                {
                    try
                    {
                        int driverId = int.Parse(group.Key.Split('_')[0]);
                        var driver = drivers.First(d => d.Id == driverId);

                        var stops = group.Value;

                        // Get appointments for this driver and date
                        var driverAppointments = await _context.Appointments
                            .Where(a => a.DriverId == driverId && a.PickupTime.Date == group.Value.First().PickupTime.Value.Date)
                            .ToListAsync();

                        // Optimize route
                        var optimized = await OptimizeRouteUsingGoogle(driverAppointments, driver);

                        // Update sequence order
                        int seq = 1;
                        foreach (var appt in optimized)
                        {
                            appt.SequenceOrder = seq++;
                            driver.CurrentLat = appt.DropOffLatitude;
                            driver.CurrentLng = appt.DropOffLongitude;
                            driver.LastDropTime = appt.DropOffTime;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Route optimization failed");
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed");
                return StatusCode(500, $"Import failed: {ex.Message}");
            }
        }

        // ================= GOOGLE OPTIMIZATION =================
        private async Task<List<Appointment>> OptimizeRouteUsingGoogle(
    List<Appointment> appointments,
    Driver driver)
        {
            try
            {
                if (!appointments.Any()) return appointments;

                string origin = $"{driver.CurrentLat ?? 0},{driver.CurrentLng ?? 0}";
                string destination = $"{appointments.Last().PickupLatitude},{appointments.Last().PickupLongitude}";
                string waypoints = "optimize:true|" + string.Join("|", appointments.Select(a => $"{a.PickupLatitude},{a.PickupLongitude}"));

                var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={destination}&waypoints={waypoints}&key={_googleApiKey}";

                var response = await _httpClient.GetStringAsync(url);
                dynamic data = JsonConvert.DeserializeObject(response);

                if (data.status != "OK")
                {
                    _logger.LogWarning("Google optimization failed, returning original order");
                    return appointments;
                }

                var order = data.routes[0].waypoint_order;

                var optimized = new List<Appointment>();

                foreach (var i in order)
                {
                    var appt = appointments[(int)i];
                    optimized.Add(appt);
                }

                return optimized.Any() ? optimized : appointments;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google optimization failed, returning original order");
                return appointments;
            }
        }

        // ================= GEO =================
        private async Task<(double lat, double lng)> GetCoordinatesCached(string address,
            Dictionary<string, (double lat, double lng)> cache)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                    return (0, 0);

                if (cache.TryGetValue(address, out var val))
                    return val;

                var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_googleApiKey}";
                var json = await _httpClient.GetStringAsync(url);

                dynamic data = JsonConvert.DeserializeObject(json);

                if (data.status != "OK")
                    return (0, 0);

                double lat = (double)data.results[0].geometry.location.lat;
                double lng = (double)data.results[0].geometry.location.lng;

                cache[address] = (lat, lng);

                return (lat, lng);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geocoding failed");
                return (0, 0);
            }
        }

        // ================= DRIVER =================
        private async Task<Driver?> FindBestDriverSmart(
    List<Driver> drivers,
    TripVm trip,
    (double lat, double lng) pickup,
    (double lat, double lng) drop,
    Dictionary<string, double> cache)
        {
            double bestScore = double.MaxValue;
            Driver? best = null;

            foreach (var d in drivers)
            {
                if (trip.PickupTime == null) continue;

                var time = trip.PickupTime.Value.TimeOfDay;

                // shift filter
                if (time < d.ShiftStartTime || time > d.ShiftEndTime)
                    continue;

                // driver location
                var driverLoc = d.CurrentLat != null
                    ? (d.CurrentLat.Value, d.CurrentLng.Value)
                    : pickup;

                double distToPickup = await GetDistanceCached(driverLoc, pickup, cache);
                double tripDistance = await GetDistanceCached(pickup, drop, cache);

                double timePenalty = 0;

                if (d.LastDropTime != null)
                {
                    var gap = (trip.PickupTime.Value - d.LastDropTime.Value).TotalMinutes;

                    if (gap < 15)
                        timePenalty = 50; // too tight
                }

                double score = distToPickup + tripDistance + timePenalty;

                if (score < bestScore)
                {
                    bestScore = score;
                    best = d;
                }
            }

            return best;
        }

        // ================= DISTANCE CALCULATION =================
        private async Task<double> GetDistanceCached(
       (double lat, double lng) o,
       (double lat, double lng) d,
       Dictionary<string, double> cache)
        {
            try
            {
                // ✅ Better cache key (avoid float noise)
                string key = $"{Math.Round(o.lat, 5)},{Math.Round(o.lng, 5)}_" +
                             $"{Math.Round(d.lat, 5)},{Math.Round(d.lng, 5)}";

                if (cache.TryGetValue(key, out var val))
                    return val;

                var url = "https://routes.googleapis.com/distanceMatrix/v2:computeRouteMatrix";

                var body = new
                {
                    origins = new[]
                    {
                new
                {
                    waypoint = new
                    {
                        location = new
                        {
                            latLng = new
                            {
                                latitude = o.lat,
                                longitude = o.lng
                            }
                        }
                    }
                }
            },
                    destinations = new[]
                    {
                new
                {
                    waypoint = new
                    {
                        location = new
                        {
                            latLng = new
                            {
                                latitude = d.lat,
                                longitude = d.lng
                            }
                        }
                    }
                }
            },
                    travelMode = "DRIVE"
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);

                request.Headers.Add("X-Goog-Api-Key", _googleApiKey);

                // ✅ IMPORTANT: tells Google what to return (REQUIRED)
                request.Headers.Add("X-Goog-FieldMask", "distanceMeters");

                request.Content = new StringContent(
                    JsonConvert.SerializeObject(body),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"Routes API failed: {response.StatusCode}");
                    return 9999;
                }

                var json = await response.Content.ReadAsStringAsync();

                /*
                 Response is ARRAY:
                 [
                   {
                     "distanceMeters": 12345
                   }
                 ]
                */

                var data = JsonConvert.DeserializeObject<List<dynamic>>(json);

                if (data == null || data.Count == 0 || data[0].distanceMeters == null)
                    return 9999;

                double meters = (double)data[0].distanceMeters;

                double km = meters / 1000.0;

                cache[key] = km;

                return km;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Routes API failed");
                return 9999;
            }
        }

        
        // ================= UTIL =================
        private string Trim50(string? v) => string.IsNullOrEmpty(v) ? "" : v.Length > 50 ? v[..50] : v;
        private string Trim30(string? v) => string.IsNullOrEmpty(v) ? "" : v.Length > 30 ? v[..30] : v;
        private string Trim200(string? v) => string.IsNullOrEmpty(v) ? "" : v.Length > 200 ? v[..200] : v;

        private string ExtractPdfText(IFormFile file)
        {
            try
            {
                using var pdf = PdfDocument.Open(file.OpenReadStream());
                return string.Join("\n", pdf.GetPages().Select(p => p.Text));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF read failed");
                return "";
            }
        }

        private List<TripVm> ParseTripsV2(string text)
        {
            try
            {
                var trips = new List<TripVm>();

                // Split by ride blocks (----- separator)
                var blocks = Regex.Split(text, @"-{5,}");

                foreach (var block in blocks)
                {
                    if (!block.Contains("NEW")) continue;

                    var t = new TripVm();

                    // -----------------------------
                    // Ride ID
                    // -----------------------------
                    //var rideId = Regex.Match(block, @"(\d{2}-\d{4}-[A-Z])");
                    //if (rideId.Success)
                    //    t.RideId = rideId.Groups[1].Value.Trim();

                    // -----------------------------
                    // Patient Name (LAST, FIRST)
                    // -----------------------------
                    var name = Regex.Match(block, @"([A-Z]+),\s*([A-Z]+)");
                    if (name.Success)
                    {
                        t.LastName = name.Groups[1].Value.Trim();
                        t.FirstName = name.Groups[2].Value.Trim();
                    }

                    // -----------------------------
                    // Age
                    // -----------------------------
                    //var age = Regex.Match(block, @"Age:\s*(\d+)");
                    //if (age.Success)
                    //    t.PatientAge = int.Parse(age.Groups[1].Value);

                    // Parse phone
                    var phone = Regex.Match(block, @"\(\d{3}\)\s*\d{3}-\d{4}");
                    if (phone.Success) t.PatientPhone = phone.Value;

                    // Parse times
                    var times = Regex.Matches(block, @"\d{2}:\d{2}");
                    if (times.Count >= 2)
                    {
                        t.PickupTime = DateTime.Parse(times[0].Value);
                        t.DropTime = DateTime.Parse(times[1].Value);
                    }

                    // Parse addresses
                    t.PickupAddress = ExtractPickupAddress(block);
                    var dropAddress = ExtractDropOffAddress(block);

                    var addressParts = dropAddress.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                    if (addressParts.Length > 0)
                    {
                        t.HospitalName = addressParts[0].Trim();
                        if (addressParts.Length > 1)
                        {
                            t.HospitalAddress = string.Join(" ", addressParts.Skip(1)).Trim();
                        }
                    }

                    // -----------------------------
                    // Patient Phone
                    // -----------------------------
                    var patientPhone = Regex.Match(block, @"Phy:\s*\((\d{3})\)\s*(\d{3}-\d{4})");
                    if (patientPhone.Success)
                        t.PatientPhone = $"({patientPhone.Groups[1].Value}) {patientPhone.Groups[2].Value}";

                    // -----------------------------
                    // Pickup Time (PU)
                    // -----------------------------
                    var pickupMatch = Regex.Match(block, @"(\d{2}:\d{2})\s*PU");
                    if (pickupMatch.Success)
                        t.PickupTime = DateTime.Parse(pickupMatch.Groups[1].Value);

                    // -----------------------------
                    // Drop Time (DO)
                    // -----------------------------
                    var dropMatch = Regex.Match(block, @"(\d{2}:\d{2})\s*DO");
                    if (dropMatch.Success)
                        t.DropTime = DateTime.Parse(dropMatch.Groups[1].Value);

                    // -----------------------------
                    // Pickup Address (everything between "PU" and the next phone number or "DO")
                    // -----------------------------
                    //var pickupAddress = Regex.Match(block, @"PU\s+(.*?)\s+Phy:");
                    //if (pickupAddress.Success)
                    //    t.PickupAddress = pickupAddress.Groups[1].Value.Trim();
                    //else
                    //{
                    //    // Alternative pattern if phone number is on next line
                    //    pickupAddress = Regex.Match(block, @"PU\s+(.*?)\s+\d{4}[\s-]?\d{4}");
                    //    if (pickupAddress.Success)
                    //        t.PickupAddress = pickupAddress.Groups[1].Value.Trim();
                    //}

                    // -----------------------------
                    // Hospital Name
                    // -----------------------------
                    var hospitalName = Regex.Match(block, @"DO\s+(.*?)\s+Phy:");
                    if (hospitalName.Success)
                        t.HospitalName = hospitalName.Groups[1].Value.Trim();

                    // -----------------------------
                    // Hospital Phone
                    // -----------------------------
                    var hospitalPhoneMatch = Regex.Match(block, @"DO.*?Phy:\s*\((\d{3})\)\s*(\d{3}-\d{4})");
                    if (hospitalPhoneMatch.Success)
                        t.HospitalPhone = $"({hospitalPhoneMatch.Groups[1].Value}) {hospitalPhoneMatch.Groups[2].Value}";

                    // -----------------------------
                    // Hospital Address (between hospital phone and LOS)
                    // -----------------------------
                    //var hospitalAddress = Regex.Match(block, @"Phy:\s*\(?\d{3}\)?\s*\d{3}-\d{4}[\sx]*\s+(.*?)\s+LOS:");
                    //if (hospitalAddress.Success)
                    //    t.HospitalAddress = hospitalAddress.Groups[1].Value.Trim();

                    // -----------------------------
                    // LOS
                    // -----------------------------
                    //var los = Regex.Match(block, @"LOS:\s*([^C]+)");
                    //if (los.Success)
                    //    t.LOS = los.Groups[1].Value.Trim();
                    var losMatch = Regex.Match(block, @"LOS:\s*([A-Z\s]+)", RegexOptions.IgnoreCase);
                    if (losMatch.Success)
                        t.LOS = losMatch.Groups[1].Value.Trim();

                    // -----------------------------
                    // CPay
                    // -----------------------------
                    //var cpay = Regex.Match(block, @"CPay:\s*\$([\d\.]+)");
                    //if (cpay.Success)
                    //    t.CPay = decimal.Parse(cpay.Groups[1].Value);
                    var cpayMatch = Regex.Match(block, @"CPay:\s*\$?([\d.]+)");
                    if (cpayMatch.Success && decimal.TryParse(cpayMatch.Groups[1].Value, out var cpay))
                        t.CPay = cpay;

                    // -----------------------------
                    // PCA
                    // -----------------------------
                    //var pca = Regex.Match(block, @"PCA:\s*(\d)");
                    //if (pca.Success)
                    //    t.PCA = pca.Groups[1].Value == "1";
                    var pcaMatch = Regex.Match(block, @"PCA:\s*(\d+)");
                    if (pcaMatch.Success && int.TryParse(pcaMatch.Groups[1].Value, out var pca))
                        t.PCA = pca;

                    // -----------------------------
                    // AESC
                    // -----------------------------
                    //var aesc = Regex.Match(block, @"AEsc:\s*(\d)");
                    //if (aesc.Success)
                    //    t.AESC = aesc.Groups[1].Value == "1";
                    var aescMatch = Regex.Match(block, @"AEsc:\s*(\d+)");
                    if (aescMatch.Success && int.TryParse(aescMatch.Groups[1].Value, out var aesc))
                        t.AESC = aesc;

                    // -----------------------------
                    // CESC
                    // -----------------------------
                    //var cesc = Regex.Match(block, @"CEsc:\s*(\d)");
                    //if (cesc.Success)
                    //    t.CESC = cesc.Groups[1].Value == "1";
                    var cescMatch = Regex.Match(block, @"CEsc:\s*(\d+)");
                    if (cescMatch.Success && int.TryParse(cescMatch.Groups[1].Value, out var cesc))
                        t.CESC = cesc;

                    // -----------------------------
                    // Seats
                    // -----------------------------
                    //var seats = Regex.Match(block, @"Seats:\s*(\d+)");
                    //if (seats.Success)
                    //    t.Seats = int.Parse(seats.Groups[1].Value);
                    var seatsMatch = Regex.Match(block, @"Seats:\s*(\d+)");
                    if (seatsMatch.Success && int.TryParse(seatsMatch.Groups[1].Value, out var seats))
                        t.Seats = seats;

                    // -----------------------------
                    // Miles
                    // -----------------------------
                    //var miles = Regex.Match(block, @"Miles:\s*([\d\.]+)");
                    //if (miles.Success)
                    //    t.Miles = decimal.Parse(miles.Groups[1].Value);
                    var milesMatch = Regex.Match(block, @"Miles:\s*([\d.]+)");
                    if (milesMatch.Success && decimal.TryParse(milesMatch.Groups[1].Value, out var miles))
                        t.Miles = miles;

                    // -----------------------------
                    // Notes
                    // -----------------------------
                    //var notes = Regex.Match(block, @"Notes:\s*(.+)");
                    //if (notes.Success)
                    //    t.Notes = notes.Groups[1].Value.Trim();
                    var notesMatch = Regex.Match(block, @"Notes:\s*(.+)", RegexOptions.Singleline);
                    if (notesMatch.Success)
                        t.Notes = notesMatch.Groups[1].Value.Trim();


                    t.PickupAddress = ExtractPickupAddress(block);

                    // Extract hospital/drop-off address (after DO, without phone)
                    //var dropAddress = ExtractDropOffAddress(block);

                    // The first part before the full address is usually the hospital name
                    // addressParts = dropAddress.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                    //if (addressParts.Length > 0)
                    //{
                    //    t.HospitalName = addressParts[0].Trim();
                    //    if (addressParts.Length > 1)
                    //    {
                    //        t.HospitalAddress = string.Join(" ", addressParts.Skip(1)).Trim();
                    //    }
                    //}

                    trips.Add(t);
                }

                return trips;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parsing failed");
                return new List<TripVm>();
            }
        }

        /// <summary>
        /// Extracts the complete drop-off address after DO
        /// </summary>
        private string ExtractDropOffAddress(string text)
        {
            try
            {
                var doIndex = text.IndexOf("DO", StringComparison.OrdinalIgnoreCase);

                if (doIndex == -1)
                {
                    _logger.LogWarning("Could not find DO marker in text");
                    return string.Empty;
                }

                // Extract text after DO until we hit "LOS:" or end
                var losIndex = text.IndexOf("LOS:", doIndex, StringComparison.OrdinalIgnoreCase);
                var endIndex = losIndex != -1 ? losIndex : text.Length;

                var addressText = text.Substring(doIndex + 2, endIndex - doIndex - 2).Trim();

                // Split by newlines
                var lines = addressText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(l => l.Trim())
                                       .Where(l => !string.IsNullOrWhiteSpace(l))
                                       .ToList();

                var addressParts = new List<string>();

                foreach (var line in lines)
                {
                    var cleaned = line;

                    // Remove time pattern
                    cleaned = Regex.Replace(cleaned, @"^\d{2}:\d{2}\s*", string.Empty);

                    // Remove phone pattern
                    cleaned = Regex.Replace(cleaned, @"Phy:\s*\(\d{3}\)\s*\d{3}-\d{4}", string.Empty);

                    // Remove any remaining times
                    cleaned = Regex.Replace(cleaned, @"\d{2}:\d{2}", string.Empty);

                    // Clean whitespace
                    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        addressParts.Add(cleaned);
                    }
                }

                var result = string.Join(" ", addressParts);
                _logger.LogInformation($"Extracted drop-off address: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract drop-off address");
                return string.Empty;
            }
        }

        private string CleanAddressForGeocoding(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return string.Empty;

            var cleaned = address;
            cleaned = Regex.Replace(cleaned, @"\d{1,2}:\d{2}\s*(?:AM|PM)?", string.Empty, RegexOptions.IgnoreCase);
            cleaned = Regex.Replace(cleaned, @"\(\d{3}\)\s*\d{3}-\d{4}", string.Empty);
            cleaned = Regex.Replace(cleaned, @"\d{3}-\d{3}-\d{4}", string.Empty);
            cleaned = Regex.Replace(cleaned, @"Phy:\s*", string.Empty, RegexOptions.IgnoreCase);
            cleaned = cleaned.Replace("  ", " ").Trim();

            return cleaned;
        }

        /// <summary>
        /// Extracts the complete address between PU (pickup) and DO (drop-off),
        /// excluding the phone number pattern "Phy: (###) ###-####" and time patterns
        /// </summary>
        private string ExtractPickupAddress(string text)
        {
            try
            {
                // Find the text between "PU" and "DO"
                var puIndex = text.IndexOf("PU", StringComparison.OrdinalIgnoreCase);
                var doIndex = text.IndexOf("DO", StringComparison.OrdinalIgnoreCase);

                if (puIndex == -1 || doIndex == -1 || puIndex >= doIndex)
                {
                    _logger.LogWarning("Could not find PU or DO markers in text");
                    return string.Empty;
                }

                // Extract text between PU and DO (skip "PU" itself - 2 chars)
                var addressText = text.Substring(puIndex + 2, doIndex - puIndex - 2).Trim();

                // Split by newlines to get clean address lines
                var lines = addressText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(l => l.Trim())
                                       .Where(l => !string.IsNullOrWhiteSpace(l))
                                       .ToList();

                var addressParts = new List<string>();

                foreach (var line in lines)
                {
                    var cleaned = line;

                    // Remove time pattern at the beginning (e.g., "11:30" or "04:55")
                    cleaned = Regex.Replace(cleaned, @"^\d{2}:\d{2}\s*", string.Empty);

                    // Remove phone pattern
                    cleaned = Regex.Replace(cleaned, @"Phy:\s*\(\d{3}\)\s*\d{3}-\d{4}", string.Empty);

                    // Remove any remaining time patterns
                    cleaned = Regex.Replace(cleaned, @"\d{2}:\d{2}", string.Empty);

                    // Clean whitespace
                    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

                    if (!string.IsNullOrWhiteSpace(cleaned))
                    {
                        addressParts.Add(cleaned);
                    }
                }

                var result = string.Join(" ", addressParts);
                _logger.LogInformation($"Extracted pickup address: {result}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract pickup address");
                return string.Empty;
            }
        }

    }
}