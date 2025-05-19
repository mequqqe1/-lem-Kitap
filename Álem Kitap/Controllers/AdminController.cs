using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BookStore.Data;

namespace BookStore.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        private bool IsAdmin()
        {
            var isAdmin = User.FindFirst("IsAdmin")?.Value;
            return isAdmin == "True";
        }
        public class UpdateOrderStatusRequest
        {
            public int PurchaseId { get; set; }
            public OrderStatus NewStatus { get; set; }
        }

        public class PhysicalBookRequest
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public string Description { get; set; }
            public decimal Price { get; set; }
            public int Stock { get; set; }
            public IFormFile Cover { get; set; }
        }

        [HttpGet("all-physical-orders")]
        public async Task<IActionResult> GetAllPhysicalOrders()
        {
            if (!IsAdmin())
                return Forbid();

            var orders = await _context.PhysicalPurchases
                .Include(p => p.PhysicalBook)
                .Include(p => p.User)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            return Ok(orders);
        }
        [HttpPut("update-physical-order-status")]
        public async Task<IActionResult> UpdatePhysicalOrderStatus([FromBody] UpdateOrderStatusRequest request)
        {
            if (!IsAdmin())
                return Forbid("У вас нет доступа.");

            var purchase = await _context.PhysicalPurchases
                .Include(p => p.PhysicalBook)
                .FirstOrDefaultAsync(p => p.Id == request.PurchaseId);

            if (purchase == null)
                return NotFound("Заказ не найден.");

            purchase.Status = request.NewStatus;
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Статус обновлён",
                OrderId = purchase.Id,
                NewStatus = purchase.Status.ToString()
            });
        }

        [HttpPost("add-physical-book")]
        public async Task<IActionResult> AddPhysicalBook([FromForm] PhysicalBookRequest request)
        {
            if (!IsAdmin()) return Forbid();

            if (request.Cover == null) 
                return BadRequest("Нужна обложка.");

            var coverName = $"{Guid.NewGuid()}_{request.Cover.FileName}";
            var coverPath = Path.Combine("wwwroot/covers", coverName);

            using var stream = new FileStream(coverPath, FileMode.Create);
            await request.Cover.CopyToAsync(stream);

            var book = new PhysicalBook
            {
                Title = request.Title,
                Author = request.Author,
                Description = request.Description,
                Price = request.Price,
                Stock = request.Stock,
                CoverPath = $"covers/{coverName}"
            };

            _context.PhysicalBooks.Add(book);
            await _context.SaveChangesAsync();

            return Ok(book);
        }
        [HttpGet("physical-books")]
        public IActionResult GetAllPhysicalBooks()
        {
            if (!IsAdmin()) return Forbid();

            return Ok(_context.PhysicalBooks);
        }

        [HttpPost("add-book")]
        public async Task<IActionResult> AddBook([FromForm] BookUploadRequest request)
        {
            if (!IsAdmin())
                return Forbid("У вас нет прав для выполнения этой операции.");

            // Проверяем, есть ли файл книги
            if (request.File == null || request.File.Length == 0)
                return BadRequest("Файл книги не загружен.");

            // Проверяем, есть ли обложка
            if (request.Cover == null || request.Cover.Length == 0)
                return BadRequest("Обложка книги не загружена.");

            // Генерация уникального имени для файла книги
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.File.FileName)}";
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "books", fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await request.File.CopyToAsync(stream);
            }

            // Генерация уникального имени для обложки
            var coverName = $"{Guid.NewGuid()}_{Path.GetFileName(request.Cover.FileName)}";
            var coverPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "covers", coverName);

            using (var stream = new FileStream(coverPath, FileMode.Create))
            {
                await request.Cover.CopyToAsync(stream);
            }

            // Создание новой книги
            var book = new Book
            {
                Title = request.Title,
                Author = request.Author,
                Description = request.Description,
                Price = request.Price,
                FilePath = $"books/{fileName}",
                CoverPath = $"covers/{coverName}"
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return Ok("Книга успешно добавлена.");
        }


        [HttpGet("all-books")]
        public async Task<IActionResult> GetAllBooks()
        {
            if (!IsAdmin())
                return Forbid("У вас нет прав для выполнения этой операции.");

            var books = await _context.Books.ToListAsync();
            return Ok(books);
        }

        [HttpPut("edit-book/{id}")]
        public async Task<IActionResult> EditBook(int id, [FromForm] BookUploadRequest request)
        {
            if (!IsAdmin())
                return Forbid("У вас нет прав для выполнения этой операции.");

            var book = await _context.Books.FindAsync(id);
            if (book == null)
                return NotFound("Книга не найдена.");

            book.Title = request.Title;
            book.Author = request.Author;
            book.Description = request.Description;
            book.Price = request.Price;

            // Проверяем, есть ли новый файл книги
            if (request.File != null && request.File.Length > 0)
            {
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(request.File.FileName)}";
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "books", fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.File.CopyToAsync(stream);
                }

                // Удаляем старый файл книги
                var oldFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.FilePath);
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }

                book.FilePath = $"books/{fileName}";
            }

            // Проверяем, есть ли новая обложка
            if (request.Cover != null && request.Cover.Length > 0)
            {
                var coverName = $"{Guid.NewGuid()}_{Path.GetFileName(request.Cover.FileName)}";
                var coverPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "covers", coverName);

                using (var stream = new FileStream(coverPath, FileMode.Create))
                {
                    await request.Cover.CopyToAsync(stream);
                }

                // Удаляем старую обложку
                var oldCoverPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.CoverPath);
                if (System.IO.File.Exists(oldCoverPath))
                {
                    System.IO.File.Delete(oldCoverPath);
                }

                book.CoverPath = $"covers/{coverName}";
            }

            _context.Books.Update(book);
            await _context.SaveChangesAsync();
            return Ok("Книга успешно обновлена.");
        }

    }

    public class BookRequest
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string FilePath { get; set; } // Имя файла в папке /wwwroot/books
    }
}
