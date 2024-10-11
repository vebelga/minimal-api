using System.ComponentModel.DataAnnotations;

namespace minimal_api.Dominio.DTOs
{
    public record VeiculoDto
    {
        public string Nome { get; set; } = default!;
        public string Marca { get; set; } = default!;
        public int Ano { get; set; } = default!;
    }
}
