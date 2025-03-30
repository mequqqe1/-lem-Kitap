namespace BookStore.Data
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string FilePath { get; set; } // Путь к файлу книги
        public string CoverPath { get; set; } // Путь к обложке книги
    }

}