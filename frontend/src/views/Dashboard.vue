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

    <!-- Charts Section -->
    <div v-if="auth.isPropietario" class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Chart: Mes Actual -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 class="text-lg font-bold mb-4">📊 {{ mesActualNombre }}</h2>
        <div class="h-64">
          <Bar v-if="chartDataMes" :data="chartDataMes" :options="chartOptions" />
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

      <!-- Chart: Últimos 6 meses -->
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <h2 class="text-lg font-bold mb-4">📈 Últimos 6 Meses</h2>
        <div class="h-64">
          <Bar v-if="chartData6Meses" :data="chartData6Meses" :options="chartOptions6Meses" />
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

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend)

const auth = useAuthStore()
const stats = ref([])
const cobranzaData = ref([])

const mesActualNombre = computed(() => {
  const meses = ['Enero', 'Febrero', 'Marzo', 'Abril', 'Mayo', 'Junio', 'Julio', 'Agosto', 'Septiembre', 'Octubre', 'Noviembre', 'Diciembre']
  const now = new Date()
  return `${meses[now.getMonth()]} ${now.getFullYear()}`
})

const totalesMes = computed(() => {
  const now = new Date()
  const mesActual = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
  
  let cobrado = 0
  let porCobrar = 0
  
  cobranzaData.value.forEach(c => {
    if (c.periodo?.startsWith(mesActual)) {
      if (c.fechaCobro) {
        cobrado += c.monto || 0
      } else {
        porCobrar += c.monto || 0
      }
    }
  })
  
  return { cobrado, porCobrar }
})

const chartDataMes = computed(() => {
  if (!cobranzaData.value.length) return null
  
  return {
    labels: ['Mes Actual'],
    datasets: [
      {
        label: 'Cobrado',
        data: [totalesMes.value.cobrado],
        backgroundColor: '#10b981',
        borderRadius: 8,
      },
      {
        label: 'Por Cobrar',
        data: [totalesMes.value.porCobrar],
        backgroundColor: '#ef4444',
        borderRadius: 8,
      }
    ]
  }
})

const chartData6Meses = computed(() => {
  if (!cobranzaData.value.length) return null
  
  const mesesNombres = ['Ene', 'Feb', 'Mar', 'Abr', 'May', 'Jun', 'Jul', 'Ago', 'Sep', 'Oct', 'Nov', 'Dic']
  const now = new Date()
  const labels = []
  const cobrados = []
  const porCobrar = []
  
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
      {
        label: 'Cobrado',
        data: cobrados,
        backgroundColor: '#10b981',
        borderRadius: 4,
      },
      {
        label: 'Por Cobrar',
        data: porCobrar,
        backgroundColor: '#ef4444',
        borderRadius: 4,
      }
    ]
  }
})

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom',
      labels: { color: '#9ca3af' }
    }
  },
  scales: {
    x: {
      ticks: { color: '#9ca3af' },
      grid: { color: '#374151' }
    },
    y: {
      ticks: { 
        color: '#9ca3af',
        callback: (value) => '$' + value.toLocaleString()
      },
      grid: { color: '#374151' }
    }
  }
}

const chartOptions6Meses = {
  ...chartOptions,
  plugins: {
    ...chartOptions.plugins,
    legend: {
      position: 'bottom',
      labels: { color: '#9ca3af' }
    }
  }
}

function formatMoney(value) {
  return value.toLocaleString('es-MX', { minimumFractionDigits: 0, maximumFractionDigits: 0 })
}

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
      
      cobranzaData.value = cobRes.data
      
      items.push(
        { label: 'Ubicaciones', value: ubRes.data.length, color: 'text-blue-400' },
        { label: 'Departamentos', value: depRes.data.length, color: 'text-emerald-400' },
        { label: 'Cobros registrados', value: cobRes.data.length, color: 'text-yellow-400' },
        { label: 'Tickets abiertos', value: tickRes.data.filter(t => t.estado !== 'Cerrado').length, color: 'text-red-400' },
      )
    } else {
      const tickRes = await api.get('/tickets')
      items.push(
        { label: 'Mis tickets', value: tickRes.data.length, color: 'text-emerald-400' },
        { label: 'Tickets abiertos', value: tickRes.data.filter(t => t.estado !== 'Cerrado').length, color: 'text-red-400' },
      )
    }
    stats.value = items
  } catch (e) { console.error(e) }
})
</script>
