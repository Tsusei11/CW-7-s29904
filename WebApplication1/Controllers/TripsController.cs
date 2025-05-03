using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.DTOs;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("[controller]")]
public class TripsController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    // Zwraca wszystkie wycieczki wraz z infromacja o krajach
    public async Task<IActionResult> GetTripsAsync()
    {
        return Ok(await dbService.GetTripsAsync());
    }
}