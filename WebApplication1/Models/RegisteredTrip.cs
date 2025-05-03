namespace WebApplication1.Models;

public class RegisteredTrip
{
    public int clientId { get; set; }
    public int tripId { get; set; }
    public int RegisteredAt { get; set; }
    public int? PaymentDate { get; set; }
}