using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BookStore.Data;
using Álem_Kitap.Models;

namespace BookStore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("physical")]
        public IActionResult GetPhysicalBooks() => Ok(_context.PhysicalBooks);

        [HttpGet("physical/{id}")]
        public IActionResult GetPhysicalBook(int id)
        {
            var book = _context.PhysicalBooks.Find(id);
            return book == null ? NotFound() : Ok(book);
        }

        public class PhysicalBookPurchaseRequest
        {
            public int Quantity { get; set; }
            public string CustomerName { get; set; }
            public string PhoneNumber { get; set; }
            public string Email { get; set; }
            public string City { get; set; }
            public string Address { get; set; }
            public string PostalCode { get; set; }
        }
        [Authorize]
        [HttpPost("buy-physical/{bookId}")]
        public async Task<IActionResult> BuyPhysicalBook(int bookId, [FromBody] PhysicalBookPurchaseRequest req)
        {
            var book = await _context.PhysicalBooks.FindAsync(bookId);
            if (book == null) return NotFound("Книга не найдена.");
            if (book.Stock < req.Quantity) return BadRequest("Недостаточно книг.");

            book.Stock -= req.Quantity;

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var purchase = new PhysicalPurchase
            {
                UserId = userId,
                PhysicalBookId = book.Id,
                Quantity = req.Quantity,
                AmountPaid = book.Price * req.Quantity,
                PurchaseDate = DateTime.UtcNow,
                Status = OrderStatus.Created,
                CustomerName = req.CustomerName,
                PhoneNumber = req.PhoneNumber,
                Email = req.Email,
                City = req.City,
                Address = req.Address,
                PostalCode = req.PostalCode
            };

            _context.PhysicalPurchases.Add(purchase);
            await _context.SaveChangesAsync();

            return Ok(purchase);
        }
        [Authorize]
        [HttpGet("my-physical-purchases")]
        public IActionResult GetMyPhysicalPurchases()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var purchases = _context.PhysicalPurchases
                .Include(p => p.PhysicalBook)
                .Where(p => p.UserId == userId)
                .ToList();

            return Ok(purchases);
        }
        [HttpGet]
        public async Task<IActionResult> GetBooks()
        {
            var books = await _context.Books.ToListAsync();
            return Ok(books);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null) return NotFound("Книга не найдена.");
            return Ok(book);
        }

        [Authorize]
        [HttpPost("buy/{bookId}")]
        public async Task<IActionResult> BuyBook(int bookId, [FromBody] PaymentRequest request)
        {
            var book = await _context.Books.FindAsync(bookId);
            if (book == null) return NotFound("Книга не найдена.");

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var purchase = new Purchase
            {
                UserId = userId,
                BookId = book.Id,
                AmountPaid = request.Amount,
                PurchaseDate = DateTime.UtcNow
            };

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();
            return Ok("Книга успешно куплена.");
        }

        [Authorize]
        [HttpGet("my-purchases")]
        public async Task<IActionResult> GetMyPurchases()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            var purchases = await _context.Purchases
                .Where(p => p.UserId == userId)
                .Include(p => p.Book)
                .ToListAsync();

            return Ok(purchases);
        }

        [Authorize]
        [HttpGet("read/{bookId}")]
        public async Task<IActionResult> ReadBook(int bookId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Проверяем, купил ли пользователь эту книгу
            var purchase = await _context.Purchases
                .FirstOrDefaultAsync(p => p.UserId == userId && p.BookId == bookId);

            if (purchase == null)
            {
                return Forbid("У вас нет доступа к этой книге.");
            }

            var book = await _context.Books.FindAsync(bookId);
            if (book == null || string.IsNullOrEmpty(book.FilePath))
            {
                return NotFound("Файл книги не найден.");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.FilePath);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Файл не найден.");
            }

            // Открываем поток для чтения PDF-файла
            var fileStream = System.IO.File.OpenRead(filePath);

            // Возвращаем поток с типом application/pdf, чтобы читать в приложении
            return File(fileStream, "application/pdf");
        }

        [Authorize]
        [HttpGet("generate-access-token/{bookId}")]
        public async Task<IActionResult> GenerateAccessToken(int bookId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            // Проверяем, купил ли пользователь эту книгу
            var purchase = await _context.Purchases
                .FirstOrDefaultAsync(p => p.UserId == userId && p.BookId == bookId);

            if (purchase == null)
            {
                return Forbid("У вас нет доступа к этой книге.");
            }

            // Генерируем временный токен (валидный 1 час)
            var token = Guid.NewGuid().ToString("N");
            var expiresAt = DateTime.UtcNow.AddHours(1);

            // Сохраняем токен в базе или кэше (можно использовать Redis)
            var accessToken = new AccessToken
            {
                Token = token,
                BookId = bookId,
                UserId = userId,
                ExpiresAt = expiresAt
            };

            _context.AccessTokens.Add(accessToken);
            await _context.SaveChangesAsync();

            return Ok(new { token, expiresAt });
        }

        [Authorize]
        [HttpGet("my-physical-orders")]
        public async Task<IActionResult> GetMyPhysicalOrders()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var orders = await _context.PhysicalPurchases
                .Include(p => p.PhysicalBook)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PurchaseDate)
                .Select(p => new PhysicalOrderResponse
                {
                    OrderId = p.Id,
                    BookTitle = p.PhysicalBook.Title,
                    CoverPath = p.PhysicalBook.CoverPath,
                    Quantity = p.Quantity,
                    TotalPrice = p.AmountPaid,
                    PurchaseDate = p.PurchaseDate,
                    Status = p.Status.ToString(),

                    CustomerName = p.CustomerName,
                    PhoneNumber = p.PhoneNumber,
                    City = p.City,
                    Address = p.Address,
                    PostalCode = p.PostalCode
                })
                .ToListAsync();

            return Ok(orders);
        }

        [HttpGet("read-token/{token}")]
        public async Task<IActionResult> ReadBookWithToken(string token)
        {
            var accessToken = await _context.AccessTokens
                .FirstOrDefaultAsync(t => t.Token == token && t.ExpiresAt > DateTime.UtcNow);

            if (accessToken == null)
            {
                return Forbid("Недействительный или истёкший токен.");
            }

            var book = await _context.Books.FindAsync(accessToken.BookId);
            if (book == null || string.IsNullOrEmpty(book.FilePath))
            {
                return NotFound("Файл книги не найден.");
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", book.FilePath);
            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("Файл не найден.");
            }

            // Открываем поток для чтения PDF-файла
            var fileStream = System.IO.File.OpenRead(filePath);
            return File(fileStream, "application/pdf");
        }


    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
    }
    public class PhysicalOrderResponse
    {
        public int OrderId { get; set; }
        public string BookTitle { get; set; }
        public string CoverPath { get; set; }
        public int Quantity { get; set; }
        public decimal TotalPrice { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string Status { get; set; }

        // Контактные данные
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }

}
