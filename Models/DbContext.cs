using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Proyecto_taller.Models;

namespace Proyecto_taller.Models
{
    public class TallerDbContext : DbContext
    {
        public DbSet<Cliente> Clientes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Cambia el servidor y la base de datos según tu SQL
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=TallerMecanico;Trusted_Connection=True;TrustServerCertificate=True;");
        }
    }
}
