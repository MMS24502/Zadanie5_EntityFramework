using Zadanie5_EntityFramework.DTOs;

namespace Zadanie5_EntityFramework.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Zadanie5_EntityFramework.Context;
using Zadanie5_EntityFramework.Models;

[Route("api/[controller]")]
[ApiController]
public class TripsController : ControllerBase
{
    private readonly MasterContext _context;

    public TripsController(MasterContext context)
    {
        _context = context;
    }

    // GET: api/Trips
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripDTO>>> GetTrips()
    {
        var trips = await _context.Trips
            .Include(t => t.ClientTrips).ThenInclude(ct => ct.IdClientNavigation)
            .Include(t => t.IdCountries)
            .Select(t => new TripDTO
            {
                Name = t.Name,
                Description = t.Description,
                DateFrom = t.DateFrom,
                DateTo = t.DateTo,
                MaxPeople = t.MaxPeople,
                Countries = t.IdCountries.Select(c => new CountryDTO { Name = c.Name }).ToList(),
                Clients = t.ClientTrips.Select(ct => new ClientDTO 
                { 
                    IdClient = ct.IdClientNavigation.IdClient,
                    FirstName = ct.IdClientNavigation.FirstName, 
                    LastName = ct.IdClientNavigation.LastName,
                    Email = ct.IdClientNavigation.Email,
                    Telephone = ct.IdClientNavigation.Telephone,
                    Pesel = ct.IdClientNavigation.Pesel,
                    IdTrip = ct.IdTrip,
                    RegisteredAt = ct.RegisteredAt,
                    PaymentDate = ct.PaymentDate
                }).ToList()
            })
            .OrderByDescending(t => t.DateFrom)
            .ToListAsync();

        return Ok(trips);
    }
    
    // DELETE: api/Clients/{idClient}
    [HttpDelete("clients/{idClient}")]
    public async Task<IActionResult> DeleteClient(int idClient)
    {
        var client = await _context.Clients.Include(c => c.ClientTrips).FirstOrDefaultAsync(c => c.IdClient == idClient);
        if (client == null)
        {
            return NotFound();
        }

        if (client.ClientTrips.Any())
        {
            return BadRequest("Client cannot be deleted as they have registered trips.");
        }

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("api/trips/{idTrip}/clients")]
    public async Task<IActionResult> AddClientToTrip(int idTrip, [FromBody] ClientDTO clientDTO)
    {
        var trip = await _context.Trips.FindAsync(idTrip);
        if (trip == null)
        {
            return NotFound("Trip not found.");
        }
        
        var client = await _context.Clients.FirstOrDefaultAsync(c => c.Pesel == clientDTO.Pesel);
        if (client == null)
        {
            client = new Client
            {
                IdClient = (int)clientDTO.IdClient!,
                FirstName = clientDTO.FirstName,
                LastName = clientDTO.LastName,
                Email = clientDTO.Email,
                Telephone = clientDTO.Telephone,
                Pesel = clientDTO.Pesel
            };
            _context.Clients.Add(client);
            await _context.SaveChangesAsync();
        }
        
        bool alreadyRegistered =
            await _context.ClientTrips.AnyAsync(ct => ct.IdClient == client.IdClient && ct.IdTrip == idTrip);
        if (alreadyRegistered)
        {
            return BadRequest("Client already registered for this trip.");
        }
        
        var clientTrip = new ClientTrip
        {
            IdClient = client.IdClient,
            IdTrip = idTrip,
            RegisteredAt = DateTime.Now,
            PaymentDate = clientDTO.PaymentDate
        };
        _context.ClientTrips.Add(clientTrip);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Client added to trip successfully." });
    }
}