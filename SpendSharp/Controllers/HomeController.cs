using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;
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
            ViewBag.TotalPages = (int) Math.Ceiling((double) ViewBag.TotalCount / pageSize);
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

        public IActionResult BulkCreate()
        {
            return View();
        }

        [HttpPost]
        public IActionResult BulkCreateForm(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Message = "Invalid file provided.";
                return View();
            }

            const int FILE_SIZE_LENGTH = 5 * 1024 * 1024;

            if (file.Length > FILE_SIZE_LENGTH)
            {
                ViewBag.Message = $"Provided file is too large. Limit is {FILE_SIZE_LENGTH / 1024 / 1024} MB";
                return View();
            }

            string[] fileNameParts = file.FileName.Split(".");
            string[] validFileExtensions = { "xlsx", "xls" };

            if (fileNameParts.Length < 1 || !validFileExtensions.Contains(fileNameParts[fileNameParts.Length - 1].ToLower()))
            {
                ViewBag.Message = "Provided file has an invalid extension, and must be XLS or XLSX file.";
                return View();
            }

            var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            if (!Directory.Exists(uploadPath))
                Directory.CreateDirectory(uploadPath);

            var filePath = Path.Combine(uploadPath, Path.GetFileName(file.FileName));

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                file.CopyTo(stream);
            }

            string[] columns = { "Description", "Value" };

            int failedRows = 0, successfulRows = 0;
            using (var workbook = new ClosedXML.Excel.XLWorkbook(filePath))
            {
                var worksheet = workbook.Worksheets.First();

                var firstRowUsed = worksheet.FirstRowUsed();
                if (firstRowUsed == null)
                {
                    ViewBag.Message = "Provided file is empty or invalid.";
                    return View("BulkCreate");
                }

                var columnNames = firstRowUsed.Cells().Select(c => c.GetString()).ToArray();
                if (!columns.All(c => columnNames.Contains(c)))
                {
                    ViewBag.Message = "Provided file is missing one or more required columns: Description, Value";
                    return View("BulkCreate");
                }

                // Skip header row
                var rows = worksheet.RowsUsed().Skip(1);

                // Indexes
                int descriptionIndex = columnNames.ToList().IndexOf("Description") + 1;
                int valueIndex = columnNames.ToList().IndexOf("Value") + 1;

                // Loop through all the rows now
                foreach (var row in rows)
                {
                    try
                    {
                        var description = row.Cell(descriptionIndex).GetString();
                        var value = row.Cell(valueIndex).GetDouble();

                        if (string.IsNullOrWhiteSpace(description) || value == 0)
                        {
                            continue;
                        }

                        var expense = new Expense
                        {
                            Description = description,
                            Value = (decimal) value,
                            createdAt = DateTime.Now
                        };
                        _context.Expenses.Add(expense);
                        ++successfulRows;
                    }
                    catch (Exception ex)
                    {
                        ++failedRows;
                        _logger.LogError(ex, "Error processing row in bulk upload.");
                        continue;
                    }
                }

                // Save all changes outside of the loop
                _context.SaveChangesAsync();
            }

            if (failedRows > 0 && successfulRows == 0)
            {
                ViewBag.Message = $"File uploaded successfully, but failed to process rows. Please verify if the template provided is correct.";
            }
            else
            {
                ViewBag.Message = $"File uploaded successfully. Rows processed: {successfulRows} successfully and {failedRows} failed.";
            }
            return View("BulkCreate");
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

        [HttpPost]
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
