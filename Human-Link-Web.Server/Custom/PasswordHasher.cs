﻿using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace Human_Link_Web.Server.Custom
{
    public class PasswordHasher
    {
        private const int saltSize = 128 / 8;
        private const int keySize = 256 / 8;
        private const int iterations = 100;
        private static readonly HashAlgorithmName hashAlgorithmName = HashAlgorithmName.SHA256;
        private static char delimiter = ';';

        public string Hash(string password)
        {
            // Generar un nuevo salt aleatorio para cada hash
            var salt = RandomNumberGenerator.GetBytes(saltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithmName, keySize);
            var result = string.Join(delimiter, Convert.ToBase64String(salt), Convert.ToBase64String(hash));

            // Imprimir información de depuración
            Console.WriteLine($"Debug - Salt generado: {Convert.ToBase64String(salt)}");

            return result;
        }

        public bool Verify(string passwordHash, string inputPassword)
        {
            var elements = passwordHash.Split(delimiter);
            var salt = Convert.FromBase64String(elements[0]);
            var hash = Convert.FromBase64String(elements[1]);
            var hashInput = Rfc2898DeriveBytes.Pbkdf2(inputPassword, salt, iterations, hashAlgorithmName, keySize);
            return CryptographicOperations.FixedTimeEquals(hash, hashInput);
        }

        //Usar este método cuando se quieran validar la calidad de las contraseñas
        public bool IsPasswordValid(string password)
        {
            // Validar que la contraseña cumple con requisitos mínimos de seguridad
            var hasNumber = new Regex(@"[0-9]+");
            var hasUpperChar = new Regex(@"[A-Z]+");
            var hasLowerChar = new Regex(@"[a-z]+");
            var hasSpecialChar = new Regex(@"[!@#$%^&*(),.?"":{}|<>]+");
            var hasMinLength = new Regex(@".{8,}");

            return !string.IsNullOrEmpty(password) &&
                   hasNumber.IsMatch(password) &&
                   hasUpperChar.IsMatch(password) &&
                   hasLowerChar.IsMatch(password) &&
                   hasSpecialChar.IsMatch(password) &&
                   hasMinLength.IsMatch(password);
        }
    }
}
