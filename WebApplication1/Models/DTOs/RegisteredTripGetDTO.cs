namespace WebApplication1.Models.DTOs;

public class RegisteredTripGetDTO : TripGetDTO
{
    public int RegisteredAt { get; set; }
    public int? PaymentDate { get; set; }
}