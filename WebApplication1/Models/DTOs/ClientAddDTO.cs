using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.DTOs;

public class ClientAddDTO
{
    [Length(1, 32)]
    public string FirstName { get; set; }
    [Length(1, 32)]
    public string LastName { get; set; }
    [RegularExpression(@"^[0-9a-zA-Z]{1,16}@[0-9a-z]{2,16}\.[a-z]{2,8}$")]
    public string Email { get; set; }
    [RegularExpression(@"^\+\d{7,15}$")]
    public string Telephone { get; set; }
    [RegularExpression(@"^\d{11}$")]
    public string Pesel { get; set; }
}