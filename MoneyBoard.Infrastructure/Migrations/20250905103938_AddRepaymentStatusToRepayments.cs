using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MoneyBoard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRepaymentStatusToRepayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "repayments",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "status",
                table: "repayments");
        }
    }
}
