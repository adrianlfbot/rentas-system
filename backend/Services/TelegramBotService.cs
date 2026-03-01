using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RentasApi.Data;
using RentasApi.Models;

namespace RentasApi.Services;

public class TelegramBotService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly HttpClient _http;
    private readonly string _token = "8672511933:AAEKCEGhv0kTZXqC82-C7M6ToqPq94v6WeU";
    private int _offset = 0;

    public TelegramBotService(IServiceProvider serviceProvider, ILogger<TelegramBotService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _http = new HttpClient { 
            BaseAddress = new Uri($"https://api.telegram.org/bot{_token}/"),
            Timeout = TimeSpan.FromMinutes(10) // Timeout largo
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 Bot iniciado (Modo HttpClient - Polling Robusto)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await GetUpdatesAsync(stoppingToken);
                foreach (var update in updates)
                {
                    _offset = update.UpdateId + 1;
                    if (update.Message?.Text != null)
                    {
                        await ProcessMessageAsync(update.Message, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error polling Telegram: {ex.Message}");
                await Task.Delay(5000, stoppingToken);
            }

            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task<List<Update>> GetUpdatesAsync(CancellationToken ct)
    {
        try
        {
            // Timeout corto en la URL para evitar bloqueos de red silenciosos
            var response = await _http.GetAsync($"getUpdates?offset={_offset}&timeout=10", ct);
            
            if (!response.IsSuccessStatusCode) 
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogError($"[Telegram Error] {response.StatusCode}: {error}");
                return new List<Update>();
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<TelegramResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Result ?? new List<Update>();
        }
        catch (Exception ex) 
        { 
            _logger.LogError($"[Telegram Exception] {ex.Message}");
            return new List<Update>(); 
        }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        var chatId = message.Chat.Id;
        var text = message.Text;
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower();

        _logger.LogInformation($"📩 Comando recibido: {command} de {chatId}");

        string reply = "";

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<RentasContext>();

            switch (command)
            {
                case "/start":
                case "/ayuda":
                    reply = "🏠 *Sistema de Rentas*\n\n" +
                            "📝 `/nota [Clave] [Texto]` - Agregar nota\n" +
                            "💰 `/pagar [Clave] [Monto]` - Registrar pago\n" +
                            "📊 `/resumen` - Ver total del mes";
                    break;

                case "/resumen":
                    var periodo = DateTime.Now.ToString("yyyy-MM");
                    var total = await db.Cobranza.Where(c => c.Periodo == periodo).SumAsync(c => c.Monto);
                    reply = $"📊 *Resumen {periodo}*\n💰 Total: ${total:N2}";
                    break;

                case "/nota":
                    if (parts.Length < 3) { reply = "❌ Uso: `/nota [Clave] [Texto]`"; break; }
                    var depto = await db.Departamentos.FirstOrDefaultAsync(d => d.Clave == parts[1]);
                    if (depto == null) { reply = "❌ Depto no encontrado"; break; }
                    db.NotasDepartamento.Add(new NotaDepartamento { 
                        DepartamentoId = depto.ID, 
                        Texto = string.Join(" ", parts.Skip(2)),
                        UsuarioCreo = "@TelegramUser"
                    });
                    await db.SaveChangesAsync();
                    reply = $"✅ Nota agregada a {depto.Clave}";
                    break;
                
                case "/pagar":
                    if (parts.Length < 3) { reply = "❌ Uso: `/pagar [Clave] [Monto]`"; break; }
                    var deptoP = await db.Departamentos.FirstOrDefaultAsync(d => d.Clave == parts[1]);
                    if (deptoP == null) { reply = "❌ Depto no encontrado"; break; }
                    if (!double.TryParse(parts[2], out double monto)) { reply = "❌ Monto inválido"; break; }
                    
                    db.Cobranza.Add(new Cobranza {
                        IDUbicacion = deptoP.IDUbicacion,
                        ClaveDepartamento = deptoP.Clave,
                        Periodo = DateTime.Now.ToString("yyyy-MM"),
                        Monto = monto,
                        FechaCobro = DateTime.Now,
                        Medio = "Telegram"
                    });
                    await db.SaveChangesAsync();
                    reply = $"✅ Pago de ${monto} registrado para {deptoP.Clave}";
                    break;

                default:
                    reply = "❓ Comando no reconocido.";
                    break;
            }
        }

        await SendMessageAsync(chatId, reply, ct);
    }

    private async Task SendMessageAsync(long chatId, string text, CancellationToken ct)
    {
        var payload = new { chat_id = chatId, text = text, parse_mode = "Markdown" };
        await _http.PostAsJsonAsync("sendMessage", payload, ct);
    }

    public class TelegramResponse { public bool Ok { get; set; } public List<Update> Result { get; set; } }
    public class Update { [JsonPropertyName("update_id")] public int UpdateId { get; set; } public Message Message { get; set; } }
    public class Message { public Chat Chat { get; set; } public string Text { get; set; } }
    public class Chat { public long Id { get; set; } }
}
