#!/usr/bin/env node
const { Client, LocalAuth } = require('whatsapp-web.js');
const qrcode = require('qrcode-terminal');
const QRCode = require('qrcode');
const express = require('express');

const PORT = process.env.PORT || 3101;
const API_KEY = process.env.API_KEY || 'whatsapp-api-key-change-me';

let clientReady = false;
let lastQR = null;

// WhatsApp Client
const client = new Client({
  authStrategy: new LocalAuth({ dataPath: './session' }),
  puppeteer: {
    headless: true,
    executablePath: '/usr/bin/chromium-browser',
    args: ['--no-sandbox', '--disable-setuid-sandbox', '--disable-gpu', '--disable-dev-shm-usage']
  }
});

client.on('qr', (qr) => {
  lastQR = qr;
  console.log('\n📱 Escanea este QR con WhatsApp:\n');
  qrcode.generate(qr, { small: true });
});

client.on('ready', () => {
  clientReady = true;
  lastQR = null;
  console.log('✅ WhatsApp conectado!');
});

client.on('authenticated', () => {
  console.log('🔐 Autenticado');
});

client.on('auth_failure', (msg) => {
  console.error('❌ Error de autenticación:', msg);
});

client.on('disconnected', (reason) => {
  clientReady = false;
  console.log('📴 Desconectado:', reason);
});

// HTTP API
const app = express();
app.use(express.json());

// Auth middleware
app.use((req, res, next) => {
  if (req.path === '/status' || req.path === '/qr' || req.path === '/qr.png') {
    return next(); // Public endpoints
  }
  const apiKey = req.headers['x-api-key'] || req.headers['authorization']?.replace('Bearer ', '');
  if (apiKey !== API_KEY) {
    return res.status(401).json({ error: 'Unauthorized' });
  }
  next();
});

// Status
app.get('/status', (req, res) => {
  res.json({ 
    ready: clientReady,
    hasQR: !!lastQR
  });
});

// Get QR (for remote auth)
app.get('/qr', (req, res) => {
  if (clientReady) {
    return res.json({ ready: true, message: 'Already authenticated' });
  }
  if (!lastQR) {
    return res.json({ ready: false, message: 'QR not generated yet, wait...' });
  }
  res.json({ ready: false, qr: lastQR });
});

// Get QR as image
app.get('/qr.png', async (req, res) => {
  if (clientReady) {
    return res.status(200).send('Already authenticated');
  }
  if (!lastQR) {
    return res.status(503).send('QR not ready, wait...');
  }
  try {
    const qrImage = await QRCode.toBuffer(lastQR, { width: 300 });
    res.type('png').send(qrImage);
  } catch (err) {
    res.status(500).send('Error generating QR');
  }
});

// Send message
app.post('/send', async (req, res) => {
  if (!clientReady) {
    return res.status(503).json({ error: 'WhatsApp not ready' });
  }
  
  const { phone, message } = req.body;
  if (!phone || !message) {
    return res.status(400).json({ error: 'phone and message required' });
  }
  
  try {
    // Format phone number (remove +, spaces, dashes)
    let number = phone.toString().replace(/[\s\-\+]/g, '');
    // Add country code if not present (default Mexico +52)
    if (!number.startsWith('52') && number.length === 10) {
      number = '52' + number;
    }
    
    // Verify number is registered on WhatsApp
    const numberId = await client.getNumberId(number);
    if (!numberId) {
      return res.status(400).json({ error: `El número ${number} no está registrado en WhatsApp` });
    }
    
    const chatId = numberId._serialized;
    const result = await client.sendMessage(chatId, message);
    res.json({ success: true, messageId: result.id._serialized });
  } catch (err) {
    res.status(500).json({ error: err.message });
  }
});

// Send to multiple
app.post('/broadcast', async (req, res) => {
  if (!clientReady) {
    return res.status(503).json({ error: 'WhatsApp not ready' });
  }
  
  const { phones, message } = req.body;
  if (!phones || !Array.isArray(phones) || !message) {
    return res.status(400).json({ error: 'phones (array) and message required' });
  }
  
  const results = [];
  for (const phone of phones) {
    try {
      let number = phone.toString().replace(/[\s\-\+]/g, '');
      if (!number.startsWith('52') && number.length === 10) {
        number = '52' + number;
      }
      const chatId = number + '@c.us';
      const result = await client.sendMessage(chatId, message);
      results.push({ phone, success: true, messageId: result.id._serialized });
    } catch (err) {
      results.push({ phone, success: false, error: err.message });
    }
    // Small delay between messages
    await new Promise(r => setTimeout(r, 1000));
  }
  
  res.json({ results });
});

// Start
app.listen(PORT, () => {
  console.log(`WhatsApp API running on http://localhost:${PORT}`);
  console.log(`API Key: ${API_KEY}`);
});

client.initialize();
