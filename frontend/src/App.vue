<template>
  <div class="flex flex-col md:flex-row min-h-screen bg-gray-950">
    <!-- Sidebar -->
    <aside v-if="auth.isLoggedIn" 
      class="bg-gray-900 border-r border-gray-800 flex flex-col transition-all duration-300"
      :class="isCollapsed ? 'w-20' : 'w-64'">
      
      <div class="p-4 border-b border-gray-800 flex items-center justify-between">
        <div v-if="!isCollapsed">
          <h1 class="text-xl font-bold text-emerald-400">🏠 Rentas</h1>
          <p class="text-xs text-gray-400 mt-1 truncate">{{ auth.user?.correo }}</p>
        </div>
        <button @click="isCollapsed = !isCollapsed" class="p-2 text-gray-400 hover:text-white rounded-lg hover:bg-gray-800">
          {{ isCollapsed ? '➡️' : '⬅️' }}
        </button>
      </div>

      <nav class="flex-1 p-2 space-y-1">
        <router-link v-for="item in menuItems" :key="item.path" :to="item.path"
          class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors group"
          :class="$route.path === item.path ? 'bg-emerald-600/20 text-emerald-400' : 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'"
          :title="isCollapsed ? item.label : ''">
          <span class="text-xl">{{ item.icon }}</span>
          <span v-if="!isCollapsed" class="transition-opacity duration-200">{{ item.label }}</span>
        </router-link>
      </nav>

      <div class="p-2 border-t border-gray-800">
        <button @click="logout" class="w-full flex items-center gap-3 px-3 py-2 text-sm text-red-400 hover:bg-gray-800 rounded-lg transition-colors" :title="isCollapsed ? 'Cerrar Sesión' : ''">
          <span class="text-xl">🚪</span>
          <span v-if="!isCollapsed">Cerrar Sesión</span>
        </button>
      </div>
    </aside>

    <!-- Main content -->
    <main class="flex-1 overflow-auto w-full">
      <router-view />
    </main>
  </div>
  <ToastContainer />
  <ConfirmDialog ref="confirmDialogRef" />
</template>

<script setup>
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'
import ToastContainer from './components/ToastContainer.vue'
import ConfirmDialog from './components/ConfirmDialog.vue'
import { useConfirm } from './composables/useConfirm'
import { onMounted } from 'vue'

const { registerDialog } = useConfirm()
const confirmDialogRef = ref(null)
onMounted(() => { registerDialog(confirmDialogRef.value) })

const auth = useAuthStore()
const router = useRouter()
const isCollapsed = ref(false)

const menuItems = computed(() => {
  const items = [
    { path: '/', icon: '📊', label: 'Dashboard' },
    { path: '/tablero', icon: '💰', label: 'Tablero de Cobro' },
    { path: '/tickets', icon: '🎫', label: 'Tickets' },
  ]
  if (auth.isPropietario) {
    items.splice(1, 0,
      { path: '/ubicaciones', icon: '📍', label: 'Ubicaciones' },
      { path: '/departamentos', icon: '🏢', label: 'Departamentos' },
      { path: '/contratos', icon: '📄', label: 'Contratos' },
      { path: '/cobranza', icon: '💳', label: 'Cobranza' },
    )
    items.push({ path: '/usuarios', icon: '👥', label: 'Usuarios' })
    items.push({ path: '/backups', icon: '💾', label: 'Backups' })
  }
  return items
})

function logout() {
  auth.logout()
  router.push('/login')
}
</script>
