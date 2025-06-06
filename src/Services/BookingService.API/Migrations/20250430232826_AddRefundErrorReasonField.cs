﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingService.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRefundErrorReasonField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RefundErrorReason",
                table: "Bookings",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RefundErrorReason",
                table: "Bookings");
        }
    }
}
