namespace Álem_Kitap.Models
{
    public class AccessToken
    {
        public int Id { get; set; }
        public string Token { get; set; }
        public int BookId { get; set; }
        public int UserId { get; set; }
        public DateTime ExpiresAt { get; set; }
    }

}
