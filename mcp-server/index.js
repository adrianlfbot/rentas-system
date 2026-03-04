#!/usr/bin/env node
import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import {
  CallToolRequestSchema,
  ListToolsRequestSchema,
  ListResourcesRequestSchema,
  ReadResourceRequestSchema,
} from "@modelcontextprotocol/sdk/types.js";
import Database from "better-sqlite3";
import { fileURLToPath } from "url";
import { dirname, join } from "path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const DB_PATH = process.env.RENTAS_DB || join(__dirname, "..", "rentas.db");

const db = new Database(DB_PATH, { readonly: true });

const server = new Server(
  { name: "rentas-db", version: "1.0.0" },
  { capabilities: { tools: {}, resources: {} } }
);

// List available tools
server.setRequestHandler(ListToolsRequestSchema, async () => ({
  tools: [
    {
      name: "query",
      description: "Execute a read-only SQL query (SELECT only). Use for reports, aggregations, joins.",
      inputSchema: {
        type: "object",
        properties: {
          sql: { type: "string", description: "SQL SELECT query" },
        },
        required: ["sql"],
      },
    },
    {
      name: "list_tables",
      description: "List all tables in the database",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "describe_table",
      description: "Get column info for a specific table",
      inputSchema: {
        type: "object",
        properties: {
          table: { type: "string", description: "Table name" },
        },
        required: ["table"],
      },
    },
    {
      name: "get_schema",
      description: "Get full database schema (all CREATE statements)",
      inputSchema: { type: "object", properties: {} },
    },
    {
      name: "sample_data",
      description: "Get sample rows from a table (first 10 rows)",
      inputSchema: {
        type: "object",
        properties: {
          table: { type: "string", description: "Table name" },
          limit: { type: "number", description: "Number of rows (default 10, max 100)" },
        },
        required: ["table"],
      },
    },
  ],
}));

// Handle tool calls
server.setRequestHandler(CallToolRequestSchema, async (request) => {
  const { name, arguments: args } = request.params;

  try {
    switch (name) {
      case "query": {
        const sql = args.sql?.trim();
        if (!sql) throw new Error("SQL query required");
        
        // Only allow SELECT statements
        const normalized = sql.toUpperCase().replace(/\s+/g, " ").trim();
        if (!normalized.startsWith("SELECT") && !normalized.startsWith("WITH")) {
          throw new Error("Only SELECT queries allowed (read-only mode)");
        }
        
        // Block dangerous keywords
        const forbidden = ["INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE", "REPLACE"];
        for (const word of forbidden) {
          if (normalized.includes(word)) {
            throw new Error(`Forbidden keyword: ${word}`);
          }
        }
        
        // Block access to sensitive tables
        const sensitiveTable = ["USUARIOS"];
        for (const table of sensitiveTable) {
          if (normalized.includes(table)) {
            throw new Error(`Access denied: table ${table} is restricted`);
          }
        }

        const rows = db.prepare(sql).all();
        return {
          content: [{ type: "text", text: JSON.stringify(rows, null, 2) }],
        };
      }

      case "list_tables": {
        const tables = db
          .prepare("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != 'Usuarios' ORDER BY name")
          .all();
        return {
          content: [{ type: "text", text: JSON.stringify(tables.map((t) => t.name), null, 2) }],
        };
      }

      case "describe_table": {
        const table = args.table;
        if (!table) throw new Error("Table name required");
        
        // Sanitize table name
        if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(table)) {
          throw new Error("Invalid table name");
        }
        
        // Block sensitive tables
        if (table.toLowerCase() === "usuarios") {
          throw new Error("Access denied: table Usuarios is restricted");
        }
        
        const columns = db.prepare(`PRAGMA table_info("${table}")`).all();
        const fks = db.prepare(`PRAGMA foreign_key_list("${table}")`).all();
        
        return {
          content: [{
            type: "text",
            text: JSON.stringify({ columns, foreign_keys: fks }, null, 2),
          }],
        };
      }

      case "get_schema": {
        const schema = db
          .prepare("SELECT sql FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != 'Usuarios' ORDER BY name")
          .all();
        return {
          content: [{ type: "text", text: schema.map((s) => s.sql).join("\n\n") }],
        };
      }

      case "sample_data": {
        const table = args.table;
        const limit = Math.min(args.limit || 10, 100);
        
        if (!table) throw new Error("Table name required");
        if (!/^[a-zA-Z_][a-zA-Z0-9_]*$/.test(table)) {
          throw new Error("Invalid table name");
        }
        
        // Block sensitive tables
        if (table.toLowerCase() === "usuarios") {
          throw new Error("Access denied: table Usuarios is restricted");
        }
        
        const rows = db.prepare(`SELECT * FROM "${table}" LIMIT ?`).all(limit);
        return {
          content: [{ type: "text", text: JSON.stringify(rows, null, 2) }],
        };
      }

      default:
        throw new Error(`Unknown tool: ${name}`);
    }
  } catch (error) {
    return {
      content: [{ type: "text", text: `Error: ${error.message}` }],
      isError: true,
    };
  }
});

// List resources (expose tables as resources)
server.setRequestHandler(ListResourcesRequestSchema, async () => {
  const tables = db
    .prepare("SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name != 'Usuarios' ORDER BY name")
    .all();

  return {
    resources: tables.map((t) => ({
      uri: `rentas://table/${t.name}`,
      name: t.name,
      description: `Table: ${t.name}`,
      mimeType: "application/json",
    })),
  };
});

// Read resource
server.setRequestHandler(ReadResourceRequestSchema, async (request) => {
  const uri = request.params.uri;
  const match = uri.match(/^rentas:\/\/table\/([a-zA-Z_][a-zA-Z0-9_]*)$/);
  
  if (!match) {
    throw new Error(`Invalid resource URI: ${uri}`);
  }
  
  const table = match[1];
  
  // Block sensitive tables
  if (table.toLowerCase() === "usuarios") {
    throw new Error("Access denied: table Usuarios is restricted");
  }
  
  const rows = db.prepare(`SELECT * FROM "${table}" LIMIT 100`).all();
  
  return {
    contents: [{
      uri,
      mimeType: "application/json",
      text: JSON.stringify(rows, null, 2),
    }],
  };
});

// Start server
const transport = new StdioServerTransport();
await server.connect(transport);
console.error("Rentas MCP server running on stdio");
