#!/usr/bin/env node
/**
 * HTTP wrapper for Rentas MCP Server
 * Exposes MCP tools via REST API for remote access
 */
import { createServer } from "http";
import Database from "better-sqlite3";
import { fileURLToPath } from "url";
import { dirname, join } from "path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const DB_PATH = process.env.RENTAS_DB || join(__dirname, "..", "rentas.db");
const PORT = process.env.PORT || 3100;
const API_KEY = process.env.API_KEY || "rentas-api-key-change-me";

const db = new Database(DB_PATH, { readonly: true });

// Sensitive tables (blocked from access)
const BLOCKED_TABLES = ["usuarios"];

// Tool implementations
const tools = {
  query(args) {
    const sql = args.sql?.trim();
    if (!sql) throw new Error("SQL query required");
    
    const normalized = sql.toUpperCase().replace(/\s+/g, " ").trim();
    if (!normalized.startsWith("SELECT") && !normalized.startsWith("WITH")) {
      throw new Error("Only SELECT queries allowed");
    }
    
    const forbidden = ["INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "REPLACE"];
    for (const word of forbidden) {
      if (normalized.includes(word)) throw new Error(`Forbidden: ${word}`);
    }
    
    // Block sensitive tables
    for (const table of BLOCKED_TABLES) {
      if (normalized.includes(table.toUpperCase())) {
        throw new Error(`Access denied: table ${table} is restricted`);
      }
    }
    
    return db.prepare(sql).all();
  },

  list_tables() {
    return db
      .prepare("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND LOWER(name) NOT IN ('usuarios') ORDER BY name")
      .all()
      .map(t => t.name);
  },

  describe_table(args) {
    const table = args.table;
    if (!table || !/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(table)) {
      throw new Error("Invalid table name");
    }
    if (BLOCKED_TABLES.includes(table.toLowerCase())) {
      throw new Error(`Access denied: table ${table} is restricted`);
    }
    return {
      columns: db.prepare(`PRAGMA table_info("${table}")`).all(),
      foreign_keys: db.prepare(`PRAGMA foreign_key_list("${table}")`).all(),
    };
  },

  get_schema() {
    return db
      .prepare("SELECT name, sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND LOWER(name) NOT IN ('usuarios') ORDER BY name")
      .all();
  },

  sample_data(args) {
    const table = args.table;
    const limit = Math.min(args.limit || 10, 100);
    if (!table || !/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(table)) {
      throw new Error("Invalid table name");
    }
    if (BLOCKED_TABLES.includes(table.toLowerCase())) {
      throw new Error(`Access denied: table ${table} is restricted`);
    }
    return db.prepare(`SELECT * FROM "${table}" LIMIT ?`).all(limit);
  },
};

const server = createServer(async (req, res) => {
  // CORS
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type, Authorization, X-API-Key");
  
  if (req.method === "OPTIONS") {
    res.writeHead(204);
    return res.end();
  }

  // Auth check
  const apiKey = req.headers["x-api-key"] || req.headers["authorization"]?.replace("Bearer ", "");
  if (apiKey !== API_KEY) {
    res.writeHead(401, { "Content-Type": "application/json" });
    return res.end(JSON.stringify({ error: "Unauthorized" }));
  }

  const url = new URL(req.url, `http://${req.headers.host}`);
  const path = url.pathname;

  try {
    // GET /tools - list available tools
    if (path === "/tools" && req.method === "GET") {
      res.writeHead(200, { "Content-Type": "application/json" });
      return res.end(JSON.stringify({
        tools: Object.keys(tools).map(name => ({
          name,
          description: {
            query: "Execute read-only SQL (SELECT only)",
            list_tables: "List all tables",
            describe_table: "Get table schema",
            get_schema: "Get full database schema",
            sample_data: "Get sample rows from table",
          }[name],
        })),
      }));
    }

    // POST /call/:tool - call a tool
    const callMatch = path.match(/^\/call\/([a-z_]+)$/);
    if (callMatch && req.method === "POST") {
      const toolName = callMatch[1];
      if (!tools[toolName]) {
        res.writeHead(404, { "Content-Type": "application/json" });
        return res.end(JSON.stringify({ error: `Unknown tool: ${toolName}` }));
      }

      let body = "";
      for await (const chunk of req) body += chunk;
      const args = body ? JSON.parse(body) : {};

      const result = tools[toolName](args);
      res.writeHead(200, { "Content-Type": "application/json" });
      return res.end(JSON.stringify({ result }));
    }

    // GET /query?sql=... - shortcut for query tool
    if (path === "/query" && req.method === "GET") {
      const sql = url.searchParams.get("sql");
      const result = tools.query({ sql });
      res.writeHead(200, { "Content-Type": "application/json" });
      return res.end(JSON.stringify({ result }));
    }

    // GET /tables - shortcut for list_tables
    if (path === "/tables" && req.method === "GET") {
      const result = tools.list_tables();
      res.writeHead(200, { "Content-Type": "application/json" });
      return res.end(JSON.stringify({ result }));
    }

    // GET /schema - shortcut for get_schema
    if (path === "/schema" && req.method === "GET") {
      const result = tools.get_schema();
      res.writeHead(200, { "Content-Type": "application/json" });
      return res.end(JSON.stringify({ result }));
    }

    // Not found
    res.writeHead(404, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ error: "Not found" }));

  } catch (err) {
    res.writeHead(400, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ error: err.message }));
  }
});

server.listen(PORT, () => {
  console.log(`Rentas HTTP API running on http://localhost:${PORT}`);
  console.log(`API Key: ${API_KEY}`);
});
