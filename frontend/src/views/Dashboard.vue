<template>
  <div class="p-8">
    <h1 class="text-2xl font-bold mb-6">Dashboard</h1>
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
      <div v-for="stat in stats" :key="stat.label" class="bg-gray-900 border border-gray-800 rounded-xl p-6">
        <p class="text-3xl font-bold" :class="stat.color">{{ stat.value }}</p>
        <p class="text-sm text-gray-400 mt-1">{{ stat.label }}</p>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const stats = ref([])

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
