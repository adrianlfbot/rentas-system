<template>
  <div class="p-8">
    <h1 class="text-2xl font-bold mb-6">Contratos de Servicios</h1>

    <!-- Tabs -->
    <div class="flex gap-2 mb-6">
      <button v-for="tab in tabs" :key="tab.key" @click="activeTab = tab.key"
        :class="activeTab === tab.key ? 'bg-emerald-600 text-white' : 'bg-gray-800 text-gray-400 hover:bg-gray-700'"
        class="px-4 py-2 rounded-lg text-sm font-medium transition-colors">{{ tab.label }}</button>
    </div>

    <!-- Table -->
    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-x-auto">
      <div class="p-4 flex justify-end gap-2">
        <template v-if="activeTab === 'luz'">
          <button @click="exportarCsv" class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm font-medium">⬇ Exportar CSV</button>
          <label class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm font-medium cursor-pointer">
            ⬆ Importar CSV
            <input type="file" accept=".csv" class="hidden" @change="importarCsv" />
          </label>
          <label class="px-4 py-2 bg-yellow-700 hover:bg-yellow-600 rounded-lg text-sm font-medium cursor-pointer">
            📄 Importar Recibos XML
            <input type="file" accept=".xml" multiple class="hidden" @change="importarXml" />
          </label>
        </template>
        <button @click="openNew" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">+ Nuevo</button>
      </div>
      <table class="w-full text-sm min-w-[800px]">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">ID</th>
            <th class="px-4 py-3 text-left text-gray-400">Nombre</th>
            <th v-if="activeTab === 'luz'" class="px-4 py-3 text-left text-gray-400">Email</th>
            <th v-for="col in extraCols" :key="col" class="px-4 py-3 text-left text-gray-400">{{ col }}</th>
            <th class="px-4 py-3 text-left text-gray-400">Vencimiento</th>
            <th class="px-4 py-3 text-left text-gray-400">Periodo</th>
            <th class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in items" :key="c.id" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3">{{ c.id }}</td>
            <td class="px-4 py-3">{{ c.nombre }}</td>
            <td v-if="activeTab === 'luz'" class="px-4 py-3">{{ c.email || '—' }}</td>
            <td v-for="col in extraColKeys" :key="col" class="px-4 py-3">{{ c[col] }}</td>
            <td class="px-4 py-3">{{ c.fechaVencimiento?.split('T')[0] }}</td>
            <td class="px-4 py-3">{{ c.periodoEmision }}</td>
            <td class="px-4 py-3 space-x-2">
              <button @click="edit(c)" class="text-blue-400 hover:text-blue-300">✏️</button>
              <button @click="remove(c.id)" class="text-red-400 hover:text-red-300">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <h2 class="text-lg font-bold mb-4">{{ form.id ? 'Editar' : 'Nuevo' }} Contrato</h2>
        <form @submit.prevent="save" class="space-y-3">
          <input v-model="form.nombre" placeholder="Nombre" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <input v-if="activeTab === 'luz'" v-model="form.email" type="email" placeholder="Email" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <input v-for="f in extraFields" :key="f.key" v-model="form[f.key]" :placeholder="f.label" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <input v-model="form.fechaVencimiento" type="date" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <select v-model="form.periodoEmision" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option v-for="p in periodos" :key="p" :value="p">{{ p }}</option>
          </select>
          <div class="flex gap-2 justify-end mt-4">
            <button type="button" @click="showForm = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
            <button type="submit" class="px-4 py-2 bg-emerald-600 rounded-lg text-sm font-medium">Guardar</button>
          </div>
        </form>

        <!-- Adjuntos -->
        <FileUploader v-if="form.id" :tipo="tipoEntidad" :id-padre="form.id" />
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue'
import api from '../api'
import FileUploader from '../components/FileUploader.vue'
import { useToast } from '../composables/useToast'
const { success, error: toastError, info } = useToast()

const tabs = [
  { key: 'luz', label: '⚡ Luz' },
  { key: 'agua', label: '💧 Agua' },
  { key: 'internet', label: '🌐 Internet' },
]
const activeTab = ref('luz')
const items = ref([])
const showForm = ref(false)
const form = ref({})
const periodos = ['Semanal', 'Quincenal', 'Mensual', 'Bimestral', 'Semestral', 'Anual']

