<template>
  <div class="p-8">
    <h1 class="text-2xl font-bold mb-6">Dashboard</h1>
    
    <!-- Stats Cards -->
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4 mb-8">
      <div v-for="stat in stats" :key="stat.label" class="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <p class="text-3xl font-bold" :class="stat.color">{{ stat.value }}</p>
        <p class="text-sm text-gray-400 mt-1">{{ stat.label }}</p>
      </div>
    </div>

    <div v-if="auth.isPropietario" class="space-y-6">

      <!-- Fila 1: Mes actual + Últimos 6 meses -->
      <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div class="bg-gray-900 border border-gray-800 rounded-xl p-6">
          <h2 class="text-lg font-bold mb-4">📊 {{ mesActualNombre }}</h2>
          <div class="h-64">
            <Bar v-if="chartDataMes" :data="chartDataMes" :options="chartOptionsMoney" />
          </div>
          <div class="mt-4 grid grid-cols-2 gap-4 text-center">
            <div>
              <p class="text-2xl font-bold text-emerald-400">${{ formatMoney(totalesMes.cobrado) }}</p>
              <p class="text-xs text-gray-400">Cobrado</p>
            </div>
            <div>
              <p class="text-2xl font-bold text-red-400">${{ formatMoney(totalesMes.porCobrar) }}</p>
              <p class="text-xs text-gray-400">Por Cobrar</p>
            </div>
          </div>
        </div>

        <div class="bg-gray-900 border border-gray-800 rounded-xl p-6">
          <h2 class="text-lg font-bold mb-4">📈 Últimos 6 Meses</h2>
          <div class="h-64">
            <Bar v-if="chartData6Meses" :data="chartData6Meses" :options="chartOptionsMoney" />
          </div>
        </div>
      </div>

      <!-- Checklist de pagos por día de cobro -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <div class="flex items-center justify-between mb-4">
          <h2 class="text-lg font-bold">✅ Estado de Pagos — {{ mesActualNombre }}</h2>
          <div class="flex gap-2 text-xs">
            <span class="flex items-center gap-1"><span class="w-3 h-3 rounded-full bg-emerald-500 inline-block"></span> Pagó</span>
            <span class="flex items-center gap-1"><span class="w-3 h-3 rounded-full bg-red-500 inline-block"></span> Pendiente</span>
          </div>
        </div>

        <div v-if="checklistDias.length === 0" class="text-gray-500 text-sm">Sin departamentos con renta registrada.</div>

        <div v-for="grupo in checklistDias" :key="grupo.dia" class="mb-6">
          <div class="flex items-center justify-between mb-3">
            <h3 class="font-semibold text-gray-300">📅 Día {{ grupo.dia }} del mes</h3>
            <span class="text-xs text-gray-500">
              {{ grupo.pagados }} / {{ grupo.deptos.length }} pagaron
              — ${{ formatMoney(grupo.montoCobrado) }} de ${{ formatMoney(grupo.montoTotal) }}
            </span>
          </div>
          <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-2">
            <div v-for="d in grupo.deptos" :key="d.id"
              class="flex items-center gap-3 px-3 py-2 rounded-lg border"
              :class="d.pago ? 'border-emerald-800 bg-emerald-900/20' : 'border-red-800 bg-red-900/10'">
              <span class="w-2.5 h-2.5 rounded-full flex-shrink-0" :class="d.pago ? 'bg-emerald-400' : 'bg-red-400'"></span>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium truncate">{{ d.ubicacion }} — <span class="font-mono">{{ d.clave }}</span></p>
                <p class="text-xs text-gray-500 truncate">{{ d.inquilino || 'Sin inquilino' }}</p>
              </div>
              <div class="text-right flex-shrink-0">
                <p class="text-xs font-bold" :class="d.pago ? 'text-emerald-400' : 'text-red-400'">${{ formatMoney(d.monto) }}</p>
                <p v-if="d.pago" class="text-xs text-gray-500">{{ d.fechaPago }}</p>
              </div>
              <button v-if="!d.pago" @click="abrirPago(d)"
                class="flex-shrink-0 text-xs px-2 py-1 bg-emerald-700 hover:bg-emerald-600 rounded-lg font-medium transition-colors">
                💰
              </button>
            </div>
          </div>
        </div>
      </div>

    <!-- Modal Capturar Pago -->
    <div v-if="showPagoModal" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-sm">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-lg font-bold">💰 Registrar Pago</h2>
          <button @click="showPagoModal = false" class="text-gray-400 hover:text-white">✕</button>
        </div>
        <div class="space-y-3 mb-4">
          <div class="bg-gray-800 rounded-lg px-4 py-3 text-sm">
            <p class="text-gray-400">Departamento</p>
            <p class="font-bold">{{ pagoDepto?.ubicacion }} — <span class="font-mono">{{ pagoDepto?.clave }}</span></p>
            <p class="text-gray-400 text-xs">{{ pagoDepto?.inquilino }}</p>
          </div>
          <div>
            <label class="text-xs text-gray-400 mb-1 block">Monto $</label>
            <input v-model.number="pagoForm.monto" type="number" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          </div>
          <div>
            <label class="text-xs text-gray-400 mb-1 block">Medio de pago</label>
            <select v-model="pagoForm.medio" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
              <option>Efectivo</option>
              <option>Transferencia</option>
              <option>Tarjeta</option>
              <option>Cheque</option>
              <option>Otro</option>
            </select>
          </div>
          <div>
            <label class="text-xs text-gray-400 mb-1 block">Fecha de cobro</label>
            <input v-model="pagoForm.fecha" type="date" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          </div>
        </div>
        <div class="flex gap-2 justify-end">
          <button @click="showPagoModal = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
          <button @click="guardarPago" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">Guardar Pago</button>
        </div>
      </div>
    </div>

      <!-- Fila 2: Ingresos por Ubicación -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 class="text-lg font-bold mb-1">🏘️ Ingresos por Ubicación</h2>
        <p class="text-xs text-gray-500 mb-4">Suma de rentas mensuales de todos los departamentos por ubicación</p>
        <div class="h-72">
          <Bar v-if="chartDataUbicacion" :data="chartDataUbicacion" :options="chartOptionsUbicacion" />
        </div>
        <!-- Leyenda con montos exactos -->
        <div class="mt-4 flex flex-wrap gap-3">
          <div v-for="(item, i) in leyendaUbicacion" :key="i"
            class="flex items-center gap-2 bg-gray-800 rounded-lg px-3 py-2 text-sm">
            <span class="w-3 h-3 rounded-sm flex-shrink-0" :style="{ backgroundColor: item.color }"></span>
            <span class="text-gray-300 truncate max-w-[140px]" :title="item.label">{{ item.label }}</span>
            <span class="font-bold text-emerald-400 ml-1">${{ formatMoney(item.monto) }}</span>
          </div>
        </div>
      </div>

      <!-- Fila 3: Ingresos por Día de Vencimiento -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 class="text-lg font-bold mb-1">📅 Ingresos por Día de Vencimiento</h2>
        <p class="text-xs text-gray-500 mb-4">Suma de rentas agrupadas por el día de cobro mensual</p>
        <div class="h-72">
          <Bar v-if="chartDataVencimiento" :data="chartDataVencimiento" :options="chartOptionsVencimiento" />
        </div>
        <!-- Leyenda con montos exactos -->
        <div class="mt-4 flex flex-wrap gap-3">
          <div v-for="(item, i) in leyendaVencimiento" :key="i"
            class="flex items-center gap-2 bg-gray-800 rounded-lg px-3 py-2 text-sm">
            <span class="w-3 h-3 rounded-sm flex-shrink-0" :style="{ backgroundColor: item.color }"></span>
            <span class="text-gray-300">Día {{ item.dia }}</span>
            <span class="font-bold text-emerald-400 ml-1">${{ formatMoney(item.monto) }}</span>
            <span class="text-gray-500 text-xs">({{ item.deptos }} deptos)</span>
          </div>
        </div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import { Bar } from 'vue-chartjs'
