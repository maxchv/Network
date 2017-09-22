using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApp
{
    class Employee
    {
        [Key]
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public string Name { get; set; }

        public int Age { get; set; }

        public string Post { get; set; }

        public decimal Salary { get; set; }

        public override string ToString()
        {
            return $"ID: {Id}\r\n" +
                   $"Name: {Name}\r\n" +
                   $"Age: {Age}\r\n" +
                   $"Post: {Post}\r\n" +
                   $"Salary: {Salary}\r\n";
        }
    }
}
