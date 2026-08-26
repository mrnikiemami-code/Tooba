using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Returns.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Returns.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ReturnsDbContext))]
    [Migration("20260827020000_InitialReturns")]
    partial class InitialReturns
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("returns");
        }
    }
}
