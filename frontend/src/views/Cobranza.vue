<template>
  <div class="p-8">
    <div class="flex justify-between items-center mb-6">
      <h1 class="text-2xl font-bold">Cobranza</h1>
      <button @click="openNew" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-700 rounded-lg text-sm font-medium">+ Registrar Pago</button>
    </div>

    <div class="mb-4">
      <input v-model="filterPeriodo" type="month" @change="load" class="px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
    </div>

    <div class="bg-gray-900 border border-gray-800 rounded-xl overflow-x-auto">
      <table class="w-full text-sm min-w-[800px]">
        <thead class="bg-gray-800/50">
          <tr>
            <th class="px-4 py-3 text-left text-gray-400">ID</th>
            <th class="px-4 py-3 text-left text-gray-400">Ubicación</th>
            <th class="px-4 py-3 text-left text-gray-400">Depto</th>
            <th class="px-4 py-3 text-left text-gray-400">Periodo</th>
            <th class="px-4 py-3 text-left text-gray-400">Fecha Cobro</th>
            <th class="px-4 py-3 text-left text-gray-400">Medio</th>
            <th class="px-4 py-3 text-left text-gray-400">Monto</th>
            <th class="px-4 py-3 text-left text-gray-400">Acciones</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in items" :key="c.id" class="border-t border-gray-800 hover:bg-gray-800/30">
            <td class="px-4 py-3">{{ c.id }}</td>
            <td class="px-4 py-3">{{ c.ubicacion?.calle }} {{ c.ubicacion?.numero }}</td>
            <td class="px-4 py-3 font-mono">{{ c.claveDepartamento }}</td>
            <td class="px-4 py-3">{{ c.periodo }}</td>
            <td class="px-4 py-3">{{ c.fechaCobro?.split('T')[0] }}</td>
            <td class="px-4 py-3">{{ c.medio }}</td>
            <td class="px-4 py-3">${{ c.monto?.toLocaleString() }}</td>
            <td class="px-4 py-3 space-x-2">
              <button @click="edit(c)" class="text-blue-400">✏️</button>
              <button @click="remove(c.id)" class="text-red-400">🗑️</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Modal -->
    <div v-if="showForm" class="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div class="bg-gray-900 border border-gray-800 rounded-xl p-6 w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <h2 class="text-lg font-bold mb-4">Registrar Pago</h2>
        <form @submit.prevent="save" class="space-y-3">
          <select v-model="form.idUbicacion" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm">
            <option value="" disabled>Ubicación</option>
            <option v-for="u in ubicaciones" :key="u.idUbicacion" :value="u.idUbicacion">{{ u.calle }} {{ u.numero }}</option>
          </select>
          <input v-model="form.claveDepartamento" placeholder="Clave Departamento" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <input v-model="form.periodo" type="month" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <input v-model="form.fechaCobro" type="date" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <input v-model="form.medio" placeholder="Medio (Transferencia, Efectivo...)" class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <input v-model.number="form.monto" type="number" step="0.01" placeholder="Monto" required class="w-full px-3 py-2 bg-gray-800 border border-gray-700 rounded-lg text-sm" />
          <div class="flex gap-2 justify-end mt-4">
            <button type="button" @click="showForm = false" class="px-4 py-2 bg-gray-700 rounded-lg text-sm">Cancelar</button>
            <button type="submit" class="px-4 py-2 bg-emerald-600 rounded-lg text-sm font-medium">Guardar</button>
          </div>
        </form>

        <!-- Adjuntos (Comprobante) -->
        <FileUploader v-if="form.id" tipo="Cobranza" :id-padre="form.id" />
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
const showForm = ref(false)
const filterPeriodo = ref('')
const form = ref({ idUbicacion: '', claveDepartamento: '', periodo: '', fechaCobro: '', medio: '', monto: 0 })

async function load() {
  const params = filterPeriodo.value ? `?periodo=${filterPeriodo.value}` : ''
  items.value = (await api.get(`/cobranza${params}`)).data
  ubicaciones.value = (await api.get('/ubicaciones')).data
}
onMounted(load)

function openNew() { form.value = { idUbicacion: '', claveDepartamento: '', periodo: '', fechaCobro: '', medio: '', monto: 0 }; showForm.value = true }
function edit(c) { form.value = { ...c }; showForm.value = true }

async function save() {
  if (form.value.id) await api.put(`/cobranza/${form.value.id}`, form.value)
  else {
    const res = await api.post('/cobranza', form.value)
    form.value.id = res.data.id
  }
  success('Pago registrado correctamente.')
  await load()
}

async function remove(id) {
  if (await confirmDialog({ title: '¿Eliminar pago?', message: 'El registro de cobranza será eliminado.' })) {
    try { await api.delete(`/cobranza/${id}`); await load(); success('Pago eliminado.') }
    catch (err) { toastError(err.response?.data || err.message) }
  }
}
</script>
