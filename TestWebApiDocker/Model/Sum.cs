using System.ComponentModel.DataAnnotations;

namespace TestWebApiDocker.Model
{
    public class Sum
    {
        [Key]
        public int Id { get; set; }
        public int Num1 { get; set; }
        public int Num2 { get; set; }
        public int Total { get; set; }
    }
}
