using Microsoft.AspNetCore.Mvc;

namespace PortIA.Core.WebApi.Controllers;

/// <summary>
/// Controller de prueba para validar que Swagger UI funciona correctamente.
/// </summary>
[ApiController]
[Route("api/contenedores")]
public class ContenedoresController : ControllerBase
{
    private static readonly string[] Contenedores =
    [
        "contenedor-001",
        "contenedor-002",
        "contenedor-003"
    ];

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(Contenedores);
    }

    [HttpGet("{id}")]
    public IActionResult GetById(string id)
    {
        var contenedor = Contenedores.FirstOrDefault(c => c == id);
        if (contenedor is null)
        {
            return NotFound();
        }

        return Ok(contenedor);
    }
}
