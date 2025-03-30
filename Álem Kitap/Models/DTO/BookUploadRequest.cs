using Microsoft.AspNetCore.Http;

namespace BookStore.Data
{
    public class BookUploadRequest
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public IFormFile File { get; set; } // Файл книги
        public IFormFile Cover { get; set; } // Обложка книги
    }

}
