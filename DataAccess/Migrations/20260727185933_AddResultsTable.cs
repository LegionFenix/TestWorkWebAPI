using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddResultsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ResultsId",
                table: "ValuesRecord",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "ResultsRecord",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    delta = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultsRecord", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ValuesRecord_ResultsId",
                table: "ValuesRecord",
                column: "ResultsId");

            migrationBuilder.AddForeignKey(
                name: "FK_ValuesRecord_ResultsRecord_ResultsId",
                table: "ValuesRecord",
                column: "ResultsId",
                principalTable: "ResultsRecord",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ValuesRecord_ResultsRecord_ResultsId",
                table: "ValuesRecord");

            migrationBuilder.DropTable(
                name: "ResultsRecord");

            migrationBuilder.DropIndex(
                name: "IX_ValuesRecord_ResultsId",
                table: "ValuesRecord");

            migrationBuilder.DropColumn(
                name: "ResultsId",
                table: "ValuesRecord");
        }
    }
}
