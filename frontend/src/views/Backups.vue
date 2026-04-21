<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">💾 Backups</h1>
      <button @click="createBackup" :disabled="creating" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium disabled:opacity-50">
        {{ creating ? 'Creando...' : '+ Crear Backup' }}
      </button>
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-hidden">
      <table class="w-full text-sm">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">Archivo</th>
            <th class="px-4 py-3 text-left text-gray-400">Fecha</th>
            <th class="px-4 py-3 text-left text-gray-400">Tamaño</th>
            <th class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="b in backups" :key="b.filename" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3 font-mono text-xs">{{ b.filename }}</td>
            <td class="px-4 py-3">{{ b.date }}</td>
            <td class="px-4 py-3">{{ b.size }}</td>
            <td class="px-4 py-3 space-x-2">
              <button @click="download(b)" class="text-blue-400 hover:text-blue-300" title="Descargar">📥</button>
              <button @click="restore(b)" class="text-yellow-400 hover:text-yellow-300" title="Restaurar">🔄</button>
              <button @click="remove(b)" class="text-red-400 hover:text-red-300" title="Eliminar">🗑️</button>
            </td>
          </tr>
          <tr v-if="backups.length === 0">
            <td colspan="4" class="px-4 py-8 text-center text-gray-500">No hay backups disponibles</td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="mt-6 p-4 bg-yellow-900/20 border border-yellow-800 rounded-lg text-sm">
      <p class="text-yellow-400 font-bold mb-2">⚠️ Advertencia</p>
      <p class="text-gray-300">Restaurar un backup reemplazará todos los datos actuales. Esta acción no se puede deshacer.</p>
    </div>

    <!-- Modal Restaurar -->
    <div v-if="showRestoreModal" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-md">
        <h2 class="text-lg font-bold mb-4 text-yellow-400">🔄 Restaurar Backup</h2>
        <p class="text-gray-300 mb-4">¿Estás seguro de restaurar <strong>{{ selectedBackup?.filename }}</strong>?</p>
        <p class="text-red-400 text-sm mb-4">Esto reemplazará TODOS los datos actuales.</p>
        <div class="mb-4">
          <label class="text-sm text-gray-400">Escribe "RESTAURAR" para confirmar:</label>
          <input v-model="confirmText" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm mt-1" />
        </div>
        <div class="flex gap-2 justify-end">
          <button @click="showRestoreModal = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
          <button @click="confirmRestore" :disabled="confirmText !== 'RESTAURAR' || restoring" 
            class="px-4 py-2 bg-yellow-600 hover:bg-yellow-700 rounded-lg text-sm font-medium disabled:opacity-50">
            {{ restoring ? 'Restaurando...' : 'Restaurar' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { useToast } from '../composables/useToast'
import { useConfirm } from '../composables/useConfirm'
const { success, error: toastError } = useToast()
const { confirm: confirmDialog } = useConfirm()
import api from '../api'

const backups = ref([])
const creating = ref(false)
const restoring = ref(false)
const showRestoreModal = ref(false)
const selectedBackup = ref(null)
const confirmText = ref('')

async function load() {
  try {
    const res = await api.get('/backups')
    backups.value = res.data
  } catch (e) {
    console.error('Error cargando backups:', e)
  }
}

onMounted(load)

async function createBackup() {
  creating.value = true
  try {
    await api.post('/backups')
    success('Backup creado exitosamente')
    await load()
  } catch (e) {
    toastError('Error al crear backup: ' + (e.response?.data?.message || e.message))
  } finally {
    creating.value = false
  }
}

async function download(b) {
  try {
    const response = await api.get(`/backups/download/${b.filename}`, { responseType: 'blob' })
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', b.filename)
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch (e) {
    toastError('Error al descargar: ' + e.message)
  }
}

function restore(b) {
  selectedBackup.value = b
  confirmText.value = ''
  showRestoreModal.value = true
}

async function confirmRestore() {
  if (confirmText.value !== 'RESTAURAR') return
  restoring.value = true
  try {
    await api.post(`/backups/restore/${selectedBackup.value.filename}`)
    success('Backup restaurado exitosamente. La página se recargará.')
    window.location.reload()
  } catch (e) {
    toastError('Error al restaurar: ' + (e.response?.data?.message || e.message))
  } finally {
    restoring.value = false
    showRestoreModal.value = false
  }
}

async function remove(b) {
  if (!await confirmDialog({ title: '¿Eliminar backup?', message: `Se eliminará ${b.filename} permanentemente.` })) return
  try {
    await api.delete(`/backups/${b.filename}`)
    await load()
  } catch (e) {
    toastError('Error al eliminar: ' + e.message)
  }
}
</script>
