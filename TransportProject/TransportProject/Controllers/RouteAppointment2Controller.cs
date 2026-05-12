using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using TransportProject.DatabaseContext;
using TransportProject.Models;
using TransportProject.Repositories.Interface;
using TransportProject.ViewModels;
using UglyToad.PdfPig;
using Route = TransportProject.Models.Route;
using Newtonsoft.Json;

namespace TransportProject.Controllers
{
    [Authorize(Roles = "Admin,Dispatcher")]
    public class RouteAppointment2Controller : Controller
    {
        private readonly IRouteAppointmentRepository _repository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RouteAppointment2Controller> _logger;
        private readonly HttpClient _httpClient;

        public RouteAppointment2Controller(
            IRouteAppointmentRepository repository,
            ApplicationDbContext context,
            ILogger<RouteAppointment2Controller> logger,
            IHttpClientFactory httpClientFactory)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        // ================= INDEX =================
        public async Task<IActionResult> Index(int page = 1, int pageSize = 100,
            string searchDriver = "", string searchPatient = "",
            DateTime? date = null, bool selectAll = false)
        {
            try
            {
                var query = _repository.Query();

                if (!string.IsNullOrEmpty(searchDriver))
                    query = query.Where(x => x.DriverName.Contains(searchDriver));

                if (!string.IsNullOrEmpty(searchPatient))
                    query = query.Where(x => x.PatientName.Contains(searchPatient));

                if (date.HasValue)
                    query = query.Where(x => x.PickupTime.Date == date.Value.Date);

                var totalRecords = await query.CountAsync();

                if (!selectAll)
                {
                    query = query.OrderBy(x => x.PickupTime)
                                 .Skip((page - 1) * pageSize)
                                 .Take(pageSize);
                }

                return View(new RouteAppointmentListVM
                {
                    Items = await query.ToListAsync(),
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalRecords = totalRecords
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Index failed");
                return StatusCode(500, "Error loading data");
            }
        }

        [HttpPost]
        // ================= IMPORT =================
        public IActionResult ImportFile() => View();

        [HttpPost]
        public async Task<IActionResult> ImportFile(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded.");

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

                var tripsByDriver = new Dictionary<string,
                    List<(Appointment appt, (double lat, double lng) pickup, (double lat, double lng) drop)>>();

                foreach (var trip in trips)
                {
                    try
                    {
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
                            _context.Hospitals.Add(hospital);

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

                        var pickupCoord = await GetCoordinatesCached(trip.PickupAddress, geoCache);
                        var hospitalCoord = await GetCoordinatesCached(trip.HospitalAddress, geoCache);

                        var appointment = new Appointment
                        {
                            PatientId = patient.Id,
                            HospitalId = hospital.Id,
                            PickupTime = trip.PickupTime.Value,
                            AppointmentTime = trip.DropTime,
                            PickupAddress = Trim200(trip.PickupAddress),
                            PickupLatitude = pickupCoord.lat,
                            PickupLongitude = pickupCoord.lng,
                            Status = "Scheduled",
                            IsActive = true
                        };

                        _context.Appointments.Add(appointment);
                        await _context.SaveChangesAsync();

                        var driver = await FindBestDriverSmart(drivers, trip, pickupCoord, hospitalCoord, distanceCache);
                        if (driver == null) continue;

                        string key = $"{driver.Id}_{trip.PickupTime:yyyyMMdd}";

                        if (!tripsByDriver.ContainsKey(key))
                            tripsByDriver[key] = new();

                        tripsByDriver[key].Add((appointment, pickupCoord, hospitalCoord));
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Trip skipped: {trip.FirstName} {trip.LastName}");
                    }
                }

                foreach (var group in tripsByDriver)
                {
                    try
                    {
                        int driverId = int.Parse(group.Key.Split('_')[0]);
                        var driver = drivers.First(d => d.Id == driverId);

                        var optimizedStops = await OptimizeRouteUsingOSRM(
                            group.Value,
                            (driver.CurrentLat ?? 0, driver.CurrentLng ?? 0));

                        var route = new Route
                        {
                            DriverId = driverId,
                            RouteDate = DateTime.Now.Date,
                            Status = "Active",
                            IsActive = true
                        };

                        _context.Routes.Add(route);
                        await _context.SaveChangesAsync();

                        int seq = 1;

                        foreach (var stop in optimizedStops)
                        {
                            _context.RouteAppointments.Add(new RouteAppointment
                            {
                                RouteId = route.Id,
                                AppointmentId = stop.appt.Id,
                                SequenceOrder = seq++,
                                IsActive = true
                            });

                            driver.CurrentLat = stop.drop.lat;
                            driver.CurrentLng = stop.drop.lng;
                            driver.LastDropTime = stop.appt.AppointmentTime;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Route creation failed");
                    }
                }

                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Import failed");
                return StatusCode(500, "Import failed");
            }
        }

        //// ================= GOOGLE OPTIMIZATION =================
        //private async Task<List<(Appointment appt, (double lat, double lng) pickup, (double lat, double lng) drop)>>
        //OptimizeRouteUsingGoogle(
        //    List<(Appointment appt, (double lat, double lng) pickup, (double lat, double lng) drop)> stops,
        //    (double lat, double lng) start)
        //{
        //    try
        //    {
        //        if (!stops.Any()) return stops;

        //        string origin = $"{start.lat},{start.lng}";
        //        string destination = $"{stops.Last().drop.lat},{stops.Last().drop.lng}";
        //        string waypoints = "optimize:true|" + string.Join("|", stops.Select(s => $"{s.pickup.lat},{s.pickup.lng}"));

        //        var url = $"https://maps.googleapis.com/maps/api/directions/json?origin={origin}&destination={destination}&waypoints={waypoints}&key={_googleApiKey}";
        //        var response = await _httpClient.GetStringAsync(url);

        //        dynamic data = JsonConvert.DeserializeObject(response);

        //        if (data.status != "OK")
        //            return stops;

        //        var order = data.routes[0].waypoint_order;

        //        var optimized = new List<(Appointment appt, (double lat, double lng) pickup, (double lat, double lng) drop)>();

        //        foreach (var i in order)
        //        {
        //            var s = stops[(int)i];
        //            optimized.Add((s.appt, s.pickup, s.drop));
        //        }

        //        return optimized;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Google optimization failed");
        //        return stops;
        //    }
        //}

        //// ================= GEO =================
        //private async Task<(double lat, double lng)> GetCoordinatesCached(string address,
        //    Dictionary<string, (double lat, double lng)> cache)
        //{
        //    try
        //    {
        //        if (string.IsNullOrWhiteSpace(address))
        //            return (0, 0);

        //        if (cache.TryGetValue(address, out var val))
        //            return val;

        //        var url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(address)}&key={_googleApiKey}";
        //        var json = await _httpClient.GetStringAsync(url);

        //        dynamic data = JsonConvert.DeserializeObject(json);

        //        if (data.status != "OK")
        //            return (0, 0);

        //        double lat = (double)data.results[0].geometry.location.lat;
        //        double lng = (double)data.results[0].geometry.location.lng;

        //        cache[address] = (lat, lng);

        //        return (lat, lng);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Geocoding failed");
        //        return (0, 0);
        //    }
        //}

        // ================= OSRM ROUTE OPTIMIZATION (FREE) =================
        private async Task<List<(Appointment appt, (double lat, double lng) pickup, (double lat, double lng) drop)>>
        OptimizeRouteUsingOSRM(
            List<(Appointment appt, (double lat, double lng) pickup, (double lat, double lng) drop)> stops,
            (double lat, double lng) start)
        {
            try
            {
                if (!stops.Any()) return stops;

                // OSRM trip endpoint for route optimization
                // Format: coordinates separated by semicolon (lng,lat)
                var coordinates = new List<string>
                {
                    $"{start.lng},{start.lat}" // Start point
                };

                // Add all pickup locations
                coordinates.AddRange(stops.Select(s => $"{s.pickup.lng},{s.pickup.lat}"));

                var coordString = string.Join(";", coordinates);
                var url = $"http://router.project-osrm.org/trip/v1/driving/{coordString}?source=first&roundtrip=false";

                var response = await _httpClient.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<OsrmTripResponse>(response);

                if (data?.Code != "Ok" || data.Trips == null || !data.Trips.Any())
                {
                    _logger.LogWarning("OSRM trip optimization failed, returning original order");
                    return stops;
                }

                // Get the waypoint order from the optimized trip
                var waypointIndices = data.Waypoints
                    .OrderBy(w => w.WaypointIndex)
                    .Select(w => w.TripsIndex)
                    .Skip(1) // Skip the start point
                    .ToList();

                var optimized = new List<(Appointment appt, (double lat, double lng) pickup, (double lat, double lng) drop)>();

                foreach (var index in waypointIndices)
                {
                    if (index > 0 && index <= stops.Count)
                    {
                        optimized.Add(stops[index - 1]);
                    }
                }

                return optimized.Any() ? optimized : stops;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OSRM optimization failed, returning original order");
                return stops;
            }
        }


        // ================= OSRM DISTANCE CALCULATION (FREE) =================
        private async Task<double> GetDistanceCached(
            (double lat, double lng) o,
            (double lat, double lng) d,
            Dictionary<string, double> cache)
        {
            try
            {
                string key = $"{o.lat},{o.lng}_{d.lat},{d.lng}";

                if (cache.TryGetValue(key, out var val))
                    return val;

                // OSRM (Open Source Routing Machine) - Free routing API
                // Note: OSRM uses lng,lat format (not lat,lng)
                var url = $"http://router.project-osrm.org/route/v1/driving/{o.lng},{o.lat};{d.lng},{d.lat}?overview=false";

                var json = await _httpClient.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<OsrmRouteResponse>(json);

                if (data?.Code != "Ok" || data.Routes == null || !data.Routes.Any())
                {
                    _logger.LogWarning("OSRM distance calculation failed");
                    return 9999;
                }

                // Distance in kilometers
                double km = data.Routes[0].Distance / 1000.0;

                cache[key] = km;

                return km;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Distance API failed");
                return 9999;
            }
        }

        // ================= DRIVER SELECTION =================
        private async Task<Driver?> FindBestDriverSmart(
            List<Driver> drivers,
            TripVm trip,
            (double lat, double lng) pickup,
            (double lat, double lng) drop,
            Dictionary<string, double> cache)
        {
            try
            {
                double bestScore = double.MaxValue;
                Driver? best = null;

                foreach (var d in drivers)
                {
                    if (trip.PickupTime == null) continue;

                    var time = trip.PickupTime.Value.TimeOfDay;
                    if (time < d.ShiftStartTime || time > d.ShiftEndTime)
                        continue;

                    var driverLoc = d.CurrentLat != null
                        ? (d.CurrentLat.Value, d.CurrentLng.Value)
                        : pickup;

                    double d1 = await GetDistanceCached(driverLoc, pickup, cache);
                    double d2 = await GetDistanceCached(pickup, drop, cache);

                    double score = d1 + d2;

                    if (d.LastDropTime > trip.PickupTime)
                        score += 50;

                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = d;
                    }
                }

                return best;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Driver selection failed");
                return null;
            }
        }


        //private async Task<double> GetDistanceCached(
        //    (double lat, double lng) o,
        //    (double lat, double lng) d,
        //    Dictionary<string, double> cache)
        //{
        //    try
        //    {
        //        string key = $"{o.lat},{o.lng}_{d.lat},{d.lng}";

        //        if (cache.TryGetValue(key, out var val))
        //            return val;

        //        var url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={o.lat},{o.lng}&destinations={d.lat},{d.lng}&key={_googleApiKey}";
        //        var json = await _httpClient.GetStringAsync(url);

        //        dynamic data = JsonConvert.DeserializeObject(json);

        //        double km = (double)data.rows[0].elements[0].distance.value / 1000.0;

        //        cache[key] = km;

        //        return km;
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Distance API failed");
        //        return 9999; // fallback large distance
        //    }
        //}

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

        /// <summary>
        /// Extracts the complete address between PU (pickup) and DO (drop-off),
        /// excluding the phone number pattern "Phy: (###) ###-####"
        /// </summary>
        /// <param name="text">The text block containing PU and DO information</param>
        /// <returns>The cleaned address string without phone number</returns>
        private string ExtractPickupAddress(string text)
        {
            try
            {
                // Find the text between "PU" and "DO"
                var puIndex = text.IndexOf("PU");
                var doIndex = text.IndexOf("DO");

                if (puIndex == -1 || doIndex == -1 || puIndex >= doIndex)
                {
                    _logger.LogWarning("Could not find PU or DO markers in text");
                    return string.Empty;
                }

                // Extract text between PU and DO
                var addressText = text.Substring(puIndex + 2, doIndex - puIndex - 2).Trim();

                // Remove the phone number pattern: "Phy:" followed by phone number
                // Pattern matches: Phy:                     (###) ###-####
                var phonePattern = @"Phy:\s*\(\d{3}\)\s*\d{3}-\d{4}";
                addressText = Regex.Replace(addressText, phonePattern, string.Empty);

                // Clean up extra whitespace
                addressText = Regex.Replace(addressText, @"\s+", " ").Trim();

                return addressText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract pickup address");
                return string.Empty;
            }
        }

      
        // ================= NOMINATIM GEOCODING (FREE) =================
        private async Task<(double lat, double lng)> GetCoordinatesCached(string address,
            Dictionary<string, (double lat, double lng)> cache)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(address))
                    return (0, 0);

