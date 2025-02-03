using GeoMarker.Frontiers.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace GeoMarker.Frontiers.Web.Data
{
    public class UserRequestsDbContext : DbContext
    {
        public DbSet<UserRequest> Requests { get; set; }
        public DbSet<RecordsProcessed> RecordsProcessed { get; set; }

        public UserRequestsDbContext(DbContextOptions<UserRequestsDbContext> options)
        : base(options)
        {
        }
    }
}
