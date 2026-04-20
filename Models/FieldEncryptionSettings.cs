using System.ComponentModel.DataAnnotations;

namespace JAS_MINE_IT15.Models
{
    public class FieldEncryptionSettings
    {
        public const string SectionName = "FieldEncryption";

        public bool Enabled { get; set; }

        // Base64 encoded 256-bit key (32 bytes)
        [MinLength(44)]
        public string Key { get; set; } = string.Empty;
    }
}