                if (cache.TryGetValue(address, out var val))
                    return val;

                // Rate limit: Nominatim allows 1 request per second
                await Task.Delay(1100);

                // Nominatim (OpenStreetMap) - Free geocoding API
                var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(address)}&format=json&limit=1";

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "TransportProject/1.0 (Medical Transport Routing)");

                var json = await _httpClient.GetStringAsync(url);
                var data = JsonConvert.DeserializeObject<List<NominatimResult>>(json);

                if (data == null || !data.Any())
                {
                    _logger.LogWarning($"No geocoding results for: {address}");
                    return (0, 0);
                }

                double lat = double.Parse(data[0].Lat);
                double lng = double.Parse(data[0].Lon);

                cache[address] = (lat, lng);

                _logger.LogInformation($"Geocoded: {address} -> ({lat}, {lng})");

                return (lat, lng);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Geocoding failed for: {address}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Extracts the complete drop-off address after DO,
        /// excluding the phone number pattern "Phy: (###) ###-####"
        /// </summary>
        /// <param name="text">The text block containing DO information</param>
        /// <returns>The cleaned address string without phone number</returns>
        private string ExtractDropOffAddress(string text)
        {
            try
            {
                // Find the text after "DO" until the next line break or specific marker
                var doIndex = text.IndexOf("DO");

                if (doIndex == -1)
                {
                    _logger.LogWarning("Could not find DO marker in text");
                    return string.Empty;
                }

                // Extract text after DO until we hit "LOS:" or end of meaningful data
                var losIndex = text.IndexOf("LOS:", doIndex);
                var endIndex = losIndex != -1 ? losIndex : text.Length;

                var addressText = text.Substring(doIndex + 2, endIndex - doIndex - 2).Trim();

                // Remove the phone number pattern: "Phy:" followed by phone number
                var phonePattern = @"Phy:\s*\(\d{3}\)\s*\d{3}-\d{4}";
                addressText = Regex.Replace(addressText, phonePattern, string.Empty);

                // Clean up extra whitespace
                addressText = Regex.Replace(addressText, @"\s+", " ").Trim();

                return addressText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to extract drop-off address");
                return string.Empty;
            }
        }
        private List<TripVm> ParseTripsV2(string text)
        {
            try
            {
                var trips = new List<TripVm>();
                var blocks = Regex.Split(text, @"-{5,}");

                foreach (var block in blocks)
                {
                    if (!block.Contains("NEW")) continue;

                    var t = new TripVm();

                    var name = Regex.Match(block, @"([A-Z]+),\s*([A-Z]+)");
                    if (name.Success)
                    {
                        t.LastName = name.Groups[1].Value;
                        t.FirstName = name.Groups[2].Value;
                    }

                    var phone = Regex.Match(block, @"\(\d{3}\)\s*\d{3}-\d{4}");
                    if (phone.Success) t.PatientPhone = phone.Value;

                    var times = Regex.Matches(block, @"\d{2}:\d{2}");
                    if (times.Count >= 2)
                    {
                        t.PickupTime = DateTime.Parse(times[0].Value);
                        t.DropTime = DateTime.Parse(times[1].Value);
                    }

                    // Extract pickup address (between PU and DO, without phone)
                    t.PickupAddress = ExtractPickupAddress(block);

                    // Extract hospital/drop-off address (after DO, without phone)
                    var dropAddress = ExtractDropOffAddress(block);

                    // The first part before the full address is usually the hospital name
                    var addressParts = dropAddress.Split(new[] { "  " }, StringSplitOptions.RemoveEmptyEntries);
                    if (addressParts.Length > 0)
                    {
                        t.HospitalName = addressParts[0].Trim();
                        if (addressParts.Length > 1)
                        {
                            t.HospitalAddress = string.Join(" ", addressParts.Skip(1)).Trim();
                        }
                    }

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


        // ================= API RESPONSE MODELS =================

        // Nominatim (Geocoding) Models
        public class NominatimResult
        {
            [JsonProperty("lat")]
            public string Lat { get; set; }

            [JsonProperty("lon")]
            public string Lon { get; set; }

            [JsonProperty("display_name")]
            public string DisplayName { get; set; }
        }

        // OSRM Route Models
        public class OsrmRouteResponse
        {
            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("routes")]
            public List<OsrmRoute> Routes { get; set; }
        }

        public class OsrmRoute
        {
            [JsonProperty("distance")]
            public double Distance { get; set; } // in meters

            [JsonProperty("duration")]
            public double Duration { get; set; } // in seconds
        }

        // OSRM Trip Optimization Models
        public class OsrmTripResponse
        {
            [JsonProperty("code")]
            public string Code { get; set; }

            [JsonProperty("trips")]
            public List<OsrmTrip> Trips { get; set; }

            [JsonProperty("waypoints")]
            public List<OsrmWaypoint> Waypoints { get; set; }
        }

        public class OsrmTrip
        {
            [JsonProperty("distance")]
            public double Distance { get; set; }

            [JsonProperty("duration")]
            public double Duration { get; set; }
        }

        public class OsrmWaypoint
        {
            [JsonProperty("waypoint_index")]
            public int WaypointIndex { get; set; }

            [JsonProperty("trips_index")]
            public int TripsIndex { get; set; }

            [JsonProperty("location")]
            public List<double> Location { get; set; }
        }
    }
}