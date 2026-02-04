using Microsoft.AspNetCore.Mvc;

namespace MinhaApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsuarioController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok("API funcionando!");
        }

        [HttpGet("{id}")]
        public IActionResult GetPorId(int id)
        {
            return Ok(new { Id = id, Nome = "Matheus" });
        }

        [HttpPost]
        public IActionResult Post([FromBody] string nome)
        {
            return Ok($"Usuário {nome} criado");
        }
    }
}
