using HybridADManager.Models;

namespace HybridADManager.Infrastructure.Helpers;

public static class CsvExportHelper
{
    public static string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";
        if (field.Contains('"') || field.Contains(',') || field.Contains('\n') || field.Contains('\r'))
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        return field;
    }

    public static async Task ExportObjectsAsync(IEnumerable<DirectoryObject> objects, string filePath)
    {
        var lines = new List<string>
        {
            "Name,ObjectClass,DisplayName,UPN,Email,Description,Department,Company,JobTitle,Office,Telephone,SyncStatus,EntraObjectId"
        };

        foreach (var obj in objects)
        {
            var dept = obj is HybridUser u ? u.Department : "";
            var company = obj is HybridUser usr ? usr.Company : "";
            var jobTitle = obj is HybridUser user ? user.JobTitle : "";
            var office = obj is HybridUser us ? us.Office : "";
            var phone = obj is HybridUser us2 ? us2.Telephone : "";

            var line = string.Join(",",
                EscapeCsvField(obj.DisplayName),
                EscapeCsvField(obj.ObjectClass),
                EscapeCsvField(obj.DisplayName),
                EscapeCsvField(obj.Upn),
                EscapeCsvField(obj.Email),
                EscapeCsvField(obj.Description),
                EscapeCsvField(dept),
                EscapeCsvField(company),
                EscapeCsvField(jobTitle),
                EscapeCsvField(office),
                EscapeCsvField(phone),
                EscapeCsvField(obj.SyncStatus?.Status.ToString() ?? ""),
                EscapeCsvField(obj.EntraObjectId ?? "")
            );
            lines.Add(line);
        }

        await File.WriteAllLinesAsync(filePath, lines);
    }
}
