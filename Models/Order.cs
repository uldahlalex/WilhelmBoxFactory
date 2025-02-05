namespace Models;

public class Order
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Dictionary<Guid, int>? Boxes { get; set; }
    public Customer? Customer { get; set; }
    public ShippingStatus? ShippingStatus { get; set; }
}