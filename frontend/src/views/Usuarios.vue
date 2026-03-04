<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Usuarios</h1>
      <button @click="openNew" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">+ Nuevo</button>
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">Correo</th>
            <th class="px-4 py-3 text-left text-gray-400">Tipo</th>
            <th class="px-4 py-3 text-left text-gray-400">Teléfono</th>
            <th class="px-4 py-3 text-left text-gray-400">Último Acceso</th>
            <th class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="u in items" :key="u.correo" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3">{{ u.correo }}</td>
            <td class="px-4 py-3">
              <span :class="u.tipo === 'Propietario' ? 'text-emerald-400' : 'text-blue-400'">{{ u.tipo }}</span>
            </td>
            <td class="px-4 py-3">{{ u.telefono || '—' }}</td>
            <td class="px-4 py-3">{{ u.fechaUltimoAcceso?.split('T')[0] || '—' }}</td>
            <td class="px-4 py-3 space-x-2">
              <button @click="edit(u)" class="text-blue-400">✏️</button>
              <button @click="remove(u.correo)" class="text-red-400">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-lg">
        <h2 class="text-lg font-bold mb-4">{{ isEdit ? 'Editar' : 'Nuevo' }} Usuario</h2>
        <form @submit.prevent="save" class="space-y-3">
          <input v-model="form.correo" type="email" placeholder="Correo" :disabled="isEdit" :required="!isEdit"
            class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm disabled:opacity-50" />
          <input v-model="form.password" type="password" :placeholder="isEdit ? 'Nueva contraseña (dejar vacío para no cambiar)' : 'Contraseña'" :required="!isEdit"
            class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <select v-model="form.tipo" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option>Propietario</option><option>Inquilino</option>
          </select>
          <input v-model="form.telefono" placeholder="Teléfono" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <!-- INE Upload -->
          <div class="space-y-2">
            <label class="text-sm text-gray-400">INE (Identificación)</label>
            <div v-if="form.ine" class="flex items-center gap-2 bg-gray-800 p-2 rounded-lg">
              <span class="text-emerald-400 text-sm">✅ INE adjunto (ID: {{ form.ine }})</span>
              <button type="button" @click="viewINE" class="text-blue-400 text-sm hover:underline">Ver</button>
              <button type="button" @click="form.ine = null" class="text-red-400 text-sm">Quitar</button>
            </div>
            <div v-else class="flex gap-2">
              <input type="file" ref="ineInput" accept="image/*,.pdf" 
                class="text-xs text-gray-400 file:mr-2 file:py-1 file:px-2 file:rounded file:border-0 file:text-xs file:bg-gray-700 file:text-gray-300 hover:file:bg-gray-600" />
              <button type="button" @click="uploadINE" :disabled="uploadingINE" 
                class="px-3 py-1 bg-blue-600 hover:bg-blue-700 rounded text-xs font-medium disabled:opacity-50">
                {{ uploadingINE ? 'Subiendo...' : '📎 Adjuntar' }}
              </button>
            </div>
          </div>
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

const items = ref([])
const showForm = ref(false)
const isEdit = ref(false)
const form = ref({ correo: '', password: '', tipo: 'Inquilino', telefono: '', ine: null })
const ineInput = ref(null)
const uploadingINE = ref(false)

async function load() { items.value = (await api.get('/usuarios')).data }
onMounted(load)

function openNew() { isEdit.value = false; form.value = { correo: '', password: '', tipo: 'Inquilino', telefono: '', ine: null }; showForm.value = true }
function edit(u) { isEdit.value = true; form.value = { ...u, password: '', ine: u.ine || null }; showForm.value = true }

async function uploadINE() {
  const file = ineInput.value?.files[0]
  if (!file) return alert('Selecciona un archivo')
  
  uploadingINE.value = true
  const formData = new FormData()
  formData.append('file', file)
  
  try {
    // Subir como adjunto tipo "INE" con idPadre temporal 0
    const res = await api.post('/adjuntos/INE/0', formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    form.value.ine = res.data.id
    ineInput.value.value = ''
  } catch (e) {
    alert('Error al subir INE')
    console.error(e)
  } finally {
    uploadingINE.value = false
  }
}

async function viewINE() {
  if (!form.value.ine) return
  try {
    const response = await api.get(`/adjuntos/download/${form.value.ine}`, { responseType: 'blob' })
    const url = window.URL.createObjectURL(new Blob([response.data]))
    window.open(url, '_blank')
  } catch (e) {
    alert('Error al ver INE')
  }
}

async function save() {
  if (isEdit.value) await api.put(`/usuarios/${form.value.correo}`, form.value)
  else await api.post('/usuarios', form.value)
  showForm.value = false; await load()
}

async function remove(correo) {
  if (confirm('¿Eliminar usuario?')) { await api.delete(`/usuarios/${correo}`); await load() }
}
</script>
