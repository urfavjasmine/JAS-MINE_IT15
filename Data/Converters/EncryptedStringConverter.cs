using JAS_MINE_IT15.Services;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace JAS_MINE_IT15.Data.Converters
{
    public class EncryptedStringConverter : ValueConverter<string?, string?>
    {
        public EncryptedStringConverter(IFieldEncryptionService encryptionService)
            : base(
                value => encryptionService.Encrypt(value),
                value => encryptionService.Decrypt(value))
        {
        }
    }
}
