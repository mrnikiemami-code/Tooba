using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tooba.Promotion.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Promotion.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PromotionDbContext))]
    partial class PromotionDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("promotion");
        }
    }
}
