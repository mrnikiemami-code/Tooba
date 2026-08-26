using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tooba.Returns.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ReturnsDbContext))]
    partial class ReturnsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("returns");
        }
    }
}
