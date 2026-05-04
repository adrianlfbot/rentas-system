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
    private readonly string _token = "8704610750:AAHbIu5qzoYqFubWctRHfUFPSFaeAeLg_Eg";
    private readonly string _apiBase;
    private readonly HashSet<long> _autorizados = new() { 395860686, 6557501745 };
    private int _offset = 0;

    public TelegramBotService(IServiceProvider serviceProvider, ILogger<TelegramBotService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _apiBase = $"https://api.telegram.org/bot{_token}";
    }

    // Forzar IPv4 — la red del servidor no soporta IPv6 hacia api.telegram.org
    private static HttpClient MakeClient()
    {
        var handler = new System.Net.Http.SocketsHttpHandler();
        handler.ConnectCallback = async (ctx, ct) =>
        {
            var socket = new System.Net.Sockets.Socket(
                System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Stream,
                System.Net.Sockets.ProtocolType.Tcp);
            socket.NoDelay = true;
            await socket.ConnectAsync(ctx.DnsEndPoint.Host, ctx.DnsEndPoint.Port, ct);
            return new System.Net.Sockets.NetworkStream(socket, ownsSocket: true);
        };
        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _ = Task.Run(() => PollLoop(stoppingToken), CancellationToken.None);
        return Task.CompletedTask;
    }

    private async Task PollLoop(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🤖 Bot de Rentas iniciado");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await GetUpdatesAsync();
                foreach (var update in updates)
                {
                    _offset = update.UpdateId + 1;
                    if (update.Message?.Text != null)
                        _ = Task.Run(() => ProcessMessageAsync(update.Message));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en poll loop: {ex.Message}");
            }
            await Task.Delay(2000);
        }
    }

    private async Task<List<Update>> GetUpdatesAsync()
    {
        using var http = MakeClient();
        try
        {
            var url = $"{_apiBase}/getUpdates?offset={_offset}&timeout=0";
            var response = await http.GetAsync(url).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode) return new();
            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<TelegramResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            var updates = result?.Result ?? new();
            if (updates.Count > 0)
                _logger.LogInformation($"[Telegram] {updates.Count} mensaje(s) recibidos");
            return updates;
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"[Telegram] GetUpdates: {ex.Message}");
            return new();
        }
    }

    private async Task SendAsync(long chatId, string text)
    {
        using var http = MakeClient();
        try
        {
            if (text.Length > 4000) text = text[..4000] + "\n_(truncado)_";
            var url = $"{_apiBase}/sendMessage";
            var payload = new { chat_id = chatId, text, parse_mode = "Markdown" };
            await http.PostAsJsonAsync(url, payload).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError($"[Telegram] SendMessage: {ex.Message}");
        }
    }

    private async Task ProcessMessageAsync(Message message)
    {
        var chatId = message.Chat.Id;
        if (!_autorizados.Contains(chatId))
        {
            await SendAsync(chatId, "⛔ No tienes acceso a este bot.");
            return;
        }

        var parts = message.Text!.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var command = parts[0].ToLower().Split('@')[0];
        _logger.LogInformation($"📩 [{chatId}] {message.Text}");

        string reply;
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
                _                    => "❓ Comando no reconocido\\. Escribe /ayuda"
            };
        }
        catch (Exception ex)
        {
            reply = $"❌ Error: {ex.Message}";
            _logger.LogError($"Error en comando {command}: {ex.Message}");
        }

        await SendAsync(chatId, reply);
    }

    // ─── COMANDOS ────────────────────────────────────────────────────────────

    private static string AyudaTexto() =>
        "🏠 *Sistema de Rentas \\- Comandos*\n\n" +
        "📋 *Consultas*\n" +
        "/deptos \\— Lista departamentos con inquilino, teléfono y renta\n" +
        "/depto \\[clave\\] \\— Info detallada de un departamento\n" +
        "/resumen \\— Ingresos cobrados y por cobrar del mes\n" +
        "/deudas \\— Departamentos sin pago este mes\n" +
        "/tickets \\— Tickets abiertos\n" +
        "/gastos \\— Gastos del mes\n\n" +
        "✏️ *Acciones*\n" +
        "/pagar \\[clave\\] \\[monto\\] \\[medio\\] \\— Registrar pago\n" +
        "/nota \\[clave\\] \\[texto\\] \\— Agregar nota\n" +
        "/ticket \\[Alta/Media/Baja\\] \\[desc\\] \\— Crear ticket\n\n" +
        "📌 *Prefijos de ubicación*\n" +
        "Agrega la primera letra de la ubicación antes de la clave:\n" +
        "C1 = Chalchihuecan 1 | S3 = Salchipulpos 3\n" +
        "F2 = Fragua 2 | D1 = Dos de Abril 1";

    private static async Task<string> ResumenAsync(RentasContext db)
    {
        var periodo = DateTime.Now.ToString("yyyy-MM");
        var cobros = await db.Cobranza.Where(c => c.Periodo == periodo).ToListAsync();
        var cobrado = cobros.Where(c => c.FechaCobro != null).Sum(c => c.Monto);
        var porCobrar = cobros.Where(c => c.FechaCobro == null).Sum(c => c.Monto);
        var totalDeptos = await db.Departamentos.CountAsync();
        return $"📊 *Resumen {periodo}*\n\n✅ Cobrado: ${cobrado:N2}\n⏳ Por cobrar: ${porCobrar:N2}\n💰 Total: ${cobrado + porCobrar:N2}\n🏢 Departamentos: {totalDeptos}";
    }

    private static async Task<string> DeptosAsync(RentasContext db)
    {
        var deptos = await db.Departamentos
            .Include(d => d.Ubicacion).Include(d => d.Inquilino)
            .OrderBy(d => d.Ubicacion!.Calle).ThenBy(d => d.Clave)
            .ToListAsync();
        if (!deptos.Any()) return "📭 No hay departamentos registrados.";
        var sb = new System.Text.StringBuilder("🏢 *Departamentos*\n");
        string? ubiActual = null;
        foreach (var d in deptos)
        {
            var ubi = $"{d.Ubicacion?.Calle} {d.Ubicacion?.Numero}";
            if (ubi != ubiActual) { sb.AppendLine($"\n📍 *{ubi}*"); ubiActual = ubi; }
            var tel = d.Inquilino?.Telefono != null ? $" | 📞{d.Inquilino.Telefono}" : "";
            var inq = d.InquilinoCorreo != null ? $"{d.InquilinoCorreo}{tel}" : "Vacío";
            var renta = d.MontoRenta > 0 ? $"${d.MontoRenta:N0}" : "—";
            sb.AppendLine($"  *{d.Clave}* — {inq} — {renta}");
        }
        return sb.ToString();
    }

    /// Resuelve un departamento por clave compuesta: primera letra(s) de ubicación + clave
    /// Ejemplos: C1 = Chalchihuecan-1, S1 = Salchipulpos-1, F1 = Fragua-1
    /// Si no hay prefijo de letra, busca por clave sola (comportamiento anterior)
    private static async Task<(Departamento?, string)> ResolverDepto(RentasContext db, string input)
    {
        input = input.Trim();
        // Separar prefijo de letras y clave numérica/alfanumérica
        int i = 0;
        while (i < input.Length && char.IsLetter(input[i])) i++;

        if (i > 0 && i < input.Length)
        {
            // Hay prefijo: buscar ubicación cuya calle empiece con esas letras
            var prefijo = input[..i].ToLower();
            var clave = input[i..];
            var deptos = await db.Departamentos
                .Include(d => d.Ubicacion)
                .Include(d => d.Inquilino)
                .Include(d => d.ContratoLuz)
                .Where(d => d.Clave == clave)
                .ToListAsync();
            var d = deptos.FirstOrDefault(d =>
                d.Ubicacion != null &&
                d.Ubicacion.Calle.ToLower().StartsWith(prefijo));
            if (d == null)
                return (null, $"❌ No se encontró depto con clave '{clave}' en ubicación que empiece con '{prefijo.ToUpper()}'.");
            return (d, "");
        }
        else
        {
            // Sin prefijo: buscar por clave sola
            var deptos = await db.Departamentos
                .Include(d => d.Ubicacion)
                .Include(d => d.Inquilino)
                .Include(d => d.ContratoLuz)
                .Where(d => d.Clave == input)
                .ToListAsync();
            if (deptos.Count == 0)
                return (null, $"❌ Depto '{input}' no encontrado.");
            if (deptos.Count > 1)
                return (null, $"⚠️ Hay {deptos.Count} deptos con clave '{input}'. Usa prefijo de ubicación: C{input}, S{input}, F{input}...");
            return (deptos[0], "");
        }
    }

    private static async Task<string> DeptoAsync(RentasContext db, string[] parts)
    {
        if (parts.Length < 2) return "❌ Uso: /depto \\[clave\\] — Ej: /depto C1, /depto S1, /depto FA";
        var (d, err) = await ResolverDepto(db, parts[1]);
        if (d == null) return err;
        var periodo = DateTime.Now.ToString("yyyy-MM");
        var pagado = await db.Cobranza.Where(c => c.ClaveDepartamento == d.Clave && c.Periodo == periodo).SumAsync(c => c.Monto);
        return $"🏠 *Depto {d.Clave}*\n📍 {d.Ubicacion?.Calle} {d.Ubicacion?.Numero}\n👤 {d.InquilinoCorreo ?? "Vacío"}\n📞 {d.Inquilino?.Telefono ?? "—"}\n💰 Renta: ${d.MontoRenta:N0}\n💧 Agua: ${d.CuotaAgua:N0}\n📅 Día cobro: {d.DiaVencimiento}\n⚡ Contrato: {d.ContratoLuz?.Nombre ?? "—"}\n💳 Pagado este mes: ${pagado:N2}";
    }

    private static async Task<string> DeudasAsync(RentasContext db)
    {
        var periodo = DateTime.Now.ToString("yyyy-MM");
        var deptos = await db.Departamentos.Include(d => d.Ubicacion)
            .Where(d => d.InquilinoCorreo != null && d.MontoRenta > 0).ToListAsync();
        var conPago = await db.Cobranza.Where(c => c.Periodo == periodo && c.FechaCobro != null)
            .Select(c => c.ClaveDepartamento).ToListAsync();
        var sinPago = deptos.Where(d => !conPago.Contains(d.Clave)).OrderBy(d => d.Ubicacion?.Calle).ThenBy(d => d.Clave).ToList();
        if (!sinPago.Any()) return $"✅ Todos pagaron en {periodo}.";
        var sb = new System.Text.StringBuilder($"⚠️ *Sin pago en {periodo}*\n\n");
        foreach (var d in sinPago)
            sb.AppendLine($"• {d.Ubicacion?.Calle} {d.Ubicacion?.Numero} — *{d.Clave}* — ${d.MontoRenta:N0}");
        sb.AppendLine($"\n💰 Total pendiente: ${sinPago.Sum(d => d.MontoRenta):N0}");
        return sb.ToString();
    }

    private static async Task<string> TicketsAsync(RentasContext db)
    {
        var tickets = await db.Tickets.Where(t => t.Estado != "Cerrado")
            .OrderBy(t => t.Prioridad == "Alta" ? 0 : t.Prioridad == "Media" ? 1 : 2).ThenBy(t => t.FechaCreacion).ToListAsync();
        if (!tickets.Any()) return "✅ No hay tickets abiertos.";
        var sb = new System.Text.StringBuilder($"🎫 *Tickets abiertos ({tickets.Count})*\n\n");
        foreach (var t in tickets)
        {
            var icon = t.Prioridad == "Alta" ? "🔴" : t.Prioridad == "Media" ? "🟡" : "🟢";
            sb.AppendLine($"{icon} *\\#{t.ID}* \\[{t.Estado}\\] {t.Descripcion}");
        }
        return sb.ToString();
    }

    private static async Task<string> GastosAsync(RentasContext db)
    {
        var ini = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var gastos = await db.Gastos.Include(g => g.Departamento).ThenInclude(d => d!.Ubicacion)
            .Where(g => g.Fecha >= ini && g.Fecha < ini.AddMonths(1)).OrderBy(g => g.Fecha).ToListAsync();
        if (!gastos.Any()) return $"📭 No hay gastos en {ini:MMMM yyyy}.";
        var sb = new System.Text.StringBuilder($"💸 *Gastos {ini:MMMM yyyy}*\n\n");
        foreach (var g in gastos)
            sb.AppendLine($"• {g.Departamento?.Clave} — {g.Descripcion ?? "Sin desc."} — ${g.ManoDeObra + g.Material:N0}");
        sb.AppendLine($"\n💰 Total: ${gastos.Sum(g => g.ManoDeObra + g.Material):N0}");
        return sb.ToString();
    }

    private static async Task<string> PagarAsync(RentasContext db, string[] parts)
    {
        if (parts.Length < 3) return "❌ Uso: /pagar \\[clave\\] \\[monto\\] \\[medio\\] — Ej: /pagar C1 5600, /pagar S3 6800 Transferencia";
        if (!double.TryParse(parts[2], out double monto)) return "❌ Monto inválido.";
        var (depto, err) = await ResolverDepto(db, parts[1]);
        if (depto == null) return err;
        var medio = parts.Length > 3 ? string.Join(" ", parts.Skip(3)) : "Telegram";
        db.Cobranza.Add(new Cobranza { IDUbicacion = depto.IDUbicacion, ClaveDepartamento = depto.Clave, Periodo = DateTime.Now.ToString("yyyy-MM"), Monto = monto, FechaCobro = DateTime.Now, Medio = medio });
        await db.SaveChangesAsync();
        return $"✅ Pago registrado\n🏠 Depto: *{depto.Clave}*\n💰 Monto: ${monto:N2}\n💳 Medio: {medio}\n📅 {DateTime.Now:dd/MM/yyyy HH:mm}";
    }

    private static async Task<string> NotaAsync(RentasContext db, string[] parts)
    {
        if (parts.Length < 3) return "❌ Uso: /nota \\[clave\\] \\[texto\\] — Ej: /nota C1 Pagará el día 5";
        var (depto, err) = await ResolverDepto(db, parts[1]);
        if (depto == null) return err;
        var texto = string.Join(" ", parts.Skip(2));
        db.NotasDepartamento.Add(new NotaDepartamento { DepartamentoId = depto.ID, Texto = texto, UsuarioCreo = "@TelegramBot" });
        await db.SaveChangesAsync();
        return $"✅ Nota agregada al depto *{depto.Clave}*";
    }

    private static async Task<string> CrearTicketAsync(RentasContext db, string[] parts, string chatId)
    {
        if (parts.Length < 3) return "❌ Uso: /ticket \\[Alta/Media/Baja\\] \\[descripcion\\]";
        var prioridad = parts[1];
        if (!new[] { "Alta", "Media", "Baja" }.Contains(prioridad, StringComparer.OrdinalIgnoreCase)) return "❌ Prioridad: Alta, Media o Baja";
        var desc = string.Join(" ", parts.Skip(2));
        var t = new Ticket { Prioridad = char.ToUpper(prioridad[0]) + prioridad[1..].ToLower(), Descripcion = desc, UsuarioCreo = $"Telegram:{chatId}", Estado = "Abierto", FechaCreacion = DateTime.UtcNow };
        db.Tickets.Add(t);
        await db.SaveChangesAsync();
        var icon = t.Prioridad == "Alta" ? "🔴" : t.Prioridad == "Media" ? "🟡" : "🟢";
        return $"✅ Ticket creado\n🎫 *\\#{t.ID}*\n{icon} {t.Prioridad}\n📝 {desc}";
    }

    // ─── MODELOS ─────────────────────────────────────────────────────────────
    public class TelegramResponse { public List<Update> Result { get; set; } = new(); }
    public class Update { [JsonPropertyName("update_id")] public int UpdateId { get; set; } public Message? Message { get; set; } }
    public class Message { public Chat Chat { get; set; } = new(); public string? Text { get; set; } }
    public class Chat { public long Id { get; set; } }
}
