using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.Web;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Abre wa.me en el navegador predeterminado con un mensaje pre-escrito.
    /// No requiere instalar nada — solo el navegador del PC.
    /// </summary>
    public static class WhatsAppHelper
    {
        // ── Datos del taller (idealmente leerlos de ConfiguracionHelper en producción) ──
        private const string NombreTaller = "Taller El Choco";
        private const string DireccionTaller = "Av. América #1234, Cochabamba";
        private const string TelefonoTaller = "4-4567890";

        /// <summary>
        /// Envía confirmación de reserva CREADA.
        /// </summary>
        public static void EnviarConfirmacion(
            string telefonoCliente,
            string nombreCliente,
            DateTime fechaHoraCita,
            string tipoServicio,
            decimal? precioEstimado = null)
        {
            string mensaje =
                $"Hola {nombreCliente} 👋\n\n" +
                $"✅ *Su reserva en {NombreTaller} ha sido CONFIRMADA.*\n\n" +
                $"📅 *Fecha y hora:* {fechaHoraCita:dddd, dd/MM/yyyy} a las {fechaHoraCita:HH:mm}\n" +
                $"🔧 *Servicio:* {tipoServicio}\n" +
                (precioEstimado.HasValue ? $"💵 *Precio estimado:* Bs. {precioEstimado:N2}\n" : "") +
                $"\n📍 *{NombreTaller}*\n" +
                $"{DireccionTaller}\n" +
                $"📞 {TelefonoTaller}\n\n" +
                $"_Por favor llegue 10 minutos antes de su cita._\n" +
                $"_Si necesita cancelar o reprogramar, comuníquese con nosotros._";

            AbrirWhatsApp(telefonoCliente, mensaje);
        }

        /// <summary>
        /// Envía notificación de reserva CANCELADA.
        /// </summary>
        public static void EnviarCancelacion(
            string telefonoCliente,
            string nombreCliente,
            DateTime fechaHoraCita,
            string tipoServicio,
            string motivo = "")
        {
            string mensaje =
                $"Hola {nombreCliente},\n\n" +
                $"❌ *Su reserva en {NombreTaller} ha sido CANCELADA.*\n\n" +
                $"📅 *Fecha cancelada:* {fechaHoraCita:dd/MM/yyyy} a las {fechaHoraCita:HH:mm}\n" +
                $"🔧 *Servicio:* {tipoServicio}\n" +
                (string.IsNullOrWhiteSpace(motivo) ? "" : $"📋 *Motivo:* {motivo}\n") +
                $"\n📞 Para reagendar llámenos al {TelefonoTaller}\n" +
                $"📍 {NombreTaller} — {DireccionTaller}";

            AbrirWhatsApp(telefonoCliente, mensaje);
        }

        /// <summary>
        /// Envía recordatorio de cita (para el día anterior).
        /// </summary>
        public static void EnviarRecordatorio(
            string telefonoCliente,
            string nombreCliente,
            DateTime fechaHoraCita,
            string tipoServicio)
        {
            string mensaje =
                $"Hola {nombreCliente} 👋\n\n" +
                $"🔔 *Recordatorio de su cita en {NombreTaller}*\n\n" +
                $"📅 *Mañana {fechaHoraCita:dd/MM/yyyy}* a las *{fechaHoraCita:HH:mm}*\n" +
                $"🔧 *Servicio:* {tipoServicio}\n\n" +
                $"📍 {DireccionTaller}\n" +
                $"📞 {TelefonoTaller}\n\n" +
                $"_Le esperamos. Si necesita cancelar avísenos con anticipación._";

            AbrirWhatsApp(telefonoCliente, mensaje);
        }

        // ── Core ────────────────────────────────────────────────

        private static void AbrirWhatsApp(string telefono, string mensaje)
        {
            // Limpiar el número: solo dígitos
            string numero = LimpiarNumero(telefono);
            if (string.IsNullOrWhiteSpace(numero)) return;

            // Codificar el mensaje para URL
            string mensajeCodificado = Uri.EscapeDataString(mensaje);
            string url = $"https://wa.me/{numero}?text={mensajeCodificado}";

            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        /// <summary>
        /// Limpia el número: deja solo dígitos y agrega prefijo boliviano (+591) si falta.
        /// </summary>
        private static string LimpiarNumero(string telefono)
        {
            if (string.IsNullOrWhiteSpace(telefono)) return "";

            // Quitar todo excepto dígitos y el signo +
            var soloDigitos = "";
            foreach (char c in telefono)
                if (char.IsDigit(c)) soloDigitos += c;

            if (string.IsNullOrEmpty(soloDigitos)) return "";

            // Si el número boliviano tiene 8 dígitos (ej: 70123456), agregar 591
            if (soloDigitos.Length == 8)
                return "591" + soloDigitos;

            // Si ya empieza con 591
            if (soloDigitos.StartsWith("591"))
                return soloDigitos;

            // Devolver tal cual (puede ser número con código de país ya incluido)
            return soloDigitos;
        }
    }
}