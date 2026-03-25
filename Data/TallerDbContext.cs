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

        public TallerDbContext(DbContextOptions<TallerDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Cliente> Clientes { get; set; }
        public DbSet<Vehiculo> Vehiculos { get; set; }
        public DbSet<Trabajo> Trabajos { get; set; }
        public DbSet<Servicio> Servicios { get; set; }
        public DbSet<Trabajos_Servicios> Trabajos_Servicios { get; set; }
        public DbSet<Repuesto> Repuestos { get; set; }
        public DbSet<Trabajos_Repuestos> Trabajos_Repuestos { get; set; }
        public DbSet<Pago> Pagos { get; set; }
        public DbSet<Reserva> Reservas { get; set; }
        public DbSet<Factura> Facturas { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
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
            // ── Claves compuestas para tablas N:N ─────────────────────────────
            modelBuilder.Entity<Trabajos_Servicios>()
                .HasKey(ts => new { ts.TrabajoID, ts.ServicioID });

            modelBuilder.Entity<Trabajos_Repuestos>()
                .HasKey(tr => new { tr.TrabajoID, tr.RepuestoID });

            // ── Relaciones ────────────────────────────────────────────────────

            // Cliente → Vehiculos
            modelBuilder.Entity<Vehiculo>()
                .HasOne(v => v.Cliente)
                .WithMany(c => c.Vehiculos)
                .HasForeignKey(v => v.ClienteID)
                .OnDelete(DeleteBehavior.Cascade);

            // Vehiculo → Trabajos
            modelBuilder.Entity<Trabajo>()
                .HasOne(t => t.Vehiculo)
                .WithMany(v => v.Trabajos)
                .HasForeignKey(t => t.VehiculoID)
                .OnDelete(DeleteBehavior.Cascade);

            // Reserva → Vehiculo
            // FIX: anteriormente había dos bloques contradictorios (Cascade + SetNull).
            // Se deja solo Cascade: si se elimina un vehículo, sus reservas se eliminan también.
            modelBuilder.Entity<Reserva>()
                .HasOne(r => r.Vehiculo)
                .WithMany()
                .HasForeignKey(r => r.VehiculoID)
                .OnDelete(DeleteBehavior.Cascade);

            // Factura → Trabajo (Restrict para no borrar facturas por accidente)
            modelBuilder.Entity<Factura>()
                .HasOne(f => f.Trabajo)
                .WithMany()
                .HasForeignKey(f => f.TrabajoID)
                .OnDelete(DeleteBehavior.Restrict);

            // ── Precisión de decimales ────────────────────────────────────────
            modelBuilder.Entity<Servicio>()
                .Property(s => s.CostoBase)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Repuesto>()
                .Property(r => r.PrecioUnitario)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Factura>()
                .Property(f => f.Subtotal)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Factura>()
                .Property(f => f.Descuento)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<Factura>()
                .Property(f => f.Total)
                .HasColumnType("decimal(10,2)");

            // Unicidad en número de factura para evitar race conditions
            modelBuilder.Entity<Factura>()
                .HasIndex(f => f.NumeroFactura)
                .IsUnique();
        }
    }
}
