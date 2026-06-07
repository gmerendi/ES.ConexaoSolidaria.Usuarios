using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identidade");

           
            migrationBuilder.CreateTable(
                name: "usuario",
                schema: "identidade",
                columns: table => new
                {
                    guid = table.Column<Guid>(type: "uuid", nullable: false),
                    nome_completo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    perfil = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false),
                    senha_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    cpf = table.Column<string>(type: "varchar(11)", maxLength: 11, nullable: false),
                    data_criacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    criado_por = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false),
                    modificado_por = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true),
                    data_modificacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuario", x => x.guid);
                });

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_cpf",
                schema: "identidade",
                table: "usuario",
                column: "cpf",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_email",
                schema: "identidade",
                table: "usuario",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "usuario",
                schema: "identidade");
        }
    }
}
