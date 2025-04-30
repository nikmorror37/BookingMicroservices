﻿using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RoomService.API.Migrations
{
    /// <inheritdoc />
    public partial class AddRoomExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Capacity",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfBeds",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RoomImageUrl",
                table: "Rooms",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Capacity",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "NumberOfBeds",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomImageUrl",
                table: "Rooms");
        }
    }
}
