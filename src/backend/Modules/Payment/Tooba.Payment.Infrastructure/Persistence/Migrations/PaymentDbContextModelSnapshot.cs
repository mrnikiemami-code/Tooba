using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Tooba.Payment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Payment.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PaymentDbContext))]
    partial class PaymentDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("payment");
        }
    }
}
