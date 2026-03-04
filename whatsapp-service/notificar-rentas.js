#!/usr/bin/env node
/**
 * Script para notificar a inquilinos sobre vencimiento de renta
 * Ejecutar diariamente a las 10 AM
 */
const Database = require('better-sqlite3');
const path = require('path');

// Configuración
const CONFIG = {
  dbPath: process.env.RENTAS_DB || path.join(__dirname, '..', 'rentas.db'),
  whatsappUrl: process.env.WHATSAPP_URL || 'http://localhost:3101',
  whatsappApiKey: process.env.WHATSAPP_API_KEY || '921ee934cbd55dac2186d1ab253e34e2',
  
  // Plantilla de mensaje (usar {monto} como placeholder)
  mensaje: process.env.MENSAJE_RENTA || 
    'Su renta vence el día de hoy por el monto de ${monto}, agradecemos su pronto pago.',
  
  // Si true, solo muestra qué haría sin enviar
  dryRun: process.argv.includes('--dry-run'),
};

async function main() {
  const db = new Database(CONFIG.dbPath, { readonly: true });
  
  // Obtener día actual del mes
  const hoy = new Date();
  const diaHoy = hoy.getDate();
  
  console.log(`📅 Fecha: ${hoy.toISOString().split('T')[0]}`);
  console.log(`📆 Día del mes: ${diaHoy}`);
  console.log(`🔧 Modo: ${CONFIG.dryRun ? 'DRY RUN (no envía)' : 'PRODUCCIÓN'}\n`);
  
  // Buscar departamentos con vencimiento hoy que tengan inquilino con teléfono
  const query = `
    SELECT 
      d.Clave,
      d.MontoRenta,
      d.DiaVencimiento,
      d.InquilinoCorreo,
      u.Telefono,
      ub.Calle,
      ub.Numero
    FROM Departamento d
    JOIN Usuarios u ON d.InquilinoCorreo = u.Correo
    JOIN Ubicaciones ub ON d.IDUbicacion = ub.IDUbicacion
    WHERE d.DiaVencimiento = ?
      AND d.InquilinoCorreo IS NOT NULL
      AND u.Telefono IS NOT NULL
      AND u.Telefono != ''
  `;
  
  const inquilinos = db.prepare(query).all(diaHoy);
  
  if (inquilinos.length === 0) {
    console.log('✅ No hay rentas por vencer hoy.');
    return;
  }
  
  console.log(`📋 Inquilinos a notificar: ${inquilinos.length}\n`);
  
  const resultados = [];
  
  for (const inq of inquilinos) {
    const monto = inq.MontoRenta.toLocaleString('es-MX', { 
      minimumFractionDigits: 2,
      maximumFractionDigits: 2 
    });
    
    const mensaje = CONFIG.mensaje.replace('{monto}', monto).replace('${monto}', `$${monto}`);
    
    console.log(`👤 ${inq.Clave} - ${inq.Calle} ${inq.Numero}`);
    console.log(`   📱 Tel: ${inq.Telefono}`);
    console.log(`   💰 Monto: $${monto}`);
    console.log(`   💬 Mensaje: ${mensaje}`);
    
    if (CONFIG.dryRun) {
      console.log('   ⏭️  [DRY RUN - No enviado]\n');
      resultados.push({ phone: inq.Telefono, success: true, dryRun: true });
      continue;
    }
    
    try {
      const response = await fetch(`${CONFIG.whatsappUrl}/send`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-API-Key': CONFIG.whatsappApiKey,
        },
        body: JSON.stringify({
          phone: inq.Telefono,
          message: mensaje,
        }),
      });
      
      const result = await response.json();
      
      if (result.success) {
        console.log(`   ✅ Enviado (ID: ${result.messageId})\n`);
        resultados.push({ phone: inq.Telefono, success: true, messageId: result.messageId });
      } else {
        console.log(`   ❌ Error: ${result.error}\n`);
        resultados.push({ phone: inq.Telefono, success: false, error: result.error });
      }
    } catch (err) {
      console.log(`   ❌ Error: ${err.message}\n`);
      resultados.push({ phone: inq.Telefono, success: false, error: err.message });
    }
    
    // Pausa entre mensajes
    await new Promise(r => setTimeout(r, 2000));
  }
  
  // Resumen
  const enviados = resultados.filter(r => r.success).length;
  const fallidos = resultados.filter(r => !r.success).length;
  
  console.log('━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━');
  console.log(`📊 Resumen: ${enviados} enviados, ${fallidos} fallidos`);
  
  db.close();
}

main().catch(err => {
  console.error('Error:', err);
  process.exit(1);
});
