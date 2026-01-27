using System;
using System.ComponentModel.DataAnnotations;


namespace Mafrecal.WebDashboard.Models
{


    public class Log
    {
        [Key]
        [Display(Name = "ID")]
        public int Id { get; set; }

        [Required(ErrorMessage = "O timestamp é obrigatório.")]
        [Display(Name = "Data/Hora")]
        public DateTime Timestamp { get; set; }

        [Required(ErrorMessage = "O nível é obrigatório.")]
        [MaxLength(10, ErrorMessage = "O nível deve ter no máximo 10 caracteres.")]
        [Display(Name = "Nível")]
        public string Level { get; set; } = string.Empty;

        [MaxLength(50, ErrorMessage = "A origem deve ter no máximo 50 caracteres.")]
        [Display(Name = "Origem")]
        public string? Source { get; set; }

        [MaxLength(100, ErrorMessage = "O ID da origem deve ter no máximo 100 caracteres.")]
        [Display(Name = "ID da Origem")]
        public string? SourceId { get; set; }

        [MaxLength(100, ErrorMessage = "O método deve ter no máximo 100 caracteres.")]
        [Display(Name = "Método")]
        public string? Method { get; set; }

        [MaxLength(200, ErrorMessage = "O endpoint deve ter no máximo 200 caracteres.")]
        [Display(Name = "Endpoint")]
        public string? Endpoint { get; set; }

        [Display(Name = "Mensagem")]
        public string? Message { get; set; }

        [Display(Name = "Exceção")]
        public string? Exception { get; set; }

    }
}
