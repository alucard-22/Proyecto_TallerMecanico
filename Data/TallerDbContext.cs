using Azure.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Proyecto_taller.Models;
using Proyecto_taller.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proyecto_taller.Data
{
    public class TallerDbContext : DbContext
    {
        public TallerDbContext() { }

        // Constructor para inyección (recomendado)
        public TallerDbContext(DbContextOptions<TallerDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Trabajo> Trabajos { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Trabajos_Servicios> TrabajosServicios { get; set; }
        public DbSet<Repuesto> Repuestos { get; set; }
        public DbSet<Trabajos_Repuestos> TrabajosRepuestos { get; set; }
        public DbSet<Pago> Pagos { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // Leer connection string desde appsettings.json (útil para ejecución directa)
                var config = new ConfigurationBuilder()
                                .SetBasePath(Directory.GetCurrentDirectory())
                                .AddJsonFile("appsettings.json", optional: true)
                                .Build();

                var conn = config.GetConnectionString("TallerDB")
                           ?? "Server=localhost;Database=TallerMecanico;Trusted_Connection=True;TrustServerCertificate=True;";

                optionsBuilder.UseSqlServer(conn);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Claves compuestas para tablas N:N
            modelBuilder.Entity<Trabajos_Servicios>()
                .HasKey(ts => new { ts.TrabajoID, ts.ServicioID });

            modelBuilder.Entity<Trabajos_Repuestos>()
                .HasKey(tr => new { tr.TrabajoID, tr.RepuestoID });

            // Relaciones (ejemplos)
            modelBuilder.Entity<Vehiculo>()
                .HasOne(v => v.Cliente)
                .WithMany(c => c.Vehiculos)
                .HasForeignKey(v => v.ClienteID)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Trabajo>()
                .HasOne(t => t.Vehiculo)
                .WithMany(v => v.Trabajos)
                .HasForeignKey(t => t.VehiculoID)
                .OnDelete(DeleteBehavior.Cascade);

            // Configuración adicional (precisión decimals, longitudes, etc.)
            modelBuilder.Entity<Servicio>()
                .Property(s => s.CostoBase)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Repuesto>()
                .Property(r => r.PrecioUnitario)
                .HasColumnType("decimal(10,2)");
        }
    }
}
