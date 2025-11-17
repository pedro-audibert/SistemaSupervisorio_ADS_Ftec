using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using mmdba.Models;
using mmdba.Models.Entidades;

namespace mmdba.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<EventoMaquina> EventosMaquina { get; set; }
        public DbSet<VelocidadeInstMaquina> VelocidadeInstMaquina { get; set; }
        public DbSet<ProducaoInstMaquina> ProducaoInstMaquina { get; set; }
        public DbSet<RegraNotificacao> RegrasNotificacao { get; set; }
        public DbSet<OeeParametrosMaquina> OeeParametrosMaquina { get; set; }
        public DbSet<Turno> Turnos { get; set; }
        public DbSet<ParadaPlanejada> ParadasPlanejadas { get; set; }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 

            builder.Entity<VelocidadeInstMaquina>()
                .HasIndex(v => v.Timestamp);


            builder.Entity<ProducaoInstMaquina>()
                .HasIndex(p => p.Timestamp);


            builder.Entity<EventoMaquina>()
                .HasIndex(e => new { e.TipoEvento, e.Timestamp });
        }
    }
}