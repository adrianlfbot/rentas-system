#!/usr/bin/env node
/**
 * Notifica a Propietarios cuando un contrato de Luz, Agua o Internet
 * vence HOY (FechaVencimiento = fecha actual).
 * Ejecutar diariamente a las 9:00 AM via cron.
 */
const Database = require('better-sqlite3');
const path = require('path');

const CONFIG = {
  dbPath: process.env.RENTAS_DB || path.join(__dirname, '..', 'rentas.db'),
  whatsappUrl: process.env.WHATSAPP_URL || 'http://localhost:3101',
  whatsappApiKey: process.env.WHATSAPP_API_KEY || '921ee934cbd55dac2186d1ab253e34e2',
  dryRun: process.argv.includes('--dry-run'),
};

function hoyISO() {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
}

async function main() {
  const db = new Database(CONFIG.dbPath, { readonly: true });
  const hoy = hoyISO();

  console.log(`📅 Fecha: ${hoy}`);
  console.log(`🔧 Modo: ${CONFIG.dryRun ? 'DRY RUN' : 'PRODUCCIÓN'}\n`);

  // Contratos que vencen hoy, con la ubicación asociada
  const queries = [
    {
      tipo: '⚡ Luz',
      sql: `
        SELECT cl.Nombre, cl.RPU, cl.FechaVencimiento,
               ub.Calle, ub.Numero
        FROM ContratoLuz cl
        JOIN Ubicaciones ub ON ub.ContratoLuzId = cl.ID
        WHERE date(cl.FechaVencimiento) = ?`,
    },
    {
      tipo: '💧 Agua',
      sql: `
        SELECT ca.Nombre, ca.NumeroContrato, ca.FechaVencimiento,
               ub.Calle, ub.Numero
        FROM ContratoAgua ca
        JOIN Ubicaciones ub ON ub.ContratoAguaId = ca.ID
        WHERE date(ca.FechaVencimiento) = ?`,
    },
    {
      tipo: '🌐 Internet',
      sql: `
        SELECT ci.Nombre, ci.NumeroContrato, ci.FechaVencimiento,
               ub.Calle, ub.Numero
        FROM ContratoInternet ci
        JOIN Ubicaciones ub ON ub.ContratoInternetId = ci.ID
        WHERE date(ci.FechaVencimiento) = ?`,
    },
  ];

  // Recopilar alertas
  const alertas = [];
  for (const q of queries) {
    const rows = db.prepare(q.sql).all(hoy);
    for (const r of rows) {
      alertas.push({
        tipo: q.tipo,
        nombre: r.Nombre,
        contrato: r.RPU || r.NumeroContrato || '-',
        ubicacion: `${r.Calle} ${r.Numero}`,
      });
    }
  }

  if (alertas.length === 0) {
    console.log('✅ No hay contratos por vencer hoy.');
    db.close();
    return;
  }

  // Construir mensaje
  let mensaje = `🏠 *Contratos que vencen hoy (${hoy})*\n\n`;
  for (const a of alertas) {
    mensaje += `${a.tipo}\n`;
    mensaje += `  📍 ${a.ubicacion}\n`;
    mensaje += `  📝 ${a.nombre} (${a.contrato})\n\n`;
  }
  mensaje = mensaje.trim();

  console.log('--- Mensaje ---');
  console.log(mensaje);
  console.log('---------------\n');

  // Obtener admins (Propietario) con teléfono
  const admins = db.prepare(`
    SELECT Correo, Telefono FROM Usuarios
    WHERE Tipo = 'Propietario'
      AND Telefono IS NOT NULL
      AND Telefono != ''
  `).all();

  db.close();

  if (admins.length === 0) {
    console.log('⚠️  No hay propietarios con teléfono registrado.');
    return;
  }

  console.log(`📋 Propietarios a notificar: ${admins.length}\n`);

  for (const admin of admins) {
    console.log(`👤 ${admin.Correo} — 📱 ${admin.Telefono}`);

    if (CONFIG.dryRun) {
      console.log('   ⏭️  [DRY RUN]\n');
      continue;
    }

    try {
      const res = await fetch(`${CONFIG.whatsappUrl}/send`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-API-Key': CONFIG.whatsappApiKey,
        },
        body: JSON.stringify({ phone: admin.Telefono, message: mensaje }),
      });
      const result = await res.json();
      if (result.success) {
        console.log(`   ✅ Enviado (${result.messageId})\n`);
      } else {
        console.log(`   ❌ ${result.error}\n`);
      }
    } catch (err) {
      console.log(`   ❌ ${err.message}\n`);
    }

    await new Promise(r => setTimeout(r, 2000));
  }
}

main().catch(err => {
  console.error('Error:', err);
  process.exit(1);
});
