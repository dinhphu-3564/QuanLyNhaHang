using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_QLNhaHang.Data;
using Web_QLNhaHang.Models;

namespace Web_QLNhaHang.Controllers
{
    [Route("dat-ban")]
    [Route("Booking")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var areas = await _context.TableAreas
                .Where(a => a.IsActive)
                .Include(a => a.Tables!.Where(t => t.IsActive && t.Status == "Trống"))
                .ToListAsync();

            // Create SelectList for dropdown
            var tableList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            foreach (var area in areas)
            {
                var group = new Microsoft.AspNetCore.Mvc.Rendering.SelectListGroup { Name = area.AreaName };
                foreach (var table in area.Tables!)
                {
                    tableList.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = table.TableId.ToString(),
                        Text = $"{table.TableNumber} ({table.Capacity} ghế)",
                        Group = group
                    });
                }
            }
            ViewBag.TableId = tableList;

            return View(new Booking());
        }

        [HttpPost("Create")]
        [HttpPost("")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Booking booking)
        {
            if (ModelState.IsValid)
            {
                // Kiểm tra bàn có trống không
                if (booking.TableId.HasValue)
                {
                    var table = await _context.Tables.FindAsync(booking.TableId.Value);
                    if (table != null && table.Status != "Trống")
                    {
                        ModelState.AddModelError("", "Bàn đã được đặt hoặc đang sử dụng.");
                        var areas = await _context.TableAreas
                            .Where(a => a.IsActive)
                            .Include(a => a.Tables!.Where(t => t.IsActive))
                            .ToListAsync();

                        var tableListError = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
                        foreach (var area in areas)
                        {
                            var group = new Microsoft.AspNetCore.Mvc.Rendering.SelectListGroup { Name = area.AreaName };
                            foreach (var tbl in area.Tables!)
                            {
                                tableListError.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                                {
                                    Value = tbl.TableId.ToString(),
                                    Text = $"{tbl.TableNumber} ({tbl.Capacity} ghế)",
                                    Group = group
                                });
                            }
                        }
                        ViewBag.TableId = tableListError;
                        
                        return View("Index", booking);
                    }
                }

                booking.CreatedDate = DateTime.Now;
                booking.Status = "Chờ";

                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đặt bàn thành công! Chúng tôi sẽ liên hệ với bạn để xác nhận.";
                return RedirectToAction("Index", "Home");
            }

            // Reload areas for dropdown if validation fails
            var areas2 = await _context.TableAreas
                .Where(a => a.IsActive)
                .Include(a => a.Tables!.Where(t => t.IsActive))
                .ToListAsync();

            var tableList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();
            foreach (var area in areas2)
            {
                var group = new Microsoft.AspNetCore.Mvc.Rendering.SelectListGroup { Name = area.AreaName };
                foreach (var table in area.Tables!)
                {
                    tableList.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = table.TableId.ToString(),
                        Text = $"{table.TableNumber} ({table.Capacity} ghế)",
                        Group = group
                    });
                }
            }
            ViewBag.TableId = tableList;

            return View("Index", booking);
        }

        public async Task<IActionResult> CheckStatus(string phone)
        {
            if (string.IsNullOrEmpty(phone))
            {
                return View("CheckStatus", new List<Booking>());
            }

            var bookings = await _context.Bookings
                .Include(b => b.Table)
                .Where(b => b.CustomerPhone == phone)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View("CheckStatus", bookings);
        }
    }
}

