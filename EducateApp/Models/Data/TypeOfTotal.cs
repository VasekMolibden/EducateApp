using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EducateApp.Models.Data
{
    public class TypeOfTotal
    {
        // Key - поле первичный ключ
        // DatabaseGenerated(DatabaseGeneratedOption.Identity) - поле автоинкрементное
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Display(Name = "ИД")]
        public short Id { get; set; }

        [Required(ErrorMessage = "Введите название промежуточной аттестации")]
        [Display(Name = "Промежуточная аттестация")]
        public string CertificateName { get; set; }

        [Required]
        public string IdUser { get; set; }

        // Навигационные свойства
        // свойство нужно для более правильного отображения данных в представлениях
        [ForeignKey("IdUser")]
        public User User { get; set; }
    }
}
