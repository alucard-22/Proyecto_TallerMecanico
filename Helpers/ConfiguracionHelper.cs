using System;
using System.IO;
using System.Text.Json;

namespace Proyecto_taller.Helpers
{
    /// <summary>
    /// Clase para gestionar la configuración del sistema
    /// </summary>
    public class ConfiguracionHelper
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TallerElChoco",
            "configuracion.json");

        /// <summary>
        /// Guarda la configuración en un archivo JSON
        /// </summary>
        public static void GuardarConfiguracion(ConfiguracionModel config)
        {
            try
            {
                // Crear directorio si no existe
                var directorio = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directorio))
                {
                    Directory.CreateDirectory(directorio);
                }

                // Serializar y guardar
                var opciones = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(config, opciones);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar configuración: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga la configuración desde el archivo JSON
        /// </summary>
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
                else
                {
                    // Si no existe, crear configuración por defecto
                    var configDefault = new ConfiguracionModel();
                    GuardarConfiguracion(configDefault);
                    return configDefault;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al cargar configuración: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la ruta del archivo de configuración
        /// </summary>
        public static string ObtenerRutaConfiguracion()
        {
            return ConfigPath;
        }
    }

    /// <summary>
    /// Modelo de datos para la configuración del sistema
    /// </summary>
    public class ConfiguracionModel
    {
        // Información del Taller
        public string NombreTaller { get; set; } = "Taller Mecánico El Choco";
        public string DireccionTaller { get; set; } = "Av. América #1234, Cochabamba";
        public string TelefonoTaller { get; set; } = "4-4567890";
        public string EmailTaller { get; set; } = "contacto@tallerelchoco.com";
        public string NITTaller { get; set; } = "123456789";

        // Configuración de Facturación
        public decimal PorcentajeIVA { get; set; } = 13;
        public decimal DescuentoMaximo { get; set; } = 20;
        public bool IncluirIVAAutomatico { get; set; } = true;
        public bool SolicitarNIT { get; set; } = false;

        // Base de Datos
        public string ConnectionString { get; set; } =
            "Server=localhost;Database=TallerMecanico;Trusted_Connection=True;TrustServerCertificate=True;";
        public DateTime UltimoRespaldo { get; set; } = DateTime.Now.AddDays(-7);
    }
}