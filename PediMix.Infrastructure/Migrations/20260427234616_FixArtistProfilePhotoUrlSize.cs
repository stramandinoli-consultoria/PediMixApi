using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PediMix.Infrastructure.Migrations
{
    public partial class FixArtistProfilePhotoUrlSize : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE `ArtistProfiles`
                MODIFY COLUMN `ProfilePhotoUrl` longtext NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE `ArtistProfiles`
                MODIFY COLUMN `ProfilePhotoUrl` varchar(500) NULL;
            ");
        }
    }
}
