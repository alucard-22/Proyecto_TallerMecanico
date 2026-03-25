using System;
using System.IO;
using System.Text.Json;

namespace Proyecto_taller.Helpers
{
    public class ConfiguracionHelper
    {
        // JSON de configuración del taller (preferencias de UI, datos del negocio)
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TallerElChoco",
            "configuracion.json");

        // appsettings.json junto al ejecutable (cadena de conexión BD)
        private static readonly string AppSettingsPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "appsettings.json");

        // ── Config del taller ─────────────────────────────────────────────────

        public static void GuardarConfiguracion(ConfiguracionModel config)
        {
            try
            {
                var dir = Path.GetDirectoryName(ConfigPath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(config,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar configuración: {ex.Message}");
            }
        }

        public static ConfiguracionModel CargarConfiguracion()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<ConfiguracionModel>(json)
                           ?? new ConfiguracionModel();
                }

                // Primera ejecución: crear con valores por defecto
                var defaults = new ConfiguracionModel();
                GuardarConfiguracion(defaults);
                return defaults;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar configuración: {ex.Message}");
            }
        }

        public static string ObtenerRutaConfiguracion() => ConfigPath;

        // ── Cadena de conexión en appsettings.json ────────────────────────────
        // FIX: antes los cambios de cadena solo se guardaban en el JSON de AppData
        // y no afectaban appsettings.json, por lo que TallerDbContext nunca los leía.

        public static void GuardarConnectionString(string connectionString)
        {
            try
            {
                // Leer el appsettings.json actual si existe
                AppSettingsModel settings;
                if (File.Exists(AppSettingsPath))
                {
                    var contenido = File.ReadAllText(AppSettingsPath);
                    settings = JsonSerializer.Deserialize<AppSettingsModel>(contenido)
                               ?? new AppSettingsModel();
                }
                else
                {
                    settings = new AppSettingsModel();
                }

                settings.ConnectionStrings ??= new();
                settings.ConnectionStrings["TallerDB"] = connectionString;

                var nuevoJson = JsonSerializer.Serialize(settings,
                    new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(AppSettingsPath, nuevoJson);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Error al actualizar appsettings.json:\n{ex.Message}\n\n" +
                    $"Ruta: {AppSettingsPath}");
            }
        }

        public static string LeerConnectionStringActual()
        {
            try
            {
                if (!File.Exists(AppSettingsPath)) return string.Empty;
                var json = File.ReadAllText(AppSettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettingsModel>(json);
                return settings?.ConnectionStrings?.GetValueOrDefault("TallerDB") ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    // ── Modelos ───────────────────────────────────────────────────────────────

    public class ConfiguracionModel
    {
        // Información del taller
        public string NombreTaller { get; set; } = "Taller Mecánico El Choco";
        public string DireccionTaller { get; set; } = "Av. América #1234, Cochabamba";
        public string TelefonoTaller { get; set; } = "4-4567890";
        public string EmailTaller { get; set; } = "contacto@tallerelchoco.com";
        public string NITTaller { get; set; } = "123456789";

        // Facturación
        public decimal PorcentajeIVA { get; set; } = 13;
        public decimal DescuentoMaximo { get; set; } = 20;
        public bool IncluirIVAAutomatico { get; set; } = true;
        public bool SolicitarNIT { get; set; } = false;

        // Base de datos (solo se guarda como referencia, el real está en appsettings.json)
        public string ConnectionString { get; set; } =
            "Server=localhost;Database=TallerMecanico;Trusted_Connection=True;TrustServerCertificate=True;";
        public DateTime UltimoRespaldo { get; set; } = DateTime.Now.AddDays(-7);
    }

    public class AppSettingsModel
    {
        public Dictionary<string, string>? ConnectionStrings { get; set; }
    }
}