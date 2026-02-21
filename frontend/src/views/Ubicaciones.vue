<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Ubicaciones</h1>
      <button @click="showForm = true" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium transition-colors">+ Nueva</button>
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">ID</th>
            <th class="px-4 py-3 text-left text-gray-400">Calle</th>
            <th class="px-4 py-3 text-left text-gray-400">Número</th>
            <th class="px-4 py-3 text-left text-gray-400">Propietario</th>
            <th class="px-4 py-3 text-left text-gray-400">Predial</th>
            <th class="px-4 py-3 text-left text-gray-400">Deptos</th>
            <th class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in items" :key="u.idUbicacion" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3">{{ u.idUbicacion }}</td>
            <td class="px-4 py-3">{{ u.calle }}</td>
            <td class="px-4 py-3">{{ u.numero }}</td>
            <td class="px-4 py-3">{{ u.propietario }}</td>
            <td class="px-4 py-3">{{ u.numeroPredial }}</td>
            <td class="px-4 py-3">{{ u.departamentos?.length || 0 }}</td>
            <td class="px-4 py-3 space-x-2">
              <button @click="edit(u)" class="text-blue-400 hover:text-blue-300">✏️</button>
              <button @click="remove(u.idUbicacion)" class="text-red-400 hover:text-red-300">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-lg">
        <h2 class="text-lg font-bold mb-4">{{ form.idUbicacion ? 'Editar' : 'Nueva' }} Ubicación</h2>
        <form @submit.prevent="save" class="space-y-3">
          <input v-model="form.calle" placeholder="Calle" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <input v-model="form.numero" placeholder="Número" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <input v-model="form.propietario" placeholder="Propietario" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <input v-model="form.numeroPredial" placeholder="Número de Predial" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <div class="flex gap-2 justify-end mt-4">
            <button type="button" @click="showForm = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
            <button type="submit" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">Guardar</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'

const items = ref([])
const showForm = ref(false)
const form = ref({ calle: '', numero: '', propietario: '', numeroPredial: '' })

async function load() { items.value = (await api.get('/ubicaciones')).data }
onMounted(load)

function edit(u) {
  form.value = { ...u }
  showForm.value = true
}

async function save() {
  if (form.value.idUbicacion) {
    await api.put(`/ubicaciones/${form.value.idUbicacion}`, form.value)
  } else {
    await api.post('/ubicaciones', form.value)
  }
  showForm.value = false
  form.value = { calle: '', numero: '', propietario: '', numeroPredial: '' }
  await load()
}

async function remove(id) {
  if (confirm('¿Eliminar ubicación?')) { await api.delete(`/ubicaciones/${id}`); await load() }
}
</script>
