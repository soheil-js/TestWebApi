using NSec.Cryptography;
using System.Security.Cryptography;

namespace TestWebApi.Services
{
    public class PasswordHasher
    {
        private readonly Argon2Parameters _argon2Params;

        public PasswordHasher(long memorySize, long numberOfPasses, int degreeOfParallelism)
        {
            _argon2Params = new Argon2Parameters
            {
                MemorySize = memorySize,
                NumberOfPasses = numberOfPasses,
                DegreeOfParallelism = degreeOfParallelism,
            };
        }

        public string HashPassword(string password)
        {
            var argon2id = PasswordBasedKeyDerivationAlgorithm.Argon2id(_argon2Params);
            byte[] salt = new byte[argon2id.MinSaltSize];
            RandomNumberGenerator.Fill(salt);

            int outputLength = 32;
            byte[] hash = argon2id.DeriveBytes(password, salt, outputLength);

            string saltB64 = Convert.ToBase64String(salt);
            string hashB64 = Convert.ToBase64String(hash);
            return $"{saltB64}:{hashB64}";
        }

        public bool VerifyPassword(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return false;

            var parts = stored.Split(':');
            if (parts.Length != 2)
                return false;

            byte[] salt, storedHash;
            try
            {
                salt = Convert.FromBase64String(parts[0]);
                storedHash = Convert.FromBase64String(parts[1]);
            }
            catch
            {
                return false;
            }

            int outputLength = storedHash.Length;
            byte[] computedHash = PasswordBasedKeyDerivationAlgorithm.Argon2id(_argon2Params).DeriveBytes(password, salt, outputLength);

            return CryptographicOperations.FixedTimeEquals(computedHash, storedHash);
        }
    }
}
