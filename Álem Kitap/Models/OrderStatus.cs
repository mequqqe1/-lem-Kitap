namespace BookStore.Data
{
    public enum OrderStatus
    {
        Created,    // Создан
        Paid,       // Оплачен
        Shipped,    // Отправлен
        Delivered,  // Доставлен
        Cancelled   // Отменён
    }
}