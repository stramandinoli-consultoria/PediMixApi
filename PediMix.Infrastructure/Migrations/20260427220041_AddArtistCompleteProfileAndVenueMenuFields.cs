using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PediMix.Infrastructure.Migrations
{
    public partial class AddArtistCompleteProfileAndVenueMenuFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TransportInfo",
                table: "ArtistProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "ArtistProfiles",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransportInfo",
                table: "ArtistProfiles");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "ArtistProfiles");
        }
    }
}
