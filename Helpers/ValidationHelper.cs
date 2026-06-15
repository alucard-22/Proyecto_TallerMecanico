using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Métodos de validación y formateo de texto para los formularios del sistema.
    /// </summary>
    public static class ValidationHelper
    {
        // ── Correo electrónico ────────────────────────────────────────────────

        /// <summary>
        /// Valida que el correo tenga formato válido (contiene @ y dominio con punto).
        /// Acepta cadena vacía/null (correo opcional).
        /// </summary>
        public static bool EsCorreoValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return true; // campo opcional

            // Debe tener exactamente un @, algo antes y dominio con punto después
            var regex = new Regex(
                @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                RegexOptions.IgnoreCase);

            return regex.IsMatch(correo.Trim());
        }

        /// <summary>
        /// Valida correo obligatorio (no puede estar vacío).
        /// </summary>
        public static bool EsCorreoObligatorioValido(string correo)
        {
            if (string.IsNullOrWhiteSpace(correo)) return false;
            return EsCorreoValido(correo);
        }

        // ── Teléfono ──────────────────────────────────────────────────────────

        /// <summary>
        /// Valida que el teléfono tenga al menos 6 dígitos y solo caracteres permitidos.
        /// </summary>
        public static bool EsTelefonoValido(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return false;
            var soloDigitos = Regex.Replace(telefono, @"[^\d]", "");
            return soloDigitos.Length >= 6;
        }

        // ── Capitalización de texto ───────────────────────────────────────────

        /// <summary>
        /// Convierte la primera letra de cada palabra a mayúscula (Title Case).
        /// Ej: "JUAN PABLO" → "Juan Pablo", "juan pablo" → "Juan Pablo"
        /// </summary>
        public static string AplicarTitleCase(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto ?? string.Empty;

            var ti = new CultureInfo("es-BO", false).TextInfo;
            return ti.ToTitleCase(texto.Trim().ToLower());
        }

        /// <summary>
        /// Convierte texto a mayúsculas (para placas, códigos, etc.).
        /// </summary>
        public static string AplicarMayusculas(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto ?? string.Empty;
            return texto.ToUpper().Trim();
        }

        /// <summary>
        /// Capitaliza solo la primera letra de la oración.
        /// Ej: "descripcion del trabajo" → "Descripcion del trabajo"
        /// </summary>
        public static string AplicarPrimeraLetraMayuscula(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return texto ?? string.Empty;
            texto = texto.Trim();
            return char.ToUpper(texto[0]) + texto.Substring(1).ToLower();
        }

        // ── Mensajes de error estandarizados ─────────────────────────────────

        public const string MsgCorreoInvalido =
            "El correo electrónico no tiene un formato válido.\n\n" +
            "Ejemplo correcto: usuario@dominio.com\n" +
            "Debe contener el símbolo @.";

        public const string MsgTelefonoInvalido =
            "El teléfono ingresado no es válido.\n" +
            "Debe tener al menos 6 dígitos.";

        public const string MsgCorreoRequerido =
            "El correo electrónico es obligatorio y debe tener formato válido.\n\n" +
            "Ejemplo: usuario@dominio.com";
    }
}
