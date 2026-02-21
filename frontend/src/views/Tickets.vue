<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Tickets de Mantenimiento</h1>
      <button @click="openNew" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">+ Nuevo Ticket</button>
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">ID</th>
            <th class="px-4 py-3 text-left text-gray-400">Fecha</th>
            <th class="px-4 py-3 text-left text-gray-400">Creado por</th>
            <th class="px-4 py-3 text-left text-gray-400">Prioridad</th>
            <th class="px-4 py-3 text-left text-gray-400">Descripción</th>
            <th class="px-4 py-3 text-left text-gray-400">Estado</th>
            <th v-if="auth.isPropietario" class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="t in items" :key="t.id" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3">{{ t.id }}</td>
            <td class="px-4 py-3">{{ t.fechaCreacion?.split('T')[0] }}</td>
            <td class="px-4 py-3">{{ t.usuarioCreo }}</td>
            <td class="px-4 py-3">
              <span :class="prioridadColor(t.prioridad)" class="px-2 py-1 rounded text-xs font-medium">{{ t.prioridad }}</span>
            </td>
            <td class="px-4 py-3 max-w-xs truncate">{{ t.descripcion }}</td>
            <td class="px-4 py-3">
              <span :class="estadoColor(t.estado)" class="px-2 py-1 rounded text-xs font-medium">{{ t.estado }}</span>
            </td>
            <td v-if="auth.isPropietario" class="px-4 py-3 space-x-2">
              <button @click="edit(t)" class="text-blue-400">✏️</button>
              <button @click="remove(t.id)" class="text-red-400">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-lg">
        <h2 class="text-lg font-bold mb-4">{{ form.id ? 'Editar' : 'Nuevo' }} Ticket</h2>
        <form @submit.prevent="save" class="space-y-3">
          <select v-model="form.prioridad" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option>Alta</option><option>Media</option><option>Baja</option>
          </select>
          <textarea v-model="form.descripcion" placeholder="Descripción del problema" rows="4" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm"></textarea>
          <select v-if="auth.isPropietario && form.id" v-model="form.estado" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option>Abierto</option><option>EnProgreso</option><option>Cerrado</option>
          </select>
          <div class="flex gap-2 justify-end mt-4">
            <button type="button" @click="showForm = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
            <button type="submit" class="px-4 py-2 bg-emerald-600 rounded-lg text-sm font-medium">Guardar</button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import api from '../api'
import { useAuthStore } from '../stores/auth'

const auth = useAuthStore()
const items = ref([])
const showForm = ref(false)
const form = ref({ prioridad: 'Media', descripcion: '', estado: 'Abierto' })

async function load() { items.value = (await api.get('/tickets')).data }
onMounted(load)

function openNew() { form.value = { prioridad: 'Media', descripcion: '', estado: 'Abierto' }; showForm.value = true }
function edit(t) { form.value = { ...t }; showForm.value = true }

async function save() {
  if (form.value.id) await api.put(`/tickets/${form.value.id}`, form.value)
  else await api.post('/tickets', form.value)
  showForm.value = false; await load()
}

async function remove(id) {
  if (confirm('¿Eliminar ticket?')) { await api.delete(`/tickets/${id}`); await load() }
}

function prioridadColor(p) {
  return { Alta: 'bg-red-500/20 text-red-400', Media: 'bg-yellow-500/20 text-yellow-400', Baja: 'bg-blue-500/20 text-blue-400' }[p] || ''
}
function estadoColor(e) {
  return { Abierto: 'bg-red-500/20 text-red-400', EnProgreso: 'bg-yellow-500/20 text-yellow-400', Cerrado: 'bg-emerald-500/20 text-emerald-400' }[e] || ''
}
</script>
