using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Usuarios.Domain.Entities.Usuarios;

namespace Usuarios.Infrastructure.Data.Configurations
{
    public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
    {
        public void Configure(EntityTypeBuilder<Usuario> builder)
        {
            builder.ToTable("usuario", schema: "identidade");

            // Chave Primária
            builder.HasKey(u => u.Guid);
            builder.Property(u => u.Guid)
                .HasColumnType("uuid")
                .HasColumnName("guid"); 

            // Propriedades Tradicionais
            builder.Property(u => u.NomeCompleto)
                .HasColumnName("nome_completo")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("varchar(200)");

            builder.Property(u => u.SenhaHash)
                .HasColumnName("senha_hash")
                .IsRequired()
                .HasMaxLength(255)
                .HasColumnType("varchar(255)");

            // Mapeamento de Enums como String
            builder.Property(u => u.Perfil)
                .HasColumnName("perfil")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasColumnType("varchar(30)");

            builder.Property(u => u.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(30)
                .HasColumnType("varchar(30)");

            // Propriedades de Auditoria
            builder.Property(u => u.CriadoPor)
                .HasColumnName("criado_por")
                .IsRequired()
                .HasMaxLength(150)
                .HasColumnType("varchar(150)");

            builder.Property(u => u.DataCriacao)
                .HasColumnName("data_criacao")
                .IsRequired()
                .HasColumnType("timestamp with time zone");             

            builder.Property(u => u.ModificadoPor)
                .HasColumnName("modificado_por")
                .HasMaxLength(150)
                .HasColumnType("varchar(150)");

            builder.Property(u => u.DataModificacao)
                .HasColumnName("data_modificacao")
                .HasColumnType("timestamp with time zone");


            // Mapeamento dos Value Objects 
            builder.OwnsOne(u => u.Email, emailBuilder =>
            {
                emailBuilder.Property(e => e.Endereco)
                    .HasColumnName("email")
                    .IsRequired()
                    .HasMaxLength(150)
                    .HasColumnType("varchar(150)");

                emailBuilder.HasIndex(e => e.Endereco)
                    .HasDatabaseName("ix_usuarios_email")
                    .IsUnique();
            });


            builder.OwnsOne(u => u.Cpf, cpfBuilder =>
            {
                cpfBuilder.Property(c => c.Numero)
                    .HasColumnName("cpf")
                    .IsRequired()
                    .HasMaxLength(11)
                    .HasColumnType("varchar(11)");

                cpfBuilder.HasIndex(c => c.Numero)
                    .HasDatabaseName("ix_usuarios_cpf")
                    .IsUnique();
            });
        }
    }
}