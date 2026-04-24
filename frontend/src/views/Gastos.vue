<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">💸 Gastos</h1>
      <div class="flex gap-2">
        <button @click="exportarCsv" class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm font-medium">⬇ Exportar CSV</button>
        <label class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm font-medium cursor-pointer">
          ⬆ Importar CSV
          <input type="file" accept=".csv" class="hidden" @change="importarCsv" />
        </label>
        <button @click="openNew" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">+ Nuevo</button>
      </div>
    </div>

    <!-- Buscador -->
    <div class="mb-4">
      <input v-model="busqueda" placeholder="🔍 Buscar por descripción, departamento, ubicación..."
        class="w-full md:w-96 px-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm focus:outline-none focus:border-emerald-500" />
    </div>

    <!-- Totales -->
    <div class="grid grid-cols-3 gap-4 mb-6">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 text-center">
        <p class="text-xl font-bold text-blue-400">${{ fmt(totales.manoDeObra) }}</p>
        <p class="text-xs text-gray-400 mt-1">Total Mano de Obra</p>
      </div>
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 text-center">
        <p class="text-xl font-bold text-yellow-400">${{ fmt(totales.material) }}</p>
        <p class="text-xs text-gray-400 mt-1">Total Material</p>
      </div>
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-4 text-center">
        <p class="text-xl font-bold text-emerald-400">${{ fmt(totales.total) }}</p>
        <p class="text-xs text-gray-400 mt-1">Total General</p>
      </div>
    </div>

    <!-- Tabla -->
    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-x-auto">
      <table class="w-full text-sm min-w-[900px]">
        <thead class="bg-gray-800/50">
          <tr>
            <th @click="sortBy('fecha')" class="px-4 py-3 text-left text-gray-400 cursor-pointer hover:text-white select-none">Fecha {{ sortIcon('fecha') }}</th>
            <th @click="sortBy('ubicacion')" class="px-4 py-3 text-left text-gray-400 cursor-pointer hover:text-white select-none">Ubicación {{ sortIcon('ubicacion') }}</th>
            <th @click="sortBy('clave')" class="px-4 py-3 text-left text-gray-400 cursor-pointer hover:text-white select-none">Depto {{ sortIcon('clave') }}</th>
            <th @click="sortBy('descripcion')" class="px-4 py-3 text-left text-gray-400 cursor-pointer hover:text-white select-none">Descripción {{ sortIcon('descripcion') }}</th>
            <th @click="sortBy('manoDeObra')" class="px-4 py-3 text-right text-gray-400 cursor-pointer hover:text-white select-none">Mano de Obra {{ sortIcon('manoDeObra') }}</th>
            <th @click="sortBy('material')" class="px-4 py-3 text-right text-gray-400 cursor-pointer hover:text-white select-none">Material {{ sortIcon('material') }}</th>
            <th @click="sortBy('total')" class="px-4 py-3 text-right text-gray-400 cursor-pointer hover:text-white select-none">Total {{ sortIcon('total') }}</th>
            <th class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="itemsFiltrados.length === 0">
            <td colspan="8" class="px-4 py-8 text-center text-gray-500">No hay gastos registrados.</td>
          </tr>
          <tr v-for="g in itemsFiltrados" :key="g.id" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3">{{ g.fecha?.split('T')[0] }}</td>
            <td class="px-4 py-3">{{ g.departamento?.ubicacion?.calle }} {{ g.departamento?.ubicacion?.numero }}</td>
            <td class="px-4 py-3 font-mono">{{ g.departamento?.clave }}</td>
            <td class="px-4 py-3">{{ g.descripcion || '—' }}</td>
            <td class="px-4 py-3 text-right">${{ fmt(g.manoDeObra) }}</td>
            <td class="px-4 py-3 text-right">${{ fmt(g.material) }}</td>
            <td class="px-4 py-3 text-right font-bold text-emerald-400">${{ fmt((g.manoDeObra || 0) + (g.material || 0)) }}</td>
            <td class="px-4 py-3 space-x-2">
              <button @click="edit(g)" class="text-blue-400 hover:text-blue-300">✏️</button>
              <button @click="remove(g.id)" class="text-red-400 hover:text-red-300">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-md">
        <h2 class="text-lg font-bold mb-4">{{ form.id ? 'Editar' : 'Nuevo' }} Gasto</h2>
        <form @submit.prevent="save" class="space-y-3">
          <input v-model="form.fecha" type="date" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <select v-model="form.departamentoId" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option value="">Seleccionar departamento</option>
            <option v-for="d in departamentos" :key="d.id" :value="d.id">
              {{ d.ubicacion?.calle }} {{ d.ubicacion?.numero }} — {{ d.clave }}
            </option>
          </select>
          <input v-model="form.descripcion" placeholder="Descripción del gasto" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="text-xs text-gray-400 mb-1 block">Mano de Obra</label>
              <input v-model.number="form.manoDeObra" type="number" min="0" step="0.01" placeholder="0" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
            </div>
            <div>
              <label class="text-xs text-gray-400 mb-1 block">Material</label>
              <input v-model.number="form.material" type="number" min="0" step="0.01" placeholder="0" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
            </div>
          </div>
          <!-- Preview total -->
          <div class="bg-gray-800 rounded-lg px-4 py-3 text-center">
            <span class="text-gray-400 text-sm">Total: </span>
            <span class="text-emerald-400 font-bold text-lg">${{ fmt((form.manoDeObra || 0) + (form.material || 0)) }}</span>
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
import { ref, computed, onMounted } from 'vue'
import api from '../api'
import { useToast } from '../composables/useToast'
import { useConfirm } from '../composables/useConfirm'

