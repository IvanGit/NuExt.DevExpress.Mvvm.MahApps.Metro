using DevExpress.Mvvm;
using MetroWpfApp.Interfaces.Services;
using Minimal.Mvvm;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MetroWpfApp.Services
{
    internal sealed class EnvironmentService(string baseDirectory, params string[] args)
        : EnvironmentServiceBase(baseDirectory, Path.Combine(baseDirectory, "AppData"), args), IEnvironmentService;

    internal static class EnvironmentServiceExtensions
    {
        public static void LoadLocalization(this IEnvironmentService environmentService, Type type, string culture)
        {
            var sb = new ValueStringBuilder(16);
            sb.Append($"local.{culture}.json");
            var langFilePath = Path.Combine(environmentService.ConfigDirectory, sb.ToString());
            //Debug.Assert(File.Exists(langFilePath), $"File doesn't exist: {langFilePath}");
            if (!File.Exists(langFilePath)) return;
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(langFilePath));
            if (translations.IsNullOrEmpty())
            {
                return;
            }
            translations = translations.ToDictionary(pair => LocalizeAttribute.StringToValidPropertyName(pair.Key), pair => pair.Value);
            var props = type.GetProperties();
            foreach (var prop in props)
            {
                Debug.Assert(!prop.CanWrite || translations.ContainsKey(prop.Name), $"Can't find translation for {prop.Name}");
                if (prop.CanWrite && translations.TryGetValue(prop.Name, out var text))
                {
                    prop.SetValue(null, text);
                }
            }
        }
    }
}
