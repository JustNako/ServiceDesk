using System.ComponentModel.DataAnnotations;

namespace ServiceDesk.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "¬ведите логин")]
        public required string UserName { get; set; }

        [Required(ErrorMessage = "¬ведите пароль")]
        [DataType(DataType.Password)]
        public required string Password { get; set; }
    }
}