import { Chart as ChartJS, CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend } from 'chart.js'
import api from '../api'
import { useAuthStore } from '../stores/auth'
import { useToast } from '../composables/useToast'

const { success, error: toastError } = useToast()

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

const auth = useAuthStore()
const stats = ref([])
const cobranzaData = ref([])
const departamentosData = ref([])
const ubicacionesData = ref([])

// ─── Checklist de pagos por día de cobro ─────────────────────────────────────
const checklistDias = computed(() => {
  if (!departamentosData.value.length) return []
  const now = new Date()
  const periodo = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`

  // Agrupar departamentos por día de vencimiento
  const grupos = {}
  departamentosData.value
    .filter(d => d.montoRenta > 0)
    .forEach(d => {
      const dia = d.diaVencimiento || 1
      if (!grupos[dia]) grupos[dia] = []

      // Ver si tiene pago en el mes actual (filtrando por ubicación + clave para evitar colisiones)
      const pagos = cobranzaData.value.filter(c =>
        c.claveDepartamento === d.clave &&
        c.idUbicacion === d.idUbicacion &&
        c.periodo?.startsWith(periodo) &&
        c.fechaCobro
      )
      const pago = pagos.length > 0
      const montoPagado = pagos.reduce((s, c) => s + (c.monto || 0), 0)

      const ubi = ubicacionesData.value.find(u => u.idUbicacion === d.idUbicacion)
      grupos[dia].push({
        id: d.id,
        idUbicacion: d.idUbicacion,
        ubicacion: ubi ? `${ubi.calle} ${ubi.numero}` : `Ubic. ${d.idUbicacion}`,
        clave: d.clave,
        inquilino: d.inquilinoCorreo,
        monto: d.montoRenta,
        pago,
        montoPagado,
        fechaPago: pago ? pagos[0].fechaCobro?.split('T')[0] : null
      })
    })

  return Object.entries(grupos)
    .sort((a, b) => Number(a[0]) - Number(b[0]))
    .map(([dia, deptos]) => ({
      dia: Number(dia),
      deptos: deptos.sort((a, b) => a.ubicacion.localeCompare(b.ubicacion) || a.clave.localeCompare(b.clave)),
      pagados: deptos.filter(d => d.pago).length,
      montoTotal: deptos.reduce((s, d) => s + d.monto, 0),
      montoCobrado: deptos.reduce((s, d) => s + (d.pago ? d.monto : 0), 0)
    }))
})

// ─── Modal Capturar Pago ─────────────────────────────────────────────────────
const showPagoModal = ref(false)
const pagoDepto = ref(null)
const pagoForm = ref({ monto: 0, medio: 'Efectivo', fecha: '' })

function abrirPago(d) {
  pagoDepto.value = d
  pagoForm.value = {
    monto: d.monto,
    medio: 'Efectivo',
    fecha: new Date().toISOString().split('T')[0]
  }
  showPagoModal.value = true
}

async function guardarPago() {
  const d = pagoDepto.value
  const now = new Date()
  const periodo = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
  try {
    await api.post('/cobranza', {
      idUbicacion: d.idUbicacion,
      claveDepartamento: d.clave,
      periodo,
      monto: pagoForm.value.monto,
      fechaCobro: pagoForm.value.fecha ? new Date(pagoForm.value.fecha + 'T12:00:00').toISOString() : new Date().toISOString(),
      medio: pagoForm.value.medio
    })
    success(`Pago de $${pagoForm.value.monto.toLocaleString()} registrado para ${d.clave}`)
    showPagoModal.value = false
    // Recargar cobranza
    const res = await api.get('/cobranza')
    cobranzaData.value = res.data
  } catch (e) {
    toastError('Error al guardar: ' + (e.response?.data?.message || e.message))
  }
}

// ─── Helpers ────────────────────────────────────────────────────────────────

const COLORES = [
  '#10b981','#3b82f6','#f59e0b','#8b5cf6','#ef4444',
  '#06b6d4','#ec4899','#84cc16','#f97316','#6366f1',
  '#14b8a6','#e11d48','#0ea5e9','#a855f7','#d97706',
]

function formatMoney(value) {
  return (value || 0).toLocaleString('es-MX', { minimumFractionDigits: 0, maximumFractionDigits: 0 })
}

const mesActualNombre = computed(() => {
  const meses = ['Enero','Febrero','Marzo','Abril','Mayo','Junio','Julio','Agosto','Septiembre','Octubre','Noviembre','Diciembre']
  const now = new Date()
  return `${meses[now.getMonth()]} ${now.getFullYear()}`
})

// ─── Totales mes actual ──────────────────────────────────────────────────────

const totalesMes = computed(() => {
  const now = new Date()
  const mesActual = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
  let cobrado = 0, porCobrar = 0
  cobranzaData.value.forEach(c => {
    if (c.periodo?.startsWith(mesActual)) {
      if (c.fechaCobro) cobrado += c.monto || 0
      else porCobrar += c.monto || 0
    }
  })
  return { cobrado, porCobrar }
})

// ─── Chart: Mes actual ───────────────────────────────────────────────────────

const chartDataMes = computed(() => {
  if (!cobranzaData.value.length) return null
  return {
    labels: ['Mes Actual'],
    datasets: [
      { label: 'Cobrado',     data: [totalesMes.value.cobrado],    backgroundColor: '#10b981', borderRadius: 8 },
      { label: 'Por Cobrar',  data: [totalesMes.value.porCobrar],  backgroundColor: '#ef4444', borderRadius: 8 },
    ]
  }
})

// ─── Chart: Últimos 6 meses ──────────────────────────────────────────────────

const chartData6Meses = computed(() => {
  if (!cobranzaData.value.length) return null
  const mesesNombres = ['Ene','Feb','Mar','Abr','May','Jun','Jul','Ago','Sep','Oct','Nov','Dic']
  const now = new Date()
  const labels = [], cobrados = [], porCobrar = []
  for (let i = 5; i >= 0; i--) {
    const d = new Date(now.getFullYear(), now.getMonth() - i, 1)
    const periodo = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
    labels.push(mesesNombres[d.getMonth()])
    let cob = 0, por = 0
    cobranzaData.value.forEach(c => {
      if (c.periodo?.startsWith(periodo)) {
        if (c.fechaCobro) cob += c.monto || 0
        else por += c.monto || 0
      }
    })
    cobrados.push(cob)
    porCobrar.push(por)
  }
  return {
    labels,
    datasets: [
      { label: 'Cobrado',    data: cobrados,   backgroundColor: '#10b981', borderRadius: 4 },
      { label: 'Por Cobrar', data: porCobrar,  backgroundColor: '#ef4444', borderRadius: 4 },
    ]
  }
})

// ─── Chart: Ingresos por Ubicación ──────────────────────────────────────────

const leyendaUbicacion = computed(() => {
  if (!departamentosData.value.length || !ubicacionesData.value.length) return []
  const mapa = {}
  departamentosData.value.forEach(d => {
    const ubi = ubicacionesData.value.find(u => u.idUbicacion === d.idUbicacion)
    const nombre = ubi ? `${ubi.calle} ${ubi.numero}` : `Ubic. ${d.idUbicacion}`
    mapa[nombre] = (mapa[nombre] || 0) + (d.montoRenta || 0)
  })
  return Object.entries(mapa)
    .sort((a, b) => b[1] - a[1])
    .map(([label, monto], i) => ({ label, monto, color: COLORES[i % COLORES.length] }))
})

const chartDataUbicacion = computed(() => {
  if (!leyendaUbicacion.value.length) return null
  return {
    labels: leyendaUbicacion.value.map(i => i.label),
    datasets: [{
      label: 'Renta mensual total',
      data: leyendaUbicacion.value.map(i => i.monto),
      backgroundColor: leyendaUbicacion.value.map(i => i.color),
      borderRadius: 6,
    }]
  }
})

// ─── Chart: Ingresos por Día de Vencimiento ──────────────────────────────────

const leyendaVencimiento = computed(() => {
  if (!departamentosData.value.length) return []
  const mapa = {}
  departamentosData.value.forEach(d => {
    const dia = d.diaVencimiento || 1
    if (!mapa[dia]) mapa[dia] = { monto: 0, deptos: 0 }
    mapa[dia].monto += d.montoRenta || 0
    mapa[dia].deptos++
  })
  return Object.entries(mapa)
    .sort((a, b) => Number(a[0]) - Number(b[0]))
    .map(([dia, val], i) => ({ dia: Number(dia), monto: val.monto, deptos: val.deptos, color: COLORES[i % COLORES.length] }))
})

const chartDataVencimiento = computed(() => {
  if (!leyendaVencimiento.value.length) return null
  return {
    labels: leyendaVencimiento.value.map(i => `Día ${i.dia}`),
    datasets: [{
      label: 'Renta mensual total',
      data: leyendaVencimiento.value.map(i => i.monto),
      backgroundColor: leyendaVencimiento.value.map(i => i.color),
      borderRadius: 6,
    }]
  }
})

// ─── Chart Options ───────────────────────────────────────────────────────────

const tooltipMoney = {
  callbacks: {
    label: ctx => ` $${formatMoney(ctx.parsed.y)}`
  }
}

const chartOptionsMoney = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: 'bottom', labels: { color: '#9ca3af' } },
    tooltip: tooltipMoney,
  },
  scales: {
    x: { ticks: { color: '#9ca3af' }, grid: { color: '#374151' } },
    y: { ticks: { color: '#9ca3af', callback: v => '$' + formatMoney(v) }, grid: { color: '#374151' } }
  }
}

const chartOptionsUbicacion = {
  ...chartOptionsMoney,
  plugins: {
    ...chartOptionsMoney.plugins,
    legend: { display: false },
    tooltip: tooltipMoney,
  },
  scales: {
    x: { ticks: { color: '#9ca3af', maxRotation: 20 }, grid: { color: '#374151' } },
    y: { ticks: { color: '#9ca3af', callback: v => '$' + formatMoney(v) }, grid: { color: '#374151' } }
  }
}

const chartOptionsVencimiento = {
  ...chartOptionsUbicacion,
}

// ─── Load data ───────────────────────────────────────────────────────────────

onMounted(async () => {
  try {
    const items = []
    if (auth.isPropietario) {
      const [ubRes, depRes, cobRes, tickRes] = await Promise.all([
        api.get('/ubicaciones'),
        api.get('/departamentos'),
        api.get('/cobranza'),
        api.get('/tickets'),
      ])
      ubicacionesData.value = ubRes.data
      departamentosData.value = depRes.data
      cobranzaData.value = cobRes.data
      items.push(
        { label: 'Ubicaciones',      value: ubRes.data.length,                                        color: 'text-blue-400' },
        { label: 'Departamentos',    value: depRes.data.length,                                       color: 'text-emerald-400' },
        { label: 'Cobros registrados', value: cobRes.data.length,                                    color: 'text-yellow-400' },
        { label: 'Tickets abiertos', value: tickRes.data.filter(t => t.estado !== 'Cerrado').length, color: 'text-red-400' },
      )
    } else {
      const tickRes = await api.get('/tickets')
      items.push(
        { label: 'Mis tickets',      value: tickRes.data.length,                                       color: 'text-emerald-400' },
        { label: 'Tickets abiertos', value: tickRes.data.filter(t => t.estado !== 'Cerrado').length,  color: 'text-red-400' },
      )
    }
    stats.value = items
  } catch (e) { console.error(e) }
})
</script>
