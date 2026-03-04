# Rentas MCP Server

Servidor MCP (Model Context Protocol) para conectar IAs a la base de datos de Rentas.

## Características

- **Solo lectura** - No permite modificar datos
- **Queries SQL** - SELECT, JOINs, agregaciones para reportes
- **Validación** - Bloquea INSERT, UPDATE, DELETE, DROP, etc.

## Tools disponibles

| Tool | Descripción |
|------|-------------|
| `query` | Ejecutar consulta SQL (solo SELECT) |
| `list_tables` | Listar todas las tablas |
| `describe_table` | Ver columnas de una tabla |
| `get_schema` | Ver schema completo |
| `sample_data` | Ver datos de ejemplo (10 filas) |

## Uso

### Con mcporter (CLI)

```bash
# Listar tablas
mcporter call --stdio "node /path/to/mcp-server/index.js" list_tables

# Query para reporte
mcporter call --stdio "node /path/to/mcp-server/index.js" query \
  sql="SELECT * FROM Cobranza WHERE Pagado = 0"
```

### Con Claude Desktop

Agregar a `~/Library/Application Support/Claude/claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "rentas": {
      "command": "node",
      "args": ["/home/admin/Projects/rentas-system/mcp-server/index.js"]
    }
  }
}
```

### Con OpenClaw

Agregar a la config de OpenClaw:

```yaml
mcp:
  servers:
    rentas:
      command: node
      args:
        - /home/admin/Projects/rentas-system/mcp-server/index.js
```

### Con cualquier cliente MCP (stdio)

```bash
node /home/admin/Projects/rentas-system/mcp-server/index.js
```

El servidor usa stdio (JSON-RPC sobre stdin/stdout).

## Ejemplos de queries para reportes

```sql
-- Deudores (cobranza no pagada)
SELECT d.Clave, u.Calle, u.Numero, c.Monto, c.FechaVencimiento
FROM Cobranza c
JOIN Departamento d ON c.DepartamentoID = d.ID
JOIN Ubicaciones u ON d.IDUbicacion = u.IDUbicacion
WHERE c.Pagado = 0
ORDER BY c.FechaVencimiento;

-- Ingresos por mes
SELECT strftime('%Y-%m', FechaPago) as mes, SUM(Monto) as total
FROM Cobranza
WHERE Pagado = 1
GROUP BY mes
ORDER BY mes;

-- Ocupación por ubicación
SELECT u.Calle, u.Numero, 
       COUNT(d.ID) as total_deptos,
       SUM(CASE WHEN d.Ocupado THEN 1 ELSE 0 END) as ocupados
FROM Ubicaciones u
LEFT JOIN Departamento d ON u.IDUbicacion = d.IDUbicacion
GROUP BY u.IDUbicacion;
```

## Variables de entorno

- `RENTAS_DB` - Ruta a rentas.db (default: ../rentas.db)
