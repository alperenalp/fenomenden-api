using System.ComponentModel.DataAnnotations;

namespace fenomendenAPI.Dto
{
    public class UserDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UserRegisterRequest{
        [Required]
        public string Username {get;set;} = string.Empty;
        [Required, EmailAddress]
        public string Email {get;set;} = string.Empty;
        [Required, MinLength(6, ErrorMessage = "Please enter at least 6 characters!")]
        public string Password {get;set;} = string.Empty;
        [Required, Compare("Password")]
        public string ConfirmPassword {get;set;}=string.Empty;
    }

    public class UserLoginRequest{
        [Required, EmailAddress]
        public string Email {get;set;} = string.Empty;
        [Required]
        public string Password {get;set;} = string.Empty;
    }

    public class ResetPasswordRequest{
        [Required]
        public string Token {get;set;} = string.Empty;
        [Required, MinLength(6, ErrorMessage = "Please enter at least 6 characters!")]
        public string Password {get;set;} = string.Empty;
        [Required, Compare("Password")]
        public string ConfirmPassword {get;set;}=string.Empty;
    }
}