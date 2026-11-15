namespace PortIA.Core.Services.Component.Admin.DTOs;

public record SystemHealthResponse(
    int ContenedoresSinAsignar,
    int ContenedoresConAnalistaInactivo,
    int DocumentosPorRevisar,
    int DocumentosPendientesAtascados,
    int CargasConError,
    int CargasAtascadas,
    int NotificacionesFallidas7Dias,
    int AlertasSinLeerMas24Horas,
    DateTimeOffset GeneradoEn
);
