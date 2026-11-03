using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaGetter.Database.Sqlite.Migrations
{
    /// <summary>
    /// Adds the table that <see cref="SqliteStorageService"/> keeps package and symbol files in.
    /// It is not an EF entity, so the model snapshot does not change.
    /// </summary>
    [DbContext(typeof(SqliteContext))]
    [Migration("20261104000000_AddStorageBlobs")]
    public partial class AddStorageBlobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StorageBlobs",
                columns: table => new
                {
                    Path = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    Content = table.Column<byte[]>(type: "BLOB", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StorageBlobs", x => x.Path);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "StorageBlobs");
        }
    }
}
