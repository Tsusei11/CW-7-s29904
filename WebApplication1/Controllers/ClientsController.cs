using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using WebApplication1.Exceptions;
using WebApplication1.Models.DTOs;
using WebApplication1.Services;

namespace WebApplication1.Controllers;

[ApiController]
[Route("[controller]")]
public class ClientsController(IDbService dbService) : ControllerBase
{
    [HttpGet]
    [Route("{id}/trips")]
    // Zwraca wszystkie wycieczki klienta lub kod 404 w przypadku braku klientu lub zarejestrowanych wycieczek dla niego
    public async Task<IActionResult> GetClientsTrips([FromRoute] int id)
    {
        try
        {
            return Ok(await dbService.GetTripsByClientIdAsync(id));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    // Dodaje klienta do bazy
    public async Task<IActionResult> AddClient([FromBody] ClientAddDTO body)
    {
        var client = await dbService.AddClientAsync(body);
        return Created($"clients/{client.Id}", client);
    }

    [HttpPut]
    [Route("{clientId}/trips/{tripId}")]
    // Rejestruje klienta na wycieczke
    public async Task<IActionResult> AddTripToClient([FromRoute] int clientId, [FromRoute] int tripId)
    {
        try
        {
            await dbService.AddTripToClientAsync(clientId, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (SqlException e)
        {
            return BadRequest("Client is already registered on the trip");
        }
    }

    [HttpDelete]
    [Route("{clientId}/trips/{tripId}")]
    // Anuluje rejestracje klienta na wycieczke
    public async Task<IActionResult> RemoveTripFromClient([FromRoute] int clientId, [FromRoute] int tripId)
    {
        try
        {
            await dbService.RemoveTripFromClientAsync(clientId, tripId);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
}