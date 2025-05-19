namespace BookStore.Data
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsAdmin { get; set; } = false;
        public List<Purchase> Purchases { get; set; }
        
        public List<PhysicalPurchase> PhysicalPurchases { get; set; }
    }

}
