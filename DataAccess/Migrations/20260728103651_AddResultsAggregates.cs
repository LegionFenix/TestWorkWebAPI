using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddResultsAggregates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "delta",
                table: "ResultsRecord");

            migrationBuilder.AddColumn<double>(
                name: "AvgExecutionTime",
                table: "ResultsRecord",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgValue",
                table: "ResultsRecord",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DeltaSeconds",
                table: "ResultsRecord",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "FileName",
                table: "ResultsRecord",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "MaxValue",
                table: "ResultsRecord",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "MedianValue",
                table: "ResultsRecord",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateTime>(
                name: "MinDate",
                table: "ResultsRecord",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<double>(
                name: "MinValue",
                table: "ResultsRecord",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateIndex(
                name: "IX_ResultsRecord_FileName",
                table: "ResultsRecord",
                column: "FileName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResultsRecord_FileName",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "AvgExecutionTime",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "AvgValue",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "DeltaSeconds",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "FileName",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "MaxValue",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "MedianValue",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "MinDate",
                table: "ResultsRecord");

            migrationBuilder.DropColumn(
                name: "MinValue",
                table: "ResultsRecord");

            migrationBuilder.AddColumn<long>(
                name: "delta",
                table: "ResultsRecord",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }
    }
}
