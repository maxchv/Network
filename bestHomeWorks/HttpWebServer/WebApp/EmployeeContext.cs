using System.Data.Entity;

namespace WebApp
{
    class EmployeeContext : DbContext
    {
        public EmployeeContext() : base("EmployeesCS")
        {
            
        }

        public DbSet<Employee> Employees { get; set; }
    }
}