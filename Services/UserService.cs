using fenomendenAPI.Data;
using fenomendenAPI.Model;
using fenomendenAPI.Dto;
using System.Security.Cryptography;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace fenomendenAPI.Services
{
    public interface IUserService
    {
        string GetMyName();
    }

    public class UserService : IUserService
    {
        private readonly DataContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IEmailService _emailService;

        public UserService(IHttpContextAccessor httpContextAccessor, DataContext context, IEmailService emailService)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _emailService = emailService;
        }

        public string GetMyName()
        {
            var result = string.Empty;
            if (_httpContextAccessor.HttpContext != null)
            {
                result = _httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Name);
            }
            return result;
        }
    }
}