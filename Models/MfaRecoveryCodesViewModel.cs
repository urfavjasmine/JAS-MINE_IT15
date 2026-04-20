using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class MfaRecoveryCodesViewModel
    {
        public int RemainingCodes { get; set; }

        public List<string> NewlyGeneratedCodes { get; set; } = new();

        public string SuccessMessage { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;
    }
}
