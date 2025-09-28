using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SpendSharp.Data;
using SpendSharp.Models;

namespace SpendSharp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly SpendSharpDbContext _context;

        private readonly int[] _validPageSizes =
        {
            5,
            10,
            25,
            50
        };

        public HomeController(ILogger<HomeController> logger, SpendSharpDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Expenses(
            [FromQuery] int page,
            [FromQuery] int pageSize,
            [FromQuery][AllowedValues(new[] { "ASC", "DESC" }, "Invalid orderBy value provided")] string orderBy
        )
        {
            if (page == 0)
            {
                page = 1;
            }

            if (!_validPageSizes.Contains(pageSize))
            {
                pageSize = _validPageSizes[0];
            }

            var query = _context.Expenses.AsQueryable();

            if (orderBy == "DESC")
            {
                query = query.OrderByDescending(e => e.Id);
            }
            else
            {
                query = query.OrderBy(e => e.Id);
            }

            var allExpenses = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = _context.Expenses.Count();
            ViewBag.TotalPages = (int)Math.Ceiling((double)ViewBag.TotalCount / pageSize);
            ViewBag.PageSizes = _validPageSizes;

            return View(allExpenses);
        }

        public IActionResult CreateEditExpense(int? id)
        {
            if (id.HasValue)
            {
                var expense = _context.Expenses.Find(id.Value);
                if (expense != null)
                {
                    return View(expense);
                }
            }

            return View();
        }

        public IActionResult DeleteExpense(int id)
        {
            var expense = _context.Expenses.Find(id);
            if (expense != null)
            {
                _context.Expenses.Remove(expense);
                _context.SaveChangesAsync();
            }
            return RedirectToAction("Expenses");
        }

        public IActionResult CreateEditExpenseForm(Expense data)
        {
            if (data.Id > 0)
            {
                _context.Expenses.Update(data);
            }
            else
            {
                _context.Expenses.Add(data);
            }
            _context.SaveChangesAsync();
            return RedirectToAction("Expenses");
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
