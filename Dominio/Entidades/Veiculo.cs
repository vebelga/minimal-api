using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace minimal_api.Dominio.Entidades
{
    public class Veiculo
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; } = default!;
        [Required]
        [StringLength(150)]
        public string Nome { get; set; } = default!;
        [StringLength(100)]
        [Required]
        public string Marca { get; set; } = default!;
        [StringLength(10)]
        [Required]
        public int Ano { get; set; } = default!;
    }
}
