using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tooba.Settlement.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Settlement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(SettlementDbContext))]
    partial class SettlementDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("settlement");
        }
    }
}
