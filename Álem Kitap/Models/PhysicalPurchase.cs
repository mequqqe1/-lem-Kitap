namespace BookStore.Data
{
    public class PhysicalPurchase
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User User { get; set; }

        public int PhysicalBookId { get; set; }
        public PhysicalBook PhysicalBook { get; set; }

        public int Quantity { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PurchaseDate { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Created;

        // Данные клиента
        public string CustomerName { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string PostalCode { get; set; }
    }
}