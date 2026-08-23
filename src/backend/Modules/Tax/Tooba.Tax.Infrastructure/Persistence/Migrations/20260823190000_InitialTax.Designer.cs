using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Tooba.Tax.Infrastructure.Persistence;

#nullable disable

namespace Tooba.Tax.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(TaxDbContext))]
    [Migration("20260823190000_InitialTax")]
    partial class InitialTax
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasDefaultSchema("tax");
        }
    }
}
