using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tooba.Tax.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Tax.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(TaxDbContext))]
    partial class TaxDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("tax");
        }
    }
}
