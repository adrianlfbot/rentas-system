<template>
  <div class="p-8">
    <h1 class="text-2xl font-bold mb-6">Tablero de Cobro</h1>

    <div class="mb-4">
      <input v-model="periodo" type="month" @change="load" class="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">Departamento</th>
            <th class="px-4 py-3 text-center text-gray-400">Pagado</th>
            <th class="px-4 py-3 text-left text-gray-400">Fecha de Pago</th>
          </tr>
        </thead>
        <tbody>
          <template v-for="(group, ubicacion) in grouped" :key="ubicacion">
            <tr class="bg-gray-800/30">
              <td colspan="3" class="px-4 py-2 text-emerald-400 font-semibold">📍 {{ ubicacion }}</td>
            </tr>
            <tr v-for="item in group" :key="item.clave" class="border-t border-gray-800 hover:bg-gray-800/20">
              <td class="px-4 py-3 pl-8">
                <span class="font-mono">{{ item.clave }}</span>
                <span v-if="item.inquilino" class="text-gray-500 ml-2">— {{ item.inquilino }}</span>
                <span v-else class="text-gray-600 ml-2">— Vacío</span>
              </td>
              <td class="px-4 py-3 text-center text-2xl">
                <span v-if="item.pagado" class="text-emerald-400">✅</span>
                <span v-else class="text-red-400">❌</span>
              </td>
              <td class="px-4 py-3">
                {{ item.fechaPago ? item.fechaPago.split('T')[0] : '—' }}
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import api from '../api'

const now = new Date()
const periodo = ref(`${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`)
const items = ref([])

const grouped = computed(() => {
  const groups = {}
  for (const item of items.value) {
    if (!groups[item.ubicacion]) groups[item.ubicacion] = []
    groups[item.ubicacion].push(item)
  }
  return groups
})

async function load() {
  if (!periodo.value) return
  items.value = (await api.get(`/cobranza/tablero?periodo=${periodo.value}`)).data
}

onMounted(load)
</script>
