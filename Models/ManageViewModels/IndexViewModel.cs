using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace viafront3.Models.ManageViewModels
{
    public class IndexViewModel : BaseViewModel
    {
        public string Username { get; set; }

        public bool IsEmailConfirmed { get; set; }

        public string Email { get; set; }

        public string StatusMessage { get; set; }
    }

    public class ChangeEmailViewModel : TwoFactorRequiredViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "New email")]
        public string NewEmail { get; set; }

        public string StatusMessage { get; set; }
    }

    public class ConfirmEmailChangeViewModel : BaseViewModel
    {
        [Display(Name = "Old email")]
        public string OldEmail { get; set; }

        [Display(Name = "New email")]
        public string NewEmail { get; set; }

        public string Code { get; set; }
    }
}
