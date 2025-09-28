using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SpendSharp.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public decimal Value { get; set; } = 0;

        [Required]
        public string? Description { get; set; } = string.Empty;

        // public DateTime createdAt { get; set; } = DateTime.Now;
    }
}
