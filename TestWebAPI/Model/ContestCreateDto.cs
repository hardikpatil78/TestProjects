using System.ComponentModel.DataAnnotations;

namespace TestWebAPI.Model
{
    public class ContestCreateDto
    {
        [Required]
        public List<IFormFile> File { get; set; }
    }
}
