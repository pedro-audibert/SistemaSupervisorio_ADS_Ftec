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
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 
            // Define um índice na coluna Timestamp da tabela VelocidadeInstMaquina
            // para otimizar a carga inicial do gráfico de velocidade.
            builder.Entity<VelocidadeInstMaquina>()
                .HasIndex(v => v.Timestamp);

            // Define um índice na coluna Timestamp da tabela ProducaoInstMaquina
            // para otimizar a carga inicial do gráfico de produção.
            builder.Entity<ProducaoInstMaquina>()
                .HasIndex(p => p.Timestamp);

            // Define um índice composto na tabela EventosMaquina.
            // Isso acelera as buscas por TipoEvento e já pré-ordena os resultados por data.
            builder.Entity<EventoMaquina>()
                .HasIndex(e => new { e.TipoEvento, e.Timestamp });
        }
    }
}