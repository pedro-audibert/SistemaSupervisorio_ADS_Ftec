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

    }
}