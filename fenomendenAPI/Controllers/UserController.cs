using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using fenomendenAPI.Data;
using fenomendenAPI.Dto;
using fenomendenAPI.Model;
using fenomendenAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace fenomendenAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UserController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly IConfiguration _configuration;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;

        public UserController(DataContext context, IConfiguration configuration, IUserService userService, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _userService = userService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<string>> Register(UserRegisterRequest request)
        {
            User checkUser = _context.users.Where(x => x.Email == request.Email).SingleOrDefault();
            if (checkUser != null)
            {
                if (request.Email == checkUser.Email)
                {
                    return BadRequest("User is already registered!");
                }
            }

            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            User newUser = new User();
            newUser.Username = request.Username;
            newUser.Email = request.Email;
            newUser.PasswordHash = passwordHash;
            newUser.PasswordSalt = passwordSalt;
            string verificationToken = CreateRandomToken();
            newUser.VerificationToken = verificationToken;
            EmailDto emailForToken = new EmailDto
            {
                To = request.Email,
                Subject = "Confirmation Code",
                Body = $"<html><body><h1>Hello {request.Username}</h1><h1></br>You must enter the confirmation code to log in. </h1></br><h1>Confirmation Code: {verificationToken} </h1></body></html>"
            };
            _emailService.SendEmail(emailForToken);
            _context.users.Add(newUser);
            _context.SaveChanges();
            return Ok("User Successfully Registered. Now you must verify.");
        }

        private string? CreateRandomToken()
        {
            //return Convert.ToHexString(RandomNumberGenerator.GetBytes(64));
            string stringChars = "0123456789ABCDEFGHJKLMNOPRSTUVYZ";
            Random rand = new Random();
            var charList = stringChars.ToArray();
            string hexString = "";
            int verifyLength = 6;
            for (int i = 0; i < verifyLength; i++)
            {
                int randIndex = rand.Next(0, charList.Length);
                hexString += charList[randIndex];
            }

            return hexString;
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            }
        }

        [HttpPost("login")]
        public async Task<ActionResult<string>> Login(UserLoginRequest request)
        {
            var user = _context.users.FirstOrDefault(x => x.Email == request.Email);
            if (user == null)
            {
                return BadRequest("User not found.");
            }

            if (!VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt))
            {
                return BadRequest("Wrong password.");
            }

            if (user.VerifiedAt == null)
            {
                return BadRequest("Not verified!");
            }

            string token = CreateToken(user);

            var refreshToken = GenerateRefreshToken();
            SetRefreshToken(refreshToken, user);

            return Ok($"Welcome, {user.Email}!");
        }

        private bool VerifyPasswordHash(string password, byte[] passwordHash, byte[] passwordSalt)
        {
            using (var hmac = new HMACSHA512(passwordSalt))
            {
                var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return computedHash.SequenceEqual(passwordHash);
            }
        }
        
        private string CreateToken(User user)
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, "standart")
            };

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
                _configuration.GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds);

            var jwt = new JwtSecurityTokenHandler().WriteToken(token);

            return jwt;
        }
        private RefreshToken GenerateRefreshToken()
        {
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                Expires = DateTime.Now.AddDays(1),
                Created = DateTime.Now
            };

            return refreshToken;
        }
        private void SetRefreshToken(RefreshToken newRefreshToken, User user)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = newRefreshToken.Expires
            };
            Response.Cookies.Append("refreshToken", newRefreshToken.Token, cookieOptions);

            user.RefreshToken = newRefreshToken.Token;
            user.TokenCreated = newRefreshToken.Created;
            user.TokenExpires = newRefreshToken.Expires;
            _context.SaveChanges();
        }

        [HttpGet, Authorize]
        public ActionResult<string> GetMe()
        {
            var userName = _userService.GetMyName();
            return Ok(userName);
        }

        [HttpPost("refresh-token")]
        public async Task<ActionResult<string>> RefreshToken(UserDto request)
        {
            var refreshToken = Request.Cookies["refreshToken"];
            User user = _context.users.Where(x => x.Username == request.Username).SingleOrDefault();
            if (user == null)
            {
                return BadRequest("User not found.");
            }
            if (!user.RefreshToken.Equals(refreshToken))
            {
                return Unauthorized("Invalid Refresh Token.");
            }
            else if (user.TokenExpires < DateTime.Now)
            {
                return Unauthorized("Token Expired.");
            }
            string token = CreateToken(user);
            var newRefreshToken = GenerateRefreshToken();
            SetRefreshToken(newRefreshToken, user);

            return Ok(token);
        }

        [HttpPost("verify")]
        public async Task<IActionResult> Verify(string verificationToken)
        {
            var user = _context.users.FirstOrDefault(x => x.VerificationToken == verificationToken);
            if (user == null)
            {
                return BadRequest("Invalid token!");
            }

            user.VerifiedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok("User verified!");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = _context.users.FirstOrDefault(x => x.Email == email);
            if (user == null)
            {
                return BadRequest("User Not Found.");
            }

            string passwordResetToken = CreateRandomToken();
            user.PasswordResetToken = passwordResetToken;
            EmailDto emailForToken = new EmailDto
            {
                To = user.Email,
                Subject = "Confirmation Code",
                Body = $"<html><body><h1>Hello {user.Username}</h1><h1></br>You must enter the confirmation code to reset your password. </h1></br><h1>Confirmation Code: {passwordResetToken} </h1></body></html>"
            };
            _emailService.SendEmail(emailForToken);
            user.ResetTokenExpires = DateTime.Now.AddDays(1);
            await _context.SaveChangesAsync();

            return Ok("You may now reset your password.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            User user = _context.users.FirstOrDefault(x => x.PasswordResetToken == request.Token);
            if (user == null || user.ResetTokenExpires<DateTime.Now)
            {
                return BadRequest("Invalid Token.");
            }

            CreatePasswordHash(request.Password, out byte[] passwordHash, out byte[] passwordSalt);
            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.PasswordResetToken = null;
            user.ResetTokenExpires = null;
            await _context.SaveChangesAsync();

            return Ok("Password successfully reset.");
        }
    }
}