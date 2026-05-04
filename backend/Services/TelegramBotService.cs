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
    private readonly string _token = "8704610750:AAHbIu5qzoYqFubWctRHfUFPSFaeAeLg_Eg";
    private readonly HashSet<long> _autorizados = new() { 395860686, 6557501745 };
    private int _offset = 0;

    public TelegramBotService(IServiceProvider serviceProvider, ILogger<TelegramBotService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _http = new HttpClient
        {
            BaseAddress = new Uri($"https://api.telegram.org/bot{_token}/"),
            Timeout = TimeSpan.FromMinutes(2)
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 Bot de Rentas iniciado");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await GetUpdatesAsync(stoppingToken);
                foreach (var update in updates)
                {
                    _offset = update.UpdateId + 1;
                    if (update.Message?.Text != null)
                        await ProcessMessageAsync(update.Message, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error polling: {ex.Message}");
                await Task.Delay(5000, stoppingToken);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }

    private async Task<List<Update>> GetUpdatesAsync(CancellationToken ct)
    {
        try
        {
            var response = await _http.GetAsync($"getUpdates?offset={_offset}&timeout=10", ct);
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<TelegramResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return result?.Result ?? new();
        }
        catch { return new(); }
    }

    private async Task ProcessMessageAsync(Message message, CancellationToken ct)
    {
        var chatId = message.Chat.Id;

        // Verificar autorización
        if (!_autorizados.Contains(chatId))
        {
            await SendMessageAsync(chatId, "⛔ No tienes acceso a este bot.", ct);
            return;
        }

        var text = message.Text.Trim();
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower().Split('@')[0]; // quitar @botname si viene
        string reply;

        _logger.LogInformation($"📩 [{chatId}] {text}");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<RentasContext>();

        try
        {
            reply = command switch
            {
                "/start" or "/ayuda" => AyudaTexto(),
                "/resumen"           => await ResumenAsync(db),
                "/deptos"            => await DeptosAsync(db),
                "/depto"             => await DeptoAsync(db, parts),
                "/deudas"            => await DeudasAsync(db),
                "/tickets"           => await TicketsAsync(db),
                "/gastos"            => await GastosAsync(db),
                "/pagar"             => await PagarAsync(db, parts),
                "/nota"              => await NotaAsync(db, parts),
                "/ticket"            => await CrearTicketAsync(db, parts, chatId.ToString()),
                _                    => "❓ Comando no reconocido. Escribe /ayuda para ver los comandos disponibles."
            };
        }
        catch (Exception ex)
        {
            reply = $"❌ Error: {ex.Message}";
            _logger.LogError($"Error procesando comando {command}: {ex.Message}");
        }

        await SendMessageAsync(chatId, reply, ct);
    }

    // ─── COMANDOS ────────────────────────────────────────────────────────────

    private static string AyudaTexto() => """
        🏠 *Sistema de Rentas - Comandos*

        📋 *Consultas*
        /deptos — Lista departamentos con inquilino, teléfono y renta
        /depto \[clave\] — Info detallada de un departamento
        /resumen — Ingresos cobrados y por cobrar del mes
        /deudas — Departamentos sin pago registrado este mes
        /tickets — Tickets abiertos
        /gastos — Gastos registrados este mes

        ✏️ *Acciones*
        /pagar \[clave\] \[monto\] \[medio\] — Registrar pago
        /nota \[clave\] \[texto\] — Agregar nota a un depto
        /ticket \[prioridad\] \[descripcion\] — Crear ticket \(Alta/Media/Baja\)
        """;

    private static async Task<string> ResumenAsync(RentasContext db)
    {
        var periodo = DateTime.Now.ToString("yyyy-MM");
        var cobros = await db.Cobranza.Where(c => c.Periodo == periodo).ToListAsync();
        var cobrado = cobros.Where(c => c.FechaCobro != null).Sum(c => c.Monto);
        var porCobrar = cobros.Where(c => c.FechaCobro == null).Sum(c => c.Monto);
        var totalDeptos = await db.Departamentos.CountAsync();
        return $"""
            📊 *Resumen {periodo}*

            ✅ Cobrado: ${cobrado:N2}
            ⏳ Por cobrar: ${porCobrar:N2}
            💰 Total registrado: ${cobrado + porCobrar:N2}
            🏢 Total departamentos: {totalDeptos}
            """;
    }

    private static async Task<string> DeptosAsync(RentasContext db)
    {
        var deptos = await db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.Inquilino)
            .OrderBy(d => d.Ubicacion!.Calle).ThenBy(d => d.Clave)
            .ToListAsync();

        if (!deptos.Any()) return "📭 No hay departamentos registrados.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("🏢 *Departamentos*\n");
        string? ubiActual = null;
        foreach (var d in deptos)
        {
            var ubi = $"{d.Ubicacion?.Calle} {d.Ubicacion?.Numero}";
            if (ubi != ubiActual) { sb.AppendLine($"\n📍 *{ubi}*"); ubiActual = ubi; }
            var inquilino = d.InquilinoCorreo != null
                ? $"{d.InquilinoCorreo}{(d.Inquilino?.Telefono != null ? $" | 📞{d.Inquilino.Telefono}" : "")}"
                : "Vacío";
            var renta = d.MontoRenta > 0 ? $"${d.MontoRenta:N0}" : "—";
            sb.AppendLine($"  *{d.Clave}* — {inquilino} — {renta}");
        }
        return sb.ToString();
    }

    private static async Task<string> DeptoAsync(RentasContext db, string[] parts)
    {
        if (parts.Length < 2) return "❌ Uso: /depto \\[clave\\]";
        var clave = parts[1];
        var d = await db.Departamentos
            .Include(d => d.Ubicacion)
            .Include(d => d.Inquilino)
            .Include(d => d.ContratoLuz)
            .FirstOrDefaultAsync(d => d.Clave == clave);
        if (d == null) return $"❌ Departamento '{clave}' no encontrado.";

        var periodo = DateTime.Now.ToString("yyyy-MM");
        var pagos = await db.Cobranza.Where(c => c.ClaveDepartamento == clave && c.Periodo == periodo).ToListAsync();
        var pagado = pagos.Sum(c => c.Monto);

        return $"""
            🏠 *Depto {d.Clave}*
            📍 {d.Ubicacion?.Calle} {d.Ubicacion?.Numero}
            📝 {d.Descripcion ?? "—"}
            👤 {d.InquilinoCorreo ?? "Vacío"}
            📞 {d.Inquilino?.Telefono ?? "—"}
            💰 Renta: ${d.MontoRenta:N0}
            💧 Agua: ${d.CuotaAgua:N0}
            📅 Día cobro: {d.DiaVencimiento}
            ⚡ Contrato luz: {d.ContratoLuz?.Nombre ?? "—"}
            💳 Pagado este mes: ${pagado:N2}
            """;
    }

    private static async Task<string> DeudasAsync(RentasContext db)
    {
        var periodo = DateTime.Now.ToString("yyyy-MM");
        var deptos = await db.Departamentos
            .Include(d => d.Ubicacion)
            .Where(d => d.InquilinoCorreo != null && d.MontoRenta > 0)
            .ToListAsync();

        var conPago = await db.Cobranza
            .Where(c => c.Periodo == periodo && c.FechaCobro != null)
            .Select(c => c.ClaveDepartamento)
            .ToListAsync();

        var sinPago = deptos.Where(d => !conPago.Contains(d.Clave)).ToList();
        if (!sinPago.Any()) return $"✅ Todos los departamentos tienen pago registrado en {periodo}.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"⚠️ *Sin pago en {periodo}*\n");
        foreach (var d in sinPago.OrderBy(d => d.Ubicacion?.Calle).ThenBy(d => d.Clave))
            sb.AppendLine($"• {d.Ubicacion?.Calle} {d.Ubicacion?.Numero} — *{d.Clave}* — ${d.MontoRenta:N0}");

        var total = sinPago.Sum(d => d.MontoRenta);
        sb.AppendLine($"\n💰 Total pendiente: ${total:N0}");
        return sb.ToString();
    }

    private static async Task<string> TicketsAsync(RentasContext db)
    {
        var tickets = await db.Tickets
            .Where(t => t.Estado != "Cerrado")
            .OrderBy(t => t.Prioridad == "Alta" ? 0 : t.Prioridad == "Media" ? 1 : 2)
            .ThenBy(t => t.FechaCreacion)
            .ToListAsync();

        if (!tickets.Any()) return "✅ No hay tickets abiertos.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"🎫 *Tickets abiertos ({tickets.Count})*\n");
        foreach (var t in tickets)
        {
            var icon = t.Prioridad == "Alta" ? "🔴" : t.Prioridad == "Media" ? "🟡" : "🟢";
            sb.AppendLine($"{icon} *#{t.ID}* [{t.Estado}] {t.Descripcion}");
            sb.AppendLine($"   👤 {t.UsuarioCreo} — {t.FechaCreacion:dd/MM/yyyy}");
        }
        return sb.ToString();
    }

    private static async Task<string> GastosAsync(RentasContext db)
    {
        var inicio = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var fin = inicio.AddMonths(1);
        var gastos = await db.Gastos
            .Include(g => g.Departamento).ThenInclude(d => d!.Ubicacion)
            .Where(g => g.Fecha >= inicio && g.Fecha < fin)
            .OrderBy(g => g.Fecha)
            .ToListAsync();

        if (!gastos.Any()) return $"📭 No hay gastos registrados en {inicio:MMMM yyyy}.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"💸 *Gastos {inicio:MMMM yyyy}*\n");
        foreach (var g in gastos)
        {
            var total = g.ManoDeObra + g.Material;
            sb.AppendLine($"• {g.Departamento?.Ubicacion?.Calle} *{g.Departamento?.Clave}* — {g.Descripcion ?? "Sin desc."} — ${total:N0}");
        }
        var totalGastos = gastos.Sum(g => g.ManoDeObra + g.Material);
        sb.AppendLine($"\n💰 Total: ${totalGastos:N0}");
        return sb.ToString();
    }

    private static async Task<string> PagarAsync(RentasContext db, string[] parts)
    {
        if (parts.Length < 3) return "❌ Uso: /pagar \\[clave\\] \\[monto\\] \\[medio opcional\\]";
        var clave = parts[1];
        if (!double.TryParse(parts[2], out double monto)) return "❌ Monto inválido.";
        var medio = parts.Length > 3 ? string.Join(" ", parts.Skip(3)) : "Telegram";

        var depto = await db.Departamentos
            .Include(d => d.Ubicacion)
            .FirstOrDefaultAsync(d => d.Clave == clave);
        if (depto == null) return $"❌ Departamento '{clave}' no encontrado.";

        db.Cobranza.Add(new Cobranza
        {
            IDUbicacion = depto.IDUbicacion,
            ClaveDepartamento = depto.Clave,
            Periodo = DateTime.Now.ToString("yyyy-MM"),
            Monto = monto,
            FechaCobro = DateTime.Now,
            Medio = medio
        });
        await db.SaveChangesAsync();
        return $"✅ Pago registrado\n🏠 Depto: *{clave}*\n💰 Monto: ${monto:N2}\n💳 Medio: {medio}\n📅 {DateTime.Now:dd/MM/yyyy HH:mm}";
    }

    private static async Task<string> NotaAsync(RentasContext db, string[] parts)
    {
        if (parts.Length < 3) return "❌ Uso: /nota \\[clave\\] \\[texto\\]";
        var clave = parts[1];
        var texto = string.Join(" ", parts.Skip(2));

        var depto = await db.Departamentos.FirstOrDefaultAsync(d => d.Clave == clave);
        if (depto == null) return $"❌ Departamento '{clave}' no encontrado.";

        db.NotasDepartamento.Add(new NotaDepartamento
        {
            DepartamentoId = depto.ID,
            Texto = texto,
            UsuarioCreo = "@TelegramBot"
        });
        await db.SaveChangesAsync();
        return $"✅ Nota agregada al depto *{clave}*:\n_{texto}_";
    }

    private static async Task<string> CrearTicketAsync(RentasContext db, string[] parts, string chatId)
    {
        if (parts.Length < 3) return "❌ Uso: /ticket \\[Alta/Media/Baja\\] \\[descripcion\\]";
        var prioridad = parts[1];
        if (!new[] { "Alta", "Media", "Baja" }.Contains(prioridad, StringComparer.OrdinalIgnoreCase))
            return "❌ Prioridad debe ser: Alta, Media o Baja";
        var descripcion = string.Join(" ", parts.Skip(2));

        var ticket = new Ticket
        {
            Prioridad = char.ToUpper(prioridad[0]) + prioridad[1..].ToLower(),
            Descripcion = descripcion,
            UsuarioCreo = $"Telegram:{chatId}",
            Estado = "Abierto",
            FechaCreacion = DateTime.UtcNow
        };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return $"✅ Ticket creado\n🎫 *#{ticket.ID}*\n{(ticket.Prioridad == "Alta" ? "🔴" : ticket.Prioridad == "Media" ? "🟡" : "🟢")} Prioridad: {ticket.Prioridad}\n📝 {descripcion}";
    }

    // ─── HELPERS ─────────────────────────────────────────────────────────────

    private async Task SendMessageAsync(long chatId, string text, CancellationToken ct)
    {
        try
        {
            // Telegram tiene límite de 4096 chars por mensaje
            if (text.Length > 4000)
                text = text[..4000] + "\n\n_(mensaje truncado)_";

            var payload = new { chat_id = chatId, text, parse_mode = "Markdown" };
            await _http.PostAsJsonAsync("sendMessage", payload, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error enviando mensaje: {ex.Message}");
        }
    }

    // ─── MODELOS TELEGRAM ─────────────────────────────────────────────────────

    public class TelegramResponse { public bool Ok { get; set; } public List<Update> Result { get; set; } = new(); }
    public class Update { [JsonPropertyName("update_id")] public int UpdateId { get; set; } public Message? Message { get; set; } }
    public class Message { public Chat Chat { get; set; } = new(); public string? Text { get; set; } }
    public class Chat { public long Id { get; set; } }
}