const apiPath = computed(() => `/contratos/${activeTab.value}`)
const tipoEntidad = computed(() => {
  if (activeTab.value === 'luz') return 'ContratoLuz'
  if (activeTab.value === 'agua') return 'ContratoAgua'
  return 'ContratoInternet'
})

const tabConfig = {
  luz: { cols: ['RPU', 'Nº Medidor'], colKeys: ['rpu', 'numeroMedidor'], fields: [{ key: 'rpu', label: 'RPU' }, { key: 'numeroMedidor', label: 'Nº Medidor' }] },
  agua: { cols: ['Nº Inmueble', 'Nº Contrato'], colKeys: ['numeroInmueble', 'numeroContrato'], fields: [{ key: 'numeroInmueble', label: 'Nº Inmueble' }, { key: 'numeroContrato', label: 'Nº Contrato' }] },
  internet: { cols: ['Nº Contrato', 'Pago OXXO'], colKeys: ['numeroContrato', 'numeroPagoOXXO'], fields: [{ key: 'numeroContrato', label: 'Nº Contrato' }, { key: 'numeroPagoOXXO', label: 'Nº Pago OXXO' }] },
}

const extraCols = computed(() => tabConfig[activeTab.value].cols)
const extraColKeys = computed(() => tabConfig[activeTab.value].colKeys)
const extraFields = computed(() => tabConfig[activeTab.value].fields)

async function load() { items.value = (await api.get(apiPath.value)).data }
watch(activeTab, load)
onMounted(load)

function openNew() { form.value = { periodoEmision: 'Mensual' }; showForm.value = true }
function edit(c) { form.value = { ...c }; showForm.value = true }

async function save() {
  if (form.value.id) await api.put(`${apiPath.value}/${form.value.id}`, form.value)
  else {
    const res = await api.post(apiPath.value, form.value)
    form.value.id = res.data.id
  }
  success('Contrato guardado correctamente.')
  await load()
}

async function remove(id) {
  if (confirm('¿Eliminar contrato?')) {
    try {
      await api.delete(`${apiPath.value}/${id}`)
      await load()
      success('Contrato eliminado.')
    } catch (err) {
      const msg = err.response?.data || err.message || 'Error desconocido'
      toastError(typeof msg === 'string' ? msg : JSON.stringify(msg))
    }
  }
}

// === EXPORTAR CSV ===
async function exportarCsv() {
  const res = await api.get('/contratos/luz/exportar', { responseType: 'blob' })
  const url = URL.createObjectURL(new Blob([res.data]))
  const a = document.createElement('a')
  a.href = url; a.download = 'contratos_luz.csv'; a.click()
  URL.revokeObjectURL(url)
}

// === IMPORTAR CSV ===
// === IMPORTAR RECIBOS XML ===
async function importarXml(e) {
  const files = e.target.files
  if (!files || files.length === 0) return
  const fd = new FormData()
  for (const file of files) fd.append('archivos', file)
  try {
    const res = await api.post('/contratos/luz/importar-xml', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    const { insertados, omitidos, errores, detalle } = res.data
    const resumen = `✅ Recibos procesados:\n- Insertados: ${insertados}\n- Omitidos: ${omitidos}\n- Errores: ${errores}\n\n${detalle.join('\n')}`
    info(resumen)
  } catch (err) {
    toastError('Error al importar XML: ' + (err.response?.data || err.message))
  } finally {
    e.target.value = ''
  }
}

async function importarCsv(e) {
  const file = e.target.files[0]
  if (!file) return
  const fd = new FormData()
  fd.append('archivo', file)
  try {
    const res = await api.post('/contratos/luz/importar', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    const { insertados, actualizados, errores } = res.data
    success(`Importado: ${insertados} nuevos, ${actualizados} actualizados, ${errores} errores.`)
    await load()
  } catch (err) {
    toastError('Error al importar: ' + (err.response?.data || err.message))
  } finally {
    e.target.value = '' // reset input
  }
}
</script>
