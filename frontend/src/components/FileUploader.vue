<template>
  <div class="mt-4 border-t border-gray-700 pt-4">
    <h3 class="text-sm font-bold text-gray-300 mb-2">📎 Adjuntos</h3>

    <!-- Lista de archivos -->
    <div class="space-y-2 mb-3">
      <div v-for="file in files" :key="file.id" class="flex items-center justify-between bg-gray-800 p-2 rounded text-xs">
        <!-- Enlace cambiado a botón para descarga autenticada -->
        <button @click="downloadFile(file)" class="text-blue-400 hover:underline truncate flex-1 text-left">
          {{ file.filename }}
        </button>
        <button @click="remove(file.id)" class="text-red-400 hover:text-red-300 ml-2" title="Eliminar">🗑️</button>
      </div>
      <p v-if="files.length === 0" class="text-gray-500 text-xs italic">No hay archivos adjuntos.</p>
    </div>

    <!-- Subir nuevo -->
    <div v-if="idPadre" class="flex gap-2">
      <input type="file" ref="fileInput" class="text-xs text-gray-400 file:mr-2 file:py-1 file:px-2 file:rounded file:border-0 file:text-xs file:bg-gray-700 file:text-gray-300 hover:file:bg-gray-600" />
      <button @click="upload" :disabled="uploading" class="px-3 py-1 bg-emerald-600 hover:bg-emerald-700 rounded text-xs font-medium disabled:opacity-50">
        {{ uploading ? '...' : 'Subir' }}
      </button>
    </div>
    <p v-else class="text-yellow-500 text-xs">Guarda el registro primero para adjuntar archivos.</p>
  </div>
</template>

<script setup>
import { ref, onMounted, watch } from 'vue'
import api from '../api'

const props = defineProps({
  tipo: { type: String, required: true },
  idPadre: { type: [Number, String], default: null }
})

const files = ref([])
const fileInput = ref(null)
const uploading = ref(false)

async function load() {
  if (!props.idPadre) {
    files.value = []
    return
  }
  try {
    const res = await api.get(`/adjuntos/${props.tipo}/${props.idPadre}`)
    files.value = res.data
  } catch (e) {
    console.error("Error cargando adjuntos", e)
  }
}

// Descarga autenticada usando Blob
async function downloadFile(file) {
  try {
    const response = await api.get(`/adjuntos/download/${file.id}`, { responseType: 'blob' })
    
    // Crear URL del blob
    const url = window.URL.createObjectURL(new Blob([response.data]))
    const link = document.createElement('a')
    link.href = url
    link.setAttribute('download', file.filename) // Nombre del archivo
    document.body.appendChild(link)
    link.click()
    
    // Limpieza
    document.body.removeChild(link)
    window.URL.revokeObjectURL(url)
  } catch (e) {
    console.error("Error descargando archivo", e)
    alert("Error al descargar el archivo. Verifica tu sesión.")
  }
}

async function upload() {
  const file = fileInput.value?.files[0]
  if (!file) return

  uploading.value = true
  const formData = new FormData()
  formData.append('file', file)

  try {
    await api.post(`/adjuntos/${props.tipo}/${props.idPadre}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' }
    })
    fileInput.value.value = '' // Reset input
    await load()
  } catch (e) {
    alert('Error al subir archivo')
  } finally {
    uploading.value = false
  }
}

async function remove(id) {
  if (!confirm('¿Eliminar archivo?')) return
  try {
    await api.delete(`/adjuntos/${id}`)
    await load()
  } catch (e) {
    alert('Error al eliminar')
  }
}

// Recargar cuando cambia el ID padre (ej: al navegar entre registros)
watch(() => props.idPadre, load)
onMounted(load)
</script>
