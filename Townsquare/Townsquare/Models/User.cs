using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Townsquare.Models
{
    public class User: IdentityUser
    {
        [Required, StringLength(80)]
        public string FullName { get; set; } = "";
    }
}
