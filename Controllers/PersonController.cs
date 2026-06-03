using Microsoft.AspNetCore.Mvc;
using PubSub.models;
using PubSub.services;

namespace PubSub.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonController : ControllerBase
    {
        private readonly IPersonService _personService;

        public PersonController(IPersonService personService)
        {
            _personService = personService;
        }

        [HttpPost("register")]
        public IActionResult InitSinglePerson([FromBody] PersonRegistrationRequest request)
        {
            // Validacija
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest("Username ne može biti prazan.");
            if (string.IsNullOrWhiteSpace(request.City))
                return BadRequest("Grad ne može biti prazan.");
            if (request.Age < 0 || request.Age > 150)
                return BadRequest("Godine moraju biti pozitivan broj.");
            if (string.IsNullOrWhiteSpace(request.Phone))
                return BadRequest("Broj telefona ne može biti prazan.");
            if (!int.TryParse(request.Phone, out _))
                return BadRequest("Broj telefona mora biti broj.");

            var person = new Person
            {
                Username = request.Username,
                City = request.City,
                Age = request.Age,
                Phone = request.Phone
            };

            bool success = _personService.RegisterPerson(person, letter => { });
            if (!success)
                return BadRequest("Korisnik sa ovim imenom već postoji.");

            return Ok(new { message = "Registracija uspešna!", username = person.Username });
        }

        [HttpPost("confirm/{username}")]
        public IActionResult ConfirmLetterRead(string username)
        {
            var person = _personService.FindByUsername(username);
            if (person == null)
                return NotFound("Korisnik nije pronađen.");

            _personService.ConfirmLetterRead(username);
            return Ok(new { message = "Pismo potvrđeno." });
        }

        [HttpPost("block")]
        public IActionResult BlockUser([FromBody] BlockRequest request)
        {
            var person = _personService.FindByUsername(request.BlockerUsername);
            if (person == null)
                return NotFound("Korisnik nije pronađen.");

            person.Block(request.BlockedUsername);
            return Ok(new { message = $"Korisnik {request.BlockedUsername} je blokiran." });
        }

        [HttpGet("all")]
        public IActionResult GetAllPersons()
        {
            var persons = _personService.GetAll();
            return Ok(persons);
        }
    }

    public class PersonRegistrationRequest
    {
        public string Username { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Phone { get; set; } = string.Empty;
    }

    public class BlockRequest
    {
        public string BlockerUsername { get; set; } = string.Empty;
        public string BlockedUsername { get; set; } = string.Empty;
    }
}
