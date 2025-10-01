using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF_Aplikace_s_db.Data
{
    [AddINotifyPropertyChangedInterface]
    public class Student
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string LastName { get; set; } = string.Empty;

        [Range(1, 6)]
        public int Year { get; set; }

        [StringLength(50)]
        public string Email { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
