using System.Security.Cryptography;
using System.Text;

namespace Infinity.Web.Helpers
{
    public static class Cripto
    {
        /// <summary>
        /// Gera o hash SHA256 (hexadecimal minúsculo) de uma senha.
        /// Deve ser o mesmo algoritmo usado no script SQL de seed (ADMIN/admin).
        /// </summary>
        public static string HashSha256(string valor)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(valor));
            var sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
