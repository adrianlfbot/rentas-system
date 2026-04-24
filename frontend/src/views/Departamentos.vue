<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Departamentos</h1>
      <div class="flex gap-2">
        <button @click="exportarCsv" class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm font-medium">⬇ Exportar CSV</button>
        <label class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm font-medium cursor-pointer">
          ⬆ Importar CSV
          <input type="file" accept=".csv" class="hidden" @change="importarCsv" />
        </label>
        <button @click="openNew" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium transition-colors">+ Nuevo</button>
      </div>
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-x-auto">
      <table class="w-full text-sm min-w-[800px]">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">Ubicación</th>
            <th class="px-4 py-3 text-left text-gray-400">Clave</th>
            <th class="px-4 py-3 text-left text-gray-400">Descripción</th>
            <th class="px-4 py-3 text-left text-gray-400">Renta</th>
            <th class="px-4 py-3 text-left text-gray-400">Día Cobro</th>
            <th class="px-4 py-3 text-left text-gray-400">Contrato Luz</th>
            <th class="px-4 py-3 text-left text-gray-400">Inquilino</th>
            <th class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="d in items" :key="d.id" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3">{{ d.ubicacion?.calle }} {{ d.ubicacion?.numero }}</td>
            <td class="px-4 py-3 font-mono">{{ d.clave }}</td>
            <td class="px-4 py-3">{{ d.descripcion }}</td>
            <td class="px-4 py-3">${{ d.montoRenta?.toLocaleString() }}</td>
            <td class="px-4 py-3 text-center">{{ d.diaVencimiento || '—' }}</td>
            <td class="px-4 py-3">
              <span v-if="d.contratoLuz" class="text-yellow-400">⚡ {{ d.contratoLuz.nombre }}</span>
              <span v-else class="text-gray-600">—</span>
            </td>
            <td class="px-4 py-3">
              <span v-if="d.inquilinoCorreo" class="text-emerald-400">{{ d.inquilinoCorreo }}</span>
              <span v-else class="text-gray-500">Vacío</span>
            </td>
            <td class="px-4 py-3 space-x-2 flex">
              <button @click="openNotas(d)" class="text-yellow-400 hover:text-yellow-300" title="Notas">📝</button>
              <button @click="edit(d)" class="text-blue-400 hover:text-blue-300" title="Editar">✏️</button>
              <button @click="remove(d.id)" class="text-red-400 hover:text-red-300" title="Eliminar">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal Form -->
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <h2 class="text-lg font-bold mb-4">{{ form.id ? 'Editar' : 'Nuevo' }} Departamento</h2>
        <form @submit.prevent="save" class="space-y-3">
          <select v-model="form.idUbicacion" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option value="" disabled>Ubicación</option>
            <option v-for="u in ubicaciones" :key="u.idUbicacion" :value="u.idUbicacion">{{ u.calle }} {{ u.numero }}</option>
          </select>
          <input v-model="form.clave" placeholder="Clave (A, B, 1, 2...)" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <input v-model="form.descripcion" placeholder="Descripción" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <div class="grid grid-cols-3 gap-2">
            <input v-model.number="form.cuartos" type="number" placeholder="Cuartos" class="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
            <input v-model.number="form.banos" type="number" placeholder="Baños" class="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
            <input v-model.number="form.estacionamiento" type="number" placeholder="Estac." class="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          </div>
          <input v-model="form.extras" placeholder="Extras" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <div class="grid grid-cols-2 gap-2">
            <input v-model.number="form.montoRenta" type="number" placeholder="Renta $" class="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
            <input v-model.number="form.cuotaAgua" type="number" placeholder="Agua $" class="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          </div>
          <select v-model="form.contratoLuzId" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option :value="null">Sin Contrato de Luz propio</option>
            <option v-for="c in contratosLuz" :key="c.id" :value="c.id">⚡ {{ c.nombre }} ({{ c.rpu }})</option>
          </select>
          <input v-model.number="form.diaVencimiento" type="number" min="1" max="31" placeholder="Día vencimiento (1-31)" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <textarea v-model="form.descripcionPublicacion" placeholder="Descripción para publicación (con emojis)" rows="3" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500"></textarea>
          <input v-model="form.inquilinoCorreo" placeholder="Correo inquilino (vacío = disponible)" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <div class="flex gap-2 justify-end mt-4">
            <button type="button" @click="showForm = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
            <button type="submit" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">Guardar</button>
          </div>
        </form>

        <!-- Adjuntos -->
        <FileUploader v-if="form.id" tipo="Departamento" :id-padre="form.id" />
      </div>
    </div>

    <!-- Modal Notas -->
    <div v-if="showNotas" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-md max-h-[80vh] flex flex-col">
        <div class="flex justify-between items-center mb-4">
          <h2 class="text-lg font-bold">📝 Notas - {{ activeDepto?.clave }}</h2>
          <button @click="showNotas = false" class="text-gray-400 hover:text-white">✕</button>
        </div>
        
        <div class="flex-1 overflow-y-auto space-y-3 mb-4 pr-2">
          <div v-for="n in notas" :key="n.id" class="bg-gray-800 p-3 rounded-lg text-sm relative group">
            <p>{{ n.texto }}</p>
            <div class="text-xs text-gray-500 mt-2 flex justify-between">
              <span>{{ new Date(n.fecha).toLocaleString() }}</span>
              <span>{{ n.usuarioCreo }}</span>
            </div>
            <button @click="deleteNota(n.id)" class="absolute top-2 right-2 text-red-400 opacity-0 group-hover:opacity-100 hover:text-red-300">🗑️</button>
          </div>
          <p v-if="notas.length === 0" class="text-center text-gray-500 py-4">No hay notas registradas.</p>
        </div>

        <form @submit.prevent="addNota" class="flex gap-2">
          <input v-model="newNota" placeholder="Escribir nota..." required class="flex-1 px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <button type="submit" class="px-3 py-2 bg-blue-600 hover:bg-blue-700 rounded-lg">➤</button>
        </form>
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
import FileUploader from '../components/FileUploader.vue'

