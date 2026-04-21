using Microsoft.EntityFrameworkCore;
using RentasApi.Models;

namespace RentasApi.Data;

public class RentasContext : DbContext
{
    public RentasContext(DbContextOptions<RentasContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<ContratoLuz> ContratoLuz => Set<ContratoLuz>();
    public DbSet<ContratoAgua> ContratoAgua => Set<ContratoAgua>();
    public DbSet<ContratoInternet> ContratoInternet => Set<ContratoInternet>();
    public DbSet<Ubicacion> Ubicaciones => Set<Ubicacion>();
    public DbSet<Departamento> Departamentos => Set<Departamento>();
    public DbSet<HistorialInquilino> HistorialInquilinos => Set<HistorialInquilino>();
    public DbSet<Cobranza> Cobranza => Set<Cobranza>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Adjunto> Adjuntos => Set<Adjunto>();
    public DbSet<NotaDepartamento> NotasDepartamento => Set<NotaDepartamento>();
    public DbSet<ConsumoLuz> ConsumoLuz => Set<ConsumoLuz>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Usuario
        modelBuilder.Entity<Usuario>(e => { e.ToTable("Usuarios"); e.HasKey(u => u.Correo); });

        // Contratos
        modelBuilder.Entity<ContratoLuz>(e => { e.ToTable("ContratoLuz"); e.HasKey(c => c.ID); });
        modelBuilder.Entity<ContratoAgua>(e => { e.ToTable("ContratoAgua"); e.HasKey(c => c.ID); });
        modelBuilder.Entity<ContratoInternet>(e => { e.ToTable("ContratoInternet"); e.HasKey(c => c.ID); });

        // Ubicacion
        modelBuilder.Entity<Ubicacion>(e =>
        {
            e.ToTable("Ubicaciones");
            e.HasKey(u => u.IDUbicacion);
            e.HasOne(u => u.ContratoLuz).WithMany().HasForeignKey(u => u.ContratoLuzId);
            e.HasOne(u => u.ContratoAgua).WithMany().HasForeignKey(u => u.ContratoAguaId);
            e.HasOne(u => u.ContratoInternet).WithMany().HasForeignKey(u => u.ContratoInternetId);
        });

        // Departamento
        modelBuilder.Entity<Departamento>(e =>
        {
            e.ToTable("Departamento");
            e.HasKey(d => d.ID);
            e.HasOne(d => d.Ubicacion).WithMany(u => u.Departamentos).HasForeignKey(d => d.IDUbicacion);
            e.HasOne(d => d.ContratoLuz).WithMany().HasForeignKey(d => d.ContratoLuzId);
            e.HasOne(d => d.Inquilino).WithMany().HasForeignKey(d => d.InquilinoCorreo);
            e.HasIndex(d => new { d.IDUbicacion, d.Clave }).IsUnique();
        });

        // Notas Departamento
        modelBuilder.Entity<NotaDepartamento>(e =>
        {
            e.ToTable("NotasDepartamento");
            e.HasKey(n => n.ID);
            e.HasOne(n => n.Departamento).WithMany().HasForeignKey(n => n.DepartamentoId);
            e.HasOne(n => n.Usuario).WithMany().HasForeignKey(n => n.UsuarioCreo);
        });

        // HistorialInquilino
        modelBuilder.Entity<HistorialInquilino>(e =>
        {
            e.ToTable("HistorialInquilinos");
            e.HasKey(h => h.ID);
            e.HasOne(h => h.Departamento).WithMany().HasForeignKey(h => h.DepartamentoId);
            e.HasOne(h => h.Inquilino).WithMany().HasForeignKey(h => h.CorreoInquilino);
        });

        // Cobranza
        modelBuilder.Entity<Cobranza>(e => { e.ToTable("Cobranza"); e.HasKey(c => c.ID); e.HasOne(c => c.Ubicacion).WithMany().HasForeignKey(c => c.IDUbicacion); });

        // Ticket
        modelBuilder.Entity<Ticket>(e => { e.ToTable("Tickets"); e.HasKey(t => t.ID); e.HasOne(t => t.Usuario).WithMany().HasForeignKey(t => t.UsuarioCreo); });

        // Adjunto
        modelBuilder.Entity<Adjunto>(e => { e.ToTable("Adjuntos"); e.HasKey(a => a.ID); });

        // ConsumoLuz
        modelBuilder.Entity<ConsumoLuz>(e =>
        {
            e.ToTable("ConsumoLuz");
            e.HasKey(c => c.ID);
            e.HasOne<ContratoLuz>().WithMany().HasForeignKey(c => c.ContratoLuzId);
        });
    }
}
