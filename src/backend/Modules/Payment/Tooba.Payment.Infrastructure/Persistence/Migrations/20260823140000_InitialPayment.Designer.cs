using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Payment.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Payment.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(PaymentDbContext))]
    [Migration("20260823140000_InitialPayment")]
    partial class InitialPayment
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("payment");
        }
    }
}