const items = ref([])
const ubicaciones = ref([])
const contratosLuz = ref([])
const showForm = ref(false)
const showNotas = ref(false)
const notas = ref([])
const activeDepto = ref(null)
const newNota = ref('')

const emptyForm = { idUbicacion: '', clave: '', descripcion: '', cuartos: 0, banos: 0, estacionamiento: 0, extras: '', montoRenta: 0, cuotaAgua: 0, contratoLuzId: null, diaVencimiento: 1, descripcionPublicacion: '', inquilinoCorreo: '' }
const form = ref({ ...emptyForm })

async function load() {
  const [resDeptos, resUbi, resLuz] = await Promise.all([
    api.get('/departamentos'),
    api.get('/ubicaciones'),
    api.get('/contratos/luz')
  ])
  items.value = resDeptos.data
  ubicaciones.value = resUbi.data
  contratosLuz.value = resLuz.data
}
onMounted(load)

function openNew() { form.value = { ...emptyForm }; showForm.value = true }
function edit(d) { form.value = { ...d }; showForm.value = true }

async function save() {
  try {
    // Normalizar campos opcionales: string vacío → null
    const payload = { ...form.value }
    if (!payload.contratoLuzId) payload.contratoLuzId = null
    if (!payload.inquilinoCorreo?.trim()) payload.inquilinoCorreo = null

    if (form.value.id) {
      await api.put(`/departamentos/${form.value.id}`, payload)
    } else {
      const res = await api.post('/departamentos', payload)
      // Asignar ID para que aparezca el uploader sin cerrar
      form.value.id = res.data.id
    }
    success('Departamento guardado.')
    showForm.value = false
    await load()
  } catch (e) {
    const msg = e.response?.data?.message || e.response?.data || e.message || 'Error desconocido'
    toastError('Error al guardar: ' + msg)
    console.error('Error guardando departamento:', e)
  }
}

async function remove(id) {
  if (await confirmDialog({ title: '¿Eliminar departamento?', message: 'Se eliminará el departamento permanentemente.' })) {
    try { await api.delete(`/departamentos/${id}`); await load(); success('Departamento eliminado.') }
    catch (err) { toastError(err.response?.data || err.message) }
  }
}

// === EXPORTAR CSV ===
async function exportarCsv() {
  const res = await api.get('/departamentos/exportar', { responseType: 'blob' })
  const url = URL.createObjectURL(new Blob([res.data]))
  const a = document.createElement('a')
  a.href = url; a.download = 'departamentos.csv'; a.click()
  URL.revokeObjectURL(url)
}

// === IMPORTAR CSV ===
async function importarCsv(e) {
  const file = e.target.files[0]
  if (!file) return
  const fd = new FormData()
  fd.append('archivo', file)
  try {
    const res = await api.post('/departamentos/importar', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    const { insertados, actualizados, errores, detalle } = res.data
    const msg = `Importado: ${insertados} nuevos, ${actualizados} actualizados, ${errores} errores.${ detalle?.length ? '\n' + detalle.join('\n') : ''}`
    errores > 0 ? toastError(msg) : success(msg)
    await load()
  } catch (err) {
    toastError('Error al importar: ' + (err.response?.data || err.message))
  } finally {
    e.target.value = ''
  }
}

async function openNotas(d) {
  activeDepto.value = d
  notas.value = (await api.get(`/departamentos/${d.id}/notas`)).data
  showNotas.value = true
}

async function addNota() {
  if (!newNota.value.trim()) return
  const res = await api.post(`/departamentos/${activeDepto.value.id}/notas`, { texto: newNota.value })
  notas.value.unshift(res.data)
  newNota.value = ''
}

async function deleteNota(id) {
  if (await confirmDialog({ title: '¿Borrar nota?', message: 'La nota será eliminada.', confirmLabel: 'Borrar' })) {
    await api.delete(`/departamentos/notas/${id}`)
    notas.value = notas.value.filter(n => n.id !== id)
  }
}
</script>
