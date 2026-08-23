using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Promotion.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Promotion.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PromotionDbContext))]
    [Migration("20260823210000_InitialPromotion")]
    partial class InitialPromotion
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("promotion");
        }
    }
}
