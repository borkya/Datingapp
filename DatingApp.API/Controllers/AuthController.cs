using System.Threading.Tasks;
using System.Security.Claims;
using DatingApp.API.Data;
using DatingApp.API.Dtos;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace DatingApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
     private readonly IAuthRepository _repo;
    private readonly IConfiguration _config;
     public AuthController(IAuthRepository repo, IConfiguration config)
        {
          _repo = repo; 
          _config = config; 
        }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserForRegisterDto userForRegisterDto)
    {
       
        userForRegisterDto.Username =userForRegisterDto.Username.ToLower();
        if(await _repo.UserExists(userForRegisterDto.Username))
        return  BadRequest("Username already Exists");
        
        var userToCreate = new User
        {
        Username = userForRegisterDto.Username
        };
        var createdUser = await _repo.Register(userToCreate,userForRegisterDto.Password);
        return StatusCode(201);
     }
     [HttpPost("login")]
      public async Task<IActionResult> Login(UserForLoginDto UserForLoginDto)
      {
        var userFromRepo = await _repo.Login(UserForLoginDto.Username,UserForLoginDto.Password);
        if(userFromRepo == null)
        return Unauthorized();

        var claims = new[]
        {
          new  Claim(ClaimTypes.NameIdentifier,userFromRepo.Id.ToString()),
          new  Claim(ClaimTypes.Name, userFromRepo.Username)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8
            .GetBytes(_config.GetSection("AppSettings:Token").Value));

        var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha512Signature);
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
          Subject = new ClaimsIdentity(claims),
          Expires = System.DateTime.Now.AddDays(1),
          SigningCredentials = creds
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        
        return Ok(new{
          token = tokenHandler.WriteToken(token)
        });

      }
    }
}