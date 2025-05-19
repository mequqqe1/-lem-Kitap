namespace BookStore.Data
{
    public class PhysicalBook
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string CoverPath { get; set; }

        public int Stock { get; set; } // Запас книг
    }
}