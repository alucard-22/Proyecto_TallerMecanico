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

        // ── Placa vehicular (Bolivia) ────────────────────────────────────────

        /// <summary>
        /// Valida que la placa tenga un formato compatible con los esquemas
        /// usados en Bolivia:
        ///   - Formato clásico:  1234-ABC  (4 dígitos + 3 letras)
        ///   - Formato nuevo:    ABC-1234  (3 letras + 4 dígitos)
        ///   - Motos/variantes:  123-ABC   (3 dígitos + 3 letras)
        /// El guion es opcional y se aceptan espacios, que se eliminan antes
        /// de comparar. La comparación es insensible a mayúsculas/minúsculas.
        /// </summary>
        public static bool EsPlacaValida(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa)) return false;

            // Normalizar: quitar espacios y guiones para validar solo el patrón de caracteres
            var limpia = placa.Trim().ToUpper().Replace("-", "").Replace(" ", "");

            // 3 o 4 dígitos seguidos de 3 letras  (1234ABC / 123ABC)
            var formatoClasico = new Regex(@"^\d{3,4}[A-Z]{3}$");

            // 3 letras seguidas de 4 dígitos (ABC1234)
            var formatoNuevo = new Regex(@"^[A-Z]{3}\d{4}$");

            return formatoClasico.IsMatch(limpia) || formatoNuevo.IsMatch(limpia);
        }

        /// <summary>
        /// Da formato visual estándar a la placa: MAYÚSCULAS y con guion
        /// separando el bloque numérico del bloque alfabético.
        /// Ej: "1234abc" → "1234-ABC", "abc1234" → "ABC-1234"
        /// </summary>
        public static string FormatearPlaca(string placa)
        {
            if (string.IsNullOrWhiteSpace(placa)) return string.Empty;

            var limpia = placa.Trim().ToUpper().Replace("-", "").Replace(" ", "");

            var matchClasico = Regex.Match(limpia, @"^(\d{3,4})([A-Z]{3})$");
            if (matchClasico.Success)
                return $"{matchClasico.Groups[1].Value}-{matchClasico.Groups[2].Value}";

            var matchNuevo = Regex.Match(limpia, @"^([A-Z]{3})(\d{4})$");
            if (matchNuevo.Success)
                return $"{matchNuevo.Groups[1].Value}-{matchNuevo.Groups[2].Value}";

            // Si no coincide con ningún formato conocido, devolver tal cual en mayúsculas
            return limpia;
        }

        public const string MsgPlacaInvalida =
            "La placa ingresada no tiene un formato válido.\n\n" +
            "Formatos aceptados:\n" +
            "  • 1234-ABC  (formato clásico)\n" +
            "  • ABC-1234  (formato nuevo)\n" +
            "  • 123-ABC   (motocicletas)";

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