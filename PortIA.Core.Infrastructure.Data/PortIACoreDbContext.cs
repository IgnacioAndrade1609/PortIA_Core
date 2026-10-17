using Microsoft.EntityFrameworkCore;
using PortIA.Core.Infrastructure.Data.Entities;

namespace PortIA.Core.Infrastructure.Data;

public class PortIACoreDbContext(DbContextOptions<PortIACoreDbContext> options) : DbContext(options)
{
    public DbSet<Rol> Roles => Set<Rol>();
    public DbSet<Pais> Paises => Set<Pais>();
    public DbSet<Clima> Climas => Set<Clima>();
    public DbSet<Riesgo> Riesgos => Set<Riesgo>();
    public DbSet<TipoAlerta> TiposAlerta => Set<TipoAlerta>();
    public DbSet<Accion> Acciones => Set<Accion>();
    public DbSet<Canal> Canales => Set<Canal>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Naviera> Navieras => Set<Naviera>();
    public DbSet<Terminal> Terminales => Set<Terminal>();
    public DbSet<CargaDocumentos> CargasDocumentos => Set<CargaDocumentos>();
    public DbSet<Documento> Documentos => Set<Documento>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<Hito> Hitos => Set<Hito>();
    public DbSet<Alerta> Alertas => Set<Alerta>();
    public DbSet<LogNotificacion> LogsNotificacion => Set<LogNotificacion>();
    public DbSet<LogAuditoria> LogsAuditoria => Set<LogAuditoria>();

    public DbSet<EstadoContainer> EstadosContainer => Set<EstadoContainer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rol>(e =>
        {
            e.ToTable("ROLES");
            e.Property(x => x.Id).HasColumnName("ID_ROL").UseIdentityAlwaysColumn();
            e.Property(x => x.NombreRol).HasColumnName("NOMBRE_ROL").HasMaxLength(20).IsRequired();
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasIndex(x => x.NombreRol).IsUnique();
        });

