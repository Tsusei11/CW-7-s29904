using System.Data.SqlTypes;
using Microsoft.Data.SqlClient;
using WebApplication1.Exceptions;
using WebApplication1.Models;
using WebApplication1.Models.DTOs;

namespace WebApplication1.Services;

public interface IDbService
{
    public Task<IEnumerable<TripGetDTO>> GetTripsAsync();
    
    public Task<IEnumerable<RegisteredTripGetDTO>> GetTripsByClientIdAsync(int clientId);
    public Task<Client> AddClientAsync(ClientAddDTO client);
    public Task AddTripToClientAsync(int clientId, int tripId);
    public Task RemoveTripFromClientAsync(int clientId, int tripId);
}

public class DbService (IConfiguration configuration) : IDbService
{
    private readonly string? _connectionString = configuration.GetConnectionString("Default");


    public async Task<IEnumerable<TripGetDTO>> GetTripsAsync()
    {
        var result = new List<TripGetDTO>();
        
        await using var connection = new SqlConnection(_connectionString);
        // Zwraca szczegoly kazdej wycieczki
        const string sql = "SELECT IdTrip, Name, Description, DateFrom, DateTo, MaxPeople FROM Trip";
        await using var command = new SqlCommand(sql, connection);
        
        await connection.OpenAsync();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new TripGetDTO()
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                Countries = await GetCountriesByTripId(reader.GetInt32(0)),
            });
        }
        
        return result;
    }

    public async Task<IEnumerable<RegisteredTripGetDTO>> GetTripsByClientIdAsync(int clientId)
    {
        if (!await DoesClientExistAsync(clientId))
            throw new NotFoundException($"Client {clientId} not found");
        
        var result = new List<RegisteredTripGetDTO>();
        
        await using var connection = new SqlConnection(_connectionString);
        // Zwraca szczegoly wycieczek na ktore jest zarejestrowany klient o podanym id
        const string sql =
            @"SELECT T.IdTrip, T.Name, T.Description, T.DateFrom, T.DateTo, T.MaxPeople, C.RegisteredAt, C.PaymentDate FROM Trip T
                             JOIN Client_Trip C on T.IdTrip = C.IdTrip
                             WHERE IdClient = @clientId";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@clientId", clientId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            int? paymentDate = null;
            try
            {
                paymentDate = reader.GetInt32(7);
            }
            catch (SqlNullValueException e){}

            result.Add(new RegisteredTripGetDTO()
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Description = reader.GetString(2),
                DateFrom = reader.GetDateTime(3),
                DateTo = reader.GetDateTime(4),
                MaxPeople = reader.GetInt32(5),
                RegisteredAt = reader.GetInt32(6),
                PaymentDate = paymentDate,
                Countries = await GetCountriesByTripId(reader.GetInt32(0)),
            });
        }
        
        if (result.Count == 0)
            throw new NotFoundException($"Client {clientId} hasn't any registered trips");
        
        return result;
    }
    
    public async Task<Client> AddClientAsync(ClientAddDTO client)
    {
        await using var connection = new SqlConnection(_connectionString);
        // Dodaje do bazy nowego klienta, zwraca jego id
        const string sql = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel) 
                            VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); SELECT SCOPE_IDENTITY()";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@FirstName", client.FirstName);
        command.Parameters.AddWithValue("@LastName", client.LastName);
        command.Parameters.AddWithValue("@Email", client.Email);
        command.Parameters.AddWithValue("@Telephone", client.Telephone);
        command.Parameters.AddWithValue("@Pesel", client.Pesel);
        
        var id = Convert.ToInt32(await command.ExecuteScalarAsync());

        return new Client
        {
            Id = id,
            FirstName = client.FirstName,
            LastName = client.LastName,
            Email = client.Email,
            Telephone = client.Telephone,
            Pesel = client.Pesel,
        };
    }

    public async Task AddTripToClientAsync(int clientId, int tripId)
    {
        if (!await DoesClientExistAsync(clientId))
            throw new NotFoundException($"Client {clientId} not found");
        
        if (GetTripsAsync().Result.FirstOrDefault(t => t.Id == tripId) == null)
            throw new NotFoundException($"Trip {tripId} not found");
        
        if (GetCurrentNumberOfTripMembersAsync(tripId).Result == GetTripsAsync().Result.First(t => t.Id == tripId).MaxPeople)
            throw new MaxMembersReachedException("Maximum of trip members has been reached");
        
        await using var connection = new SqlConnection(_connectionString);
        // Rejestruje klienta o podanym id na wycieczke o podanym id
        const string sql = "INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@clientId, @TripId, @RegisteredAt)";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@clientId", clientId);
        command.Parameters.AddWithValue("@TripId", tripId);
        command.Parameters.AddWithValue("@RegisteredAt", Convert.ToInt32(DateTime.Now.ToString("yyyyMMdd")));
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveTripFromClientAsync(int clientId, int tripId)
    {
        if (!await DoesClientExistAsync(clientId))
            throw new NotFoundException($"Client {clientId} not found");
        
        if (GetTripsAsync().Result.FirstOrDefault(t => t.Id == tripId) == null)
            throw new NotFoundException($"Trip {tripId} not found");
        
        await using var connection = new SqlConnection(_connectionString);
        // Usuwa rejestracje klienta o podanym id na wycieczke o podaym id
        const string sql = "DELETE FROM Client_Trip WHERE IdTrip = @tripId AND IdClient = @clientId";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@tripId", tripId);
        command.Parameters.AddWithValue("@clientId", clientId);
        var numOfRows = await command.ExecuteNonQueryAsync();
        
        if (numOfRows == 0)
            throw new NotFoundException($"Client {clientId} hasn't been registered to the trip {tripId}");
    }
    
    private async Task<IEnumerable<CountryGetDTO>> GetCountriesByTripId(int tripId)
    {
        var result = new List<CountryGetDTO>();
        
        await using var connection = new SqlConnection(_connectionString);
        // Zwraca wszystkie kraje dla wycieczki o podanym id
        const string sql = @"SELECT Name FROM Country
                             WHERE IdCountry IN (SELECT IdCountry FROM Country_Trip WHERE IdTrip = @id)";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@id", tripId);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            result.Add(new CountryGetDTO()
            {
                Name = reader.GetString(0)
            });
        }

        return result;
    }
    
    private async Task<bool> DoesClientExistAsync(int clientId)
    {
        await using var connection = new SqlConnection(_connectionString);
        // Sprawdza czy istnieje klient o podanym id
        const string sql = "SELECT 1 FROM Client WHERE IdClient = @clientId";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@clientId", clientId);
        var selected = await command.ExecuteScalarAsync();

        return selected != null;
    }

    private async Task<int> GetCurrentNumberOfTripMembersAsync(int tripId)
    {
        await using var connection = new SqlConnection(_connectionString);
        // Zwraca liczbe klientow ktore sa obecnie zarejestrowane na wycieczke o podanym id
        const string sql = @"SELECT COUNT(1) FROM Client_Trip WHERE IdTrip = @tripId";
        await using var command = new SqlCommand(sql, connection);
        await connection.OpenAsync();
        command.Parameters.AddWithValue("@tripId", tripId);
        var number = await command.ExecuteScalarAsync();
        return Convert.ToInt32(number);
    }
}