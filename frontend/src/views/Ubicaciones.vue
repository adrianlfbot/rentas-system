<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Ubicaciones</h1>
      <button @click="showForm = true" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium transition-colors">+ Nueva</button>
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-x-auto">
      <table class="w-full text-sm min-w-[800px]">
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
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <h2 class="text-lg font-bold mb-4">{{ form.idUbicacion ? 'Editar' : 'Nueva' }} Ubicación</h2>
        <form @submit.prevent="save" class="space-y-3">
          <input v-model="form.calle" placeholder="Calle" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <input v-model="form.numero" placeholder="Número" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <input v-model="form.propietario" placeholder="Propietario" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          <input v-model="form.numeroPredial" placeholder="Número de Predial" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
          
          <!-- Contratos -->
          <div class="border-t border-gray-700 pt-3 mt-3">
            <h3 class="text-sm font-bold text-gray-400 mb-2">📋 Contratos</h3>
            <select v-model="form.contratoLuzId" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm mb-2">
              <option :value="null">⚡ Sin contrato de luz</option>
              <option v-for="c in contratosLuz" :key="c.id" :value="c.id">⚡ {{ c.nombre }} ({{ c.rpu }})</option>
            </select>
            <select v-model="form.contratoAguaId" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm mb-2">
              <option :value="null">💧 Sin contrato de agua</option>
              <option v-for="c in contratosAgua" :key="c.id" :value="c.id">💧 {{ c.nombre }} ({{ c.numeroContrato }})</option>
            </select>
            <select v-model="form.contratoInternetId" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
              <option :value="null">🌐 Sin contrato de internet</option>
              <option v-for="c in contratosInternet" :key="c.id" :value="c.id">🌐 {{ c.nombre }} ({{ c.numeroContrato }})</option>
            </select>
          </div>

          <div class="flex gap-2 justify-end mt-4">
            <button type="button" @click="showForm = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
            <button type="submit" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">Guardar</button>
          </div>
        </form>

        <!-- Adjuntos -->
        <FileUploader v-if="form.idUbicacion" tipo="Ubicacion" :id-padre="form.idUbicacion" />
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
const showForm = ref(false)
const form = ref({ calle: '', numero: '', propietario: '', numeroPredial: '', contratoLuzId: null, contratoAguaId: null, contratoInternetId: null })
const contratosLuz = ref([])
const contratosAgua = ref([])
const contratosInternet = ref([])

async function load() {
  const [resUbi, resLuz, resAgua, resInternet] = await Promise.all([
    api.get('/ubicaciones'),
    api.get('/contratos/luz'),
    api.get('/contratos/agua'),
    api.get('/contratos/internet')
  ])
  items.value = resUbi.data
  contratosLuz.value = resLuz.data
  contratosAgua.value = resAgua.data
  contratosInternet.value = resInternet.data
}
onMounted(load)

function edit(u) {
  form.value = { ...u }
  showForm.value = true
}

async function save() {
  if (form.value.idUbicacion) {
    await api.put(`/ubicaciones/${form.value.idUbicacion}`, form.value)
  } else {
    const res = await api.post('/ubicaciones', form.value)
    form.value.idUbicacion = res.data.idUbicacion
  }
  success('Ubicación guardada correctamente.')
  await load()
}

async function remove(id) {
  if (await confirmDialog({ title: '¿Eliminar ubicación?', message: 'Se eliminará la ubicación permanentemente.' })) {
    try { await api.delete(`/ubicaciones/${id}`); await load(); success('Ubicación eliminada.') }
    catch (err) { toastError(err.response?.data || err.message) }
  }
}
</script>