        modelBuilder.Entity<Pais>(e =>
        {
            e.ToTable("PAIS");
            e.Property(x => x.Id).HasColumnName("ID_PAIS").UseIdentityAlwaysColumn();
            e.Property(x => x.NombrePais).HasColumnName("NOMBRE_PAIS").HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.NombrePais).IsUnique();
        });

        modelBuilder.Entity<Clima>(e =>
        {
            e.ToTable("CLIMA");
            e.Property(x => x.Id).HasColumnName("ID_CLIMA").UseIdentityAlwaysColumn();
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<Riesgo>(e =>
        {
            e.ToTable("RIESGO");
            e.Property(x => x.Id).HasColumnName("ID_RIESGO").UseIdentityAlwaysColumn();
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(20).IsRequired();
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasIndex(x => x.Descripcion).IsUnique();
        });

        modelBuilder.Entity<TipoAlerta>(e =>
        {
            e.ToTable("TIPO_ALERTA");
            e.Property(x => x.Id).HasColumnName("ID_TIPO_ALERTA").UseIdentityAlwaysColumn();
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(20).IsRequired();
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasIndex(x => x.Descripcion).IsUnique();
        });

        modelBuilder.Entity<Accion>(e =>
        {
            e.ToTable("ACCION");
            e.Property(x => x.Id).HasColumnName("ID_ACCION").UseIdentityAlwaysColumn();
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Descripcion).IsUnique();
        });

        modelBuilder.Entity<Canal>(e =>
        {
            e.ToTable("CANAL");
            e.Property(x => x.Id).HasColumnName("ID_CANAL").UseIdentityAlwaysColumn();
            e.Property(x => x.Tipo).HasColumnName("TIPO").HasMaxLength(20).IsRequired();
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
        });

        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("USUARIO");
            e.Property(x => x.Id).HasColumnName("ID_USUARIO").UseIdentityAlwaysColumn();
            e.Property(x => x.NombreUsuario).HasColumnName("NOMBRE_USUARIO").HasMaxLength(250).IsRequired();
            e.Property(x => x.Email).HasColumnName("EMAIL").HasMaxLength(250).IsRequired();
            e.Property(x => x.PasswordHash).HasColumnName("PASSWORD_HASH").HasMaxLength(255).IsRequired();
            e.Property(x => x.RolId).HasColumnName("ROL_ID");
            e.Property(x => x.Creacion).HasColumnName("CREACION").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasOne(x => x.Rol).WithMany(x => x.Usuarios).HasForeignKey(x => x.RolId)
                .HasConstraintName("FK_USUARIO_ROL");
        });

        modelBuilder.Entity<Naviera>(e =>
        {
            e.ToTable("NAVIERA");
            e.Property(x => x.Id).HasColumnName("ID_NAVIERA").UseIdentityAlwaysColumn();
            e.Property(x => x.NombreNaviera).HasColumnName("NOMBRE_NAVIERA").HasMaxLength(150).IsRequired();
            e.Property(x => x.CodigoNaviera).HasColumnName("CODIGO_NAVIERA").HasMaxLength(10);
            e.Property(x => x.Email).HasColumnName("EMAIL").HasMaxLength(250);
            e.Property(x => x.Telefono).HasColumnName("TELEFONO").HasMaxLength(50);
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
        });

        modelBuilder.Entity<Terminal>(e =>
        {
            e.ToTable("TERMINAL");
            e.Property(x => x.Id).HasColumnName("ID_TERMINAL").UseIdentityAlwaysColumn();
            e.Property(x => x.CodigoTerminal).HasColumnName("CODIGO_TERMINAL").HasMaxLength(10).IsRequired();
            e.Property(x => x.NombreTerminal).HasColumnName("NOMBRE_TERMINAL").HasMaxLength(150).IsRequired();
            e.Property(x => x.IdPais).HasColumnName("ID_PAIS");
            e.Property(x => x.ConfigScraper).HasColumnName("CONFIG_SCRAPER").HasMaxLength(500);
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasIndex(x => x.CodigoTerminal).IsUnique();
            e.HasOne(x => x.Pais).WithMany(x => x.Terminales).HasForeignKey(x => x.IdPais)
                .HasConstraintName("FK_TERMINAL_PAIS");
        });

        modelBuilder.Entity<CargaDocumentos>(e =>
        {
            e.ToTable("CARGA_DOCUMENTOS", t => t.HasCheckConstraint("CK_CARGA_STATUS", "\"STATUS\" IN ('PENDIENTE','PROCESANDO','COMPLETO','ERROR')"));
            e.Property(x => x.Id).HasColumnName("ID_CARGA_DOCUMENTOS").UseIdentityAlwaysColumn();
            e.Property(x => x.UserId).HasColumnName("USER_ID");
            e.Property(x => x.Status).HasColumnName("STATUS").HasMaxLength(20).IsRequired().HasDefaultValue("PENDIENTE");
            e.Property(x => x.CreacionFecha).HasColumnName("CREACION_FECHA").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdateAt).HasColumnName("UPDATE_AT").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UserId)
                .HasConstraintName("FK_CARGA_USUARIO");
        });

        modelBuilder.Entity<Documento>(e =>
        {
            e.ToTable("DOCUMENTO");
            e.Property(x => x.Id).HasColumnName("ID_DOCUMENTO").UseIdentityAlwaysColumn();
            e.Property(x => x.IdCargaDocumentos).HasColumnName("ID_CARGA_DOCUMENTOS");
            e.Property(x => x.TipoDocumento).HasColumnName("TIPO_DOCUMENTO").HasMaxLength(20).IsRequired();
            e.Property(x => x.StorageUrl).HasColumnName("STORAGE_URL").HasMaxLength(500).IsRequired();
            e.Property(x => x.HuellaHash).HasColumnName("HUELLA_HASH").HasMaxLength(64).IsRequired();
            e.Property(x => x.Confidence).HasColumnName("CONFIDENCE").HasColumnType("real");
            e.Property(x => x.EstadoExtraccion).HasColumnName("ESTADO_EXTRACCION").HasMaxLength(20).IsRequired().HasDefaultValue("PENDIENTE");
            e.Property(x => x.DatosExtraidosRaw).HasColumnName("DATOS_EXTRAIDOS_RAW").HasColumnType("jsonb");
            e.Property(x => x.ContieneDatosPersonales).HasColumnName("CONTIENE_DATOS_PERSONALES").HasDefaultValue(true);
            e.Property(x => x.RetencionHasta).HasColumnName("RETENCION_HASTA").HasColumnType("timestamptz");
            e.Property(x => x.CreateAt).HasColumnName("CREATE_AT").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasIndex(x => x.HuellaHash).IsUnique().HasDatabaseName("UQ_DOCUMENTO_HASH");
            e.HasOne(x => x.CargaDocumentos).WithMany(x => x.Documentos).HasForeignKey(x => x.IdCargaDocumentos)
                .HasConstraintName("FK_DOCUMENTO_CARGA");
        });

        modelBuilder.Entity<Container>(e =>
        {
            e.ToTable("CONTAINER");
            e.Property(x => x.Id).HasColumnName("ID_CONTAINER").UseIdentityAlwaysColumn();
            e.Property(x => x.IdCargaDocumentos).HasColumnName("ID_CARGA_DOCUMENTOS");
            e.Property(x => x.NumeroContainer).HasColumnName("NUMERO_CONTAINER").HasMaxLength(20).IsRequired();
            e.Property(x => x.IdNaviera).HasColumnName("ID_NAVIERA");
            e.Property(x => x.PuertoDestino).HasColumnName("PUERTO_DESTINO").HasMaxLength(150);
            e.Property(x => x.NumeroBl).HasColumnName("NUMERO_BL").HasMaxLength(50);
            e.Property(x => x.NombreForwarder).HasColumnName("NOMBRE_FORWARDER").HasMaxLength(150);
            e.Property(x => x.IdTerminalOrigen).HasColumnName("ID_TERMINAL_ORIGEN");
            e.Property(x => x.IdTerminalLlegada).HasColumnName("ID_TERMINAL_LLEGADA");
            e.Property(x => x.FechaLlegadaEstimada).HasColumnName("FECHA_LLEGADA_ESTIMADA").HasColumnType("timestamptz");
            e.Property(x => x.DiasLibresDemurrage).HasColumnName("DIAS_LIBRES_DEMURRAGE").HasDefaultValue(0);
            e.Property(x => x.DiasLibresDetention).HasColumnName("DIAS_LIBRES_DETENTION").HasDefaultValue(0);
            e.Property(x => x.FechaArriboReal).HasColumnName("FECHA_ARRIBO_REAL").HasColumnType("timestamptz");
            e.Property(x => x.FechaRetiroContainer).HasColumnName("FECHA_RETIRO_CONTAINER").HasColumnType("timestamptz");
            e.Property(x => x.IdEstado).HasColumnName("ID_ESTADO");
            e.Property(x => x.IdClima).HasColumnName("ID_CLIMA");
            e.Property(x => x.DeadlineDemurrage).HasColumnName("DEADLINE_DEMURRAGE").HasColumnType("timestamptz");
            e.Property(x => x.DeadlineDetention).HasColumnName("DEADLINE_DETENTION").HasColumnType("timestamptz");
            e.Property(x => x.IdRiesgo).HasColumnName("ID_RIESGO");
            e.Property(x => x.AsignadoA).HasColumnName("ASIGNADO_A");
            e.Property(x => x.CreateAt).HasColumnName("CREATE_AT").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
            e.Property(x => x.UpdateAt).HasColumnName("UPDATE_AT").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);

            e.HasIndex(x => x.IdCargaDocumentos).HasDatabaseName("IDX_CONTAINER_CARGA");
            e.HasIndex(x => x.IdRiesgo).HasDatabaseName("IDX_CONTAINER_RIESGO");
            e.HasIndex(x => x.AsignadoA).HasDatabaseName("IDX_CONTAINER_ASIGNADO");

            e.HasOne(x => x.CargaDocumentos).WithMany(x => x.Containers).HasForeignKey(x => x.IdCargaDocumentos)
                .HasConstraintName("FK_CONTAINER_CARGA");
            e.HasOne(x => x.Naviera).WithMany(x => x.Containers).HasForeignKey(x => x.IdNaviera)
                .HasConstraintName("FK_CONTAINER_NAVIERA");
            e.HasOne(x => x.Clima).WithMany(x => x.Containers).HasForeignKey(x => x.IdClima)
                .HasConstraintName("FK_CONTAINER_CLIMA");
            e.HasOne(x => x.Riesgo).WithMany(x => x.Containers).HasForeignKey(x => x.IdRiesgo)
                .HasConstraintName("FK_CONTAINER_RIESGO");
            e.HasOne(x => x.Asignado).WithMany().HasForeignKey(x => x.AsignadoA)
                .HasConstraintName("FK_CONTAINER_ASIGNADO");
            e.HasOne(x => x.TerminalOrigen).WithMany(x => x.ContainersOrigen).HasForeignKey(x => x.IdTerminalOrigen)
                .HasConstraintName("FK_CONTAINER_TERMINAL_ORIGEN").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.TerminalLlegada).WithMany(x => x.ContainersLlegada).HasForeignKey(x => x.IdTerminalLlegada)
                .HasConstraintName("FK_CONTAINER_TERMINAL_LLEGADA").OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.EstadoContainer)
            .WithMany(x => x.Containers)
            .HasForeignKey(x => x.IdEstado)
            .HasConstraintName("FK_CONTAINER_ESTADO")
            .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Hito>(e =>
        {
            e.ToTable("HITO", t => t.HasCheckConstraint("CK_HITO_ORIGEN",
                "(\"ORIGEN\" = 'USUARIO' AND \"CREADO_POR\" IS NOT NULL) OR (\"ORIGEN\" IN ('IA','SCRAPER') AND \"CREADO_POR\" IS NULL)"));
            e.Property(x => x.Id).HasColumnName("ID_HITO").UseIdentityAlwaysColumn();
            e.Property(x => x.IdContainer).HasColumnName("ID_CONTAINER");
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(500).IsRequired();
            e.Property(x => x.Origen).HasColumnName("ORIGEN").HasMaxLength(20).IsRequired();
            e.Property(x => x.CreadoPor).HasColumnName("CREADO_POR");
            e.Property(x => x.FechaOcurrencia).HasColumnName("FECHA_OCURRENCIA").HasColumnType("timestamptz").IsRequired();

            e.HasIndex(x => x.IdContainer).HasDatabaseName("IDX_HITO_CONTAINER");

            e.HasOne(x => x.Container).WithMany(x => x.Hitos).HasForeignKey(x => x.IdContainer)
                .HasConstraintName("FK_HITO_CONTAINER");
            e.HasOne(x => x.CreadoPorUsuario).WithMany().HasForeignKey(x => x.CreadoPor)
                .HasConstraintName("FK_HITO_USUARIO");
        });

        modelBuilder.Entity<Alerta>(e =>
        {
            e.ToTable("ALERTA");
            e.Property(x => x.Id).HasColumnName("ID_ALERTA").UseIdentityAlwaysColumn();
            e.Property(x => x.IdContainer).HasColumnName("ID_CONTAINER");
            e.Property(x => x.IdTipoAlerta).HasColumnName("ID_TIPO_ALERTA");
            e.Property(x => x.IdRiesgo).HasColumnName("ID_RIESGO");
            e.Property(x => x.Mensaje).HasColumnName("MENSAJE").HasColumnType("text").IsRequired();
            e.Property(x => x.Leida).HasColumnName("LEIDA").HasDefaultValue(false);
            e.Property(x => x.UsuarioLector).HasColumnName("USUARIO_LECTOR");
            e.Property(x => x.FechaLectura).HasColumnName("FECHA_LECTURA").HasColumnType("timestamptz");
            e.Property(x => x.CreateAt).HasColumnName("CREATE_AT").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");

            e.HasIndex(x => x.IdContainer).HasDatabaseName("IDX_ALERTA_CONTAINER");
            e.HasIndex(x => x.Leida).HasDatabaseName("IDX_ALERTA_LEIDA");

            e.HasOne(x => x.Container).WithMany(x => x.Alertas).HasForeignKey(x => x.IdContainer)
                .HasConstraintName("FK_ALERTA");
            e.HasOne(x => x.TipoAlerta).WithMany(x => x.Alertas).HasForeignKey(x => x.IdTipoAlerta)
                .HasConstraintName("FK_ALERTA_TIPO");
            e.HasOne(x => x.Riesgo).WithMany(x => x.Alertas).HasForeignKey(x => x.IdRiesgo)
                .HasConstraintName("FK_ALERTA_RIESGO");
            e.HasOne(x => x.UsuarioLectorNavigation).WithMany().HasForeignKey(x => x.UsuarioLector)
                .HasConstraintName("FK_ALERTA_USUARIO_LEE");
        });

        modelBuilder.Entity<LogNotificacion>(e =>
        {
            e.ToTable("LOG_NOTIFICACION");
            e.Property(x => x.Id).HasColumnName("ID_LOG_NOTIFICACION").UseIdentityAlwaysColumn();
            e.Property(x => x.IdAlerta).HasColumnName("ID_ALERTA");
            e.Property(x => x.IdCanal).HasColumnName("ID_CANAL");
            e.Property(x => x.Destinatario).HasColumnName("DESTINATARIO").HasMaxLength(255).IsRequired();
            e.Property(x => x.Estado).HasColumnName("ESTADO").HasMaxLength(20).IsRequired().HasDefaultValue("PENDIENTE_APROBACION");
            e.Property(x => x.AprobadoPor).HasColumnName("APROBADO_POR");
            e.Property(x => x.CreateAt).HasColumnName("CREATE_AT").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");

            e.HasIndex(x => x.IdAlerta).HasDatabaseName("IDX_LOGNOTIF_ALERTA");

            e.HasOne(x => x.Alerta).WithMany(x => x.LogsNotificacion).HasForeignKey(x => x.IdAlerta)
                .HasConstraintName("FK_LOGNOTIF_ALERTA");
            e.HasOne(x => x.Canal).WithMany(x => x.LogsNotificacion).HasForeignKey(x => x.IdCanal)
                .HasConstraintName("FK_LOGNOTIF_CANAL");
            e.HasOne(x => x.AprobadoPorUsuario).WithMany().HasForeignKey(x => x.AprobadoPor)
                .HasConstraintName("FK_LOGNOTIF_USUARIO");
        });

        modelBuilder.Entity<LogAuditoria>(e =>
        {
            e.ToTable("LOG_AUDITORIA");
            e.Property(x => x.Id).HasColumnName("ID_LOG_AUDITORIA").UseIdentityAlwaysColumn();
            e.Property(x => x.NombreEntidad).HasColumnName("NOMBRE_ENTIDAD").HasMaxLength(50).IsRequired();
            e.Property(x => x.IdEntidad).HasColumnName("ID_ENTIDAD");
            e.Property(x => x.IdAccion).HasColumnName("ID_ACCION");
            e.Property(x => x.Responsable).HasColumnName("RESPONSABLE");
            e.Property(x => x.ValoresAnteriores).HasColumnName("VALORES_ANTERIORES").HasColumnType("jsonb");
            e.Property(x => x.ValoresNuevos).HasColumnName("VALORES_NUEVOS").HasColumnType("jsonb");
            e.Property(x => x.CreateAt).HasColumnName("CREATE_AT").HasColumnType("timestamptz").HasDefaultValueSql("NOW()");

            e.HasIndex(x => new { x.NombreEntidad, x.IdEntidad }).HasDatabaseName("IDX_AUDITORIA_ENTIDAD");

            e.HasOne(x => x.Accion).WithMany(x => x.LogsAuditoria).HasForeignKey(x => x.IdAccion)
                .HasConstraintName("FK_AUDITORIA_ACCION");
            e.HasOne(x => x.ResponsableUsuario).WithMany().HasForeignKey(x => x.Responsable)
                .HasConstraintName("FK_AUDITORIA_USUARIO");
        });
        modelBuilder.Entity<EstadoContainer>(e =>
        {   
            e.ToTable("ESTADO_CONTAINER");
            e.Property(x => x.Id).HasColumnName("ID_ESTADO"); // Sin autoincremental (Identity)
            e.Property(x => x.Descripcion).HasColumnName("DESCRIPCION").HasMaxLength(50).IsRequired();
            e.Property(x => x.Vigente).HasColumnName("VIGENTE").HasDefaultValue(true);
            e.HasIndex(x => x.Descripcion).IsUnique();
        });

        modelBuilder.Entity<Rol>().HasData(
            new Rol { Id = 1, NombreRol = "Analista" },
            new Rol { Id = 2, NombreRol = "Jefe" },
            new Rol { Id = 3, NombreRol = "Admin" });
        modelBuilder.Entity<Pais>().HasData(
            new Pais { Id = 1, NombrePais = "Chile" });
        modelBuilder.Entity<Clima>().HasData(
            new Clima { Id = 1, Descripcion = "Sin alerta" },
            new Clima { Id = 2, Descripcion = "Marejada" },
            new Clima { Id = 3, Descripcion = "Cierre de puerto" });
        modelBuilder.Entity<Riesgo>().HasData(
            new Riesgo { Id = 1, Descripcion = "OK" },
            new Riesgo { Id = 2, Descripcion = "WARNING" },
            new Riesgo { Id = 3, Descripcion = "CRITICAL" },
            new Riesgo { Id = 4, Descripcion = "OVERDUE" });
        modelBuilder.Entity<TipoAlerta>().HasData(
            new TipoAlerta { Id = 1, Descripcion = "DEMURRAGE" },
            new TipoAlerta { Id = 2, Descripcion = "DETENTION" });
        modelBuilder.Entity<Accion>().HasData(
            new Accion { Id = 1, Descripcion = "CREAR" },
            new Accion { Id = 2, Descripcion = "ACTUALIZAR" },
            new Accion { Id = 3, Descripcion = "LEER" });
        modelBuilder.Entity<Canal>().HasData(
            new Canal { Id = 1, Tipo = "EMAIL" },
            new Canal { Id = 2, Tipo = "TELEGRAM" });
        modelBuilder.Entity<EstadoContainer>().HasData(
            new EstadoContainer { Id = 1, Descripcion = "PLANIFICADO" },
            new EstadoContainer { Id = 2, Descripcion = "EN_PATIO_ORIGEN" },
            new EstadoContainer { Id = 3, Descripcion = "EMBARCADO" },
            new EstadoContainer { Id = 4, Descripcion = "EN_TRANSITO" },
            new EstadoContainer { Id = 5, Descripcion = "ARRIBADO" },
            new EstadoContainer { Id = 6, Descripcion = "DESCARGADO" },
            new EstadoContainer { Id = 7, Descripcion = "EN_INSPECCION" },
            new EstadoContainer { Id = 8, Descripcion = "LIBERADO" },
            new EstadoContainer { Id = 9, Descripcion = "DISPONIBLE_RETIRO" },
            new EstadoContainer { Id = 10, Descripcion = "RETIRADO" },
            new EstadoContainer { Id = 11, Descripcion = "ENTREGADO_DESTINO" },
            new EstadoContainer { Id = 12, Descripcion = "DEVUELTO_VACIO" },
            new EstadoContainer { Id = 13, Descripcion = "DEMURRAGE" },
            new EstadoContainer { Id = 14, Descripcion = "VENCIDO" },
            new EstadoContainer { Id = 15, Descripcion = "RETENIDO" },
            new EstadoContainer { Id = 16, Descripcion = "DANADO" }

        );
        
    }
}