const { success, error: toastError } = useToast()
const { confirm: confirmDialog } = useConfirm()

const items = ref([])
const departamentos = ref([])
const showForm = ref(false)
const busqueda = ref('')
const sortCol = ref('fecha')
const sortDir = ref(-1) // más reciente primero

const emptyForm = { fecha: new Date().toISOString().split('T')[0], departamentoId: '', descripcion: '', manoDeObra: 0, material: 0 }
const form = ref({ ...emptyForm })

// ─── Ordenamiento y búsqueda ─────────────────────────────────────────────────

function sortBy(col) {
  if (sortCol.value === col) sortDir.value *= -1
  else { sortCol.value = col; sortDir.value = 1 }
}
function sortIcon(col) {
  if (sortCol.value !== col) return ''
  return sortDir.value === 1 ? '↑' : '↓'
}

const itemsFiltrados = computed(() => {
  let lista = [...items.value]
  const q = busqueda.value.toLowerCase().trim()
  if (q) {
    lista = lista.filter(g =>
      (g.descripcion || '').toLowerCase().includes(q) ||
      (g.departamento?.clave || '').toLowerCase().includes(q) ||
      (`${g.departamento?.ubicacion?.calle} ${g.departamento?.ubicacion?.numero}`).toLowerCase().includes(q)
    )
  }
  lista.sort((a, b) => {
    let va, vb
    if (sortCol.value === 'ubicacion') {
      va = `${a.departamento?.ubicacion?.calle} ${a.departamento?.ubicacion?.numero}` || ''
      vb = `${b.departamento?.ubicacion?.calle} ${b.departamento?.ubicacion?.numero}` || ''
    } else if (sortCol.value === 'clave') {
      va = a.departamento?.clave || ''
      vb = b.departamento?.clave || ''
    } else if (sortCol.value === 'total') {
      va = (a.manoDeObra || 0) + (a.material || 0)
      vb = (b.manoDeObra || 0) + (b.material || 0)
    } else {
      va = a[sortCol.value] ?? ''
      vb = b[sortCol.value] ?? ''
    }
    if (typeof va === 'string') return va.localeCompare(vb) * sortDir.value
    return (va - vb) * sortDir.value
  })
  return lista
})

// ─── Totales de la vista filtrada ────────────────────────────────────────────

const totales = computed(() => {
  return itemsFiltrados.value.reduce((acc, g) => {
    acc.manoDeObra += g.manoDeObra || 0
    acc.material   += g.material || 0
    acc.total      += (g.manoDeObra || 0) + (g.material || 0)
    return acc
  }, { manoDeObra: 0, material: 0, total: 0 })
})

// ─── CRUD ─────────────────────────────────────────────────────────────────────

function fmt(v) { return (v || 0).toLocaleString('es-MX', { minimumFractionDigits: 0, maximumFractionDigits: 2 }) }

async function load() {
  const [gRes, dRes] = await Promise.all([api.get('/gastos'), api.get('/departamentos')])
  items.value = gRes.data
  departamentos.value = dRes.data
}

function openNew() { form.value = { ...emptyForm }; showForm.value = true }
function edit(g) {
  form.value = { ...g, fecha: g.fecha?.split('T')[0], departamentoId: g.departamentoId }
  showForm.value = true
}

async function save() {
  try {
    const payload = { ...form.value, manoDeObra: form.value.manoDeObra || 0, material: form.value.material || 0 }
    if (form.value.id) await api.put(`/gastos/${form.value.id}`, payload)
    else await api.post('/gastos', payload)
    success('Gasto guardado.')
    showForm.value = false
    await load()
  } catch (err) {
    toastError(err.response?.data || err.message)
  }
}

async function remove(id) {
  if (await confirmDialog({ title: '¿Eliminar gasto?', message: 'Esta acción no se puede deshacer.' })) {
    try {
      await api.delete(`/gastos/${id}`)
      await load()
      success('Gasto eliminado.')
    } catch (err) {
      toastError(err.response?.data || err.message)
    }
  }
}

// ─── CSV ──────────────────────────────────────────────────────────────────────

async function exportarCsv() {
  const res = await api.get('/gastos/exportar', { responseType: 'blob' })
  const url = URL.createObjectURL(new Blob([res.data]))
  const a = document.createElement('a')
  a.href = url; a.download = 'gastos.csv'; a.click()
  URL.revokeObjectURL(url)
}

async function importarCsv(e) {
  const file = e.target.files[0]
  if (!file) return
  const fd = new FormData()
  fd.append('archivo', file)
  try {
    const res = await api.post('/gastos/importar', fd, { headers: { 'Content-Type': 'multipart/form-data' } })
    const { insertados, actualizados, errores, detalle } = res.data
    const msg = `Importado: ${insertados} nuevos, ${actualizados} actualizados, ${errores} errores.${detalle?.length ? '\n' + detalle.join('\n') : ''}`
    errores > 0 ? toastError(msg) : success(msg)
    await load()
  } catch (err) {
    toastError('Error al importar: ' + (err.response?.data || err.message))
  } finally {
    e.target.value = ''
  }
}

onMounted(load)
</script>
