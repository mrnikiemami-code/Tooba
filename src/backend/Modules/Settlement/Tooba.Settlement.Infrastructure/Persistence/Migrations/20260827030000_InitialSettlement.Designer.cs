using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Settlement.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Settlement.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(SettlementDbContext))]
    [Migration("20260827030000_InitialSettlement")]
    partial class InitialSettlement
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("settlement");
        }
    }
}
