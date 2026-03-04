using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RentasApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Propietario")]
public class BackupsController : ControllerBase
{
    private readonly string _dbPath;
    private readonly string _backupDir;

    public BackupsController(IConfiguration config)
    {
        _dbPath = Environment.GetEnvironmentVariable("DB_PATH") 
            ?? Path.Combine(Directory.GetCurrentDirectory(), "..", "rentas.db");
        _backupDir = Path.Combine(Path.GetDirectoryName(_dbPath)!, "backups");
        
        if (!Directory.Exists(_backupDir))
            Directory.CreateDirectory(_backupDir);
    }

    [HttpGet]
    public IActionResult List()
    {
        var files = Directory.GetFiles(_backupDir, "*.db")
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.CreationTime)
            .Select(f => new 
            {
                filename = f.Name,
                date = f.CreationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                size = FormatSize(f.Length)
            })
            .ToList();

        return Ok(files);
    }

    [HttpPost]
    public IActionResult Create()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var backupFile = Path.Combine(_backupDir, $"rentas-{timestamp}.db");
            
            System.IO.File.Copy(_dbPath, backupFile, overwrite: true);
            
            return Ok(new { message = "Backup creado", filename = Path.GetFileName(backupFile) });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al crear backup: {ex.Message}" });
        }
    }

    [HttpGet("download/{filename}")]
    public IActionResult Download(string filename)
    {
        // Sanitize filename
        filename = Path.GetFileName(filename);
        var filePath = Path.Combine(_backupDir, filename);
        
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Backup no encontrado" });

        var bytes = System.IO.File.ReadAllBytes(filePath);
        return File(bytes, "application/octet-stream", filename);
    }

    [HttpPost("restore/{filename}")]
    public IActionResult Restore(string filename)
    {
        try
        {
            // Sanitize filename
            filename = Path.GetFileName(filename);
            var backupFile = Path.Combine(_backupDir, filename);
            
            if (!System.IO.File.Exists(backupFile))
                return NotFound(new { message = "Backup no encontrado" });

            // Create a backup of current DB before restoring
            var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var preRestoreBackup = Path.Combine(_backupDir, $"pre-restore-{timestamp}.db");
            System.IO.File.Copy(_dbPath, preRestoreBackup, overwrite: true);

            // Restore the backup
            System.IO.File.Copy(backupFile, _dbPath, overwrite: true);
            
            return Ok(new { message = "Backup restaurado exitosamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al restaurar: {ex.Message}" });
        }
    }

    [HttpDelete("{filename}")]
    public IActionResult Delete(string filename)
    {
        try
        {
            // Sanitize filename
            filename = Path.GetFileName(filename);
            var filePath = Path.Combine(_backupDir, filename);
            
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { message = "Backup no encontrado" });

            System.IO.File.Delete(filePath);
            return Ok(new { message = "Backup eliminado" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error al eliminar: {ex.Message}" });
        }
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
