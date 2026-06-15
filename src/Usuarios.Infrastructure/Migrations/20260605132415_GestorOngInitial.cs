using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GestorOngInitial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var passwordHash = "$2a$12$/pON1EOepHSUoW6ck99tg.nEgFEAT6pHnhhlQGUPDPy9atrPegd9K";
            var usuarioId = Guid.NewGuid().ToString();

            migrationBuilder.Sql($@"
                INSERT INTO ""identidade"".""usuario"" (""guid"",""nome_completo"", ""perfil"", ""senha_hash"", 
                                            ""email"", ""cpf"", ""data_criacao"", ""criado_por"", 
                                            ""modificado_por"", ""data_modificacao"", ""status"")
                VALUES (
                    '{usuarioId}',
                    'Administrador', 
                    'GESTOR_ONG',
                    '{passwordHash}', 
                    'admin@conexao-solidaria.com.br', 
                    '25688069074', 
                    NOW(),
                    'Sistema', 
                    NULL,   
                    NULL,
                    'ACTIVE'                
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM ""identidade"".""usuario"" WHERE ""email"" = 'admin@conexao-solidaria.com';
            ");
        }
    }
}
