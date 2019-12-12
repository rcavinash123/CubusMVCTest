using System.ComponentModel.DataAnnotations;
namespace CubusMVCTest.Models
{
    public class LoginModel
    {
        [Required (ErrorMessage="Username is Required")]
        public string Username { get; set; }

        [Required (ErrorMessage="Password is Required")]
        public string Password { get; set; }
        public string Error { get; set; }
        public string Version {get;set;}
    }
}