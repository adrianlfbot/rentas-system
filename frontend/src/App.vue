<template>
  <div class="flex min-h-screen bg-gray-950">
    <!-- Sidebar -->
    <aside v-if="auth.isLoggedIn" class="w-64 bg-gray-900 border-r border-gray-800 flex flex-col">
      <div class="p-6 border-b border-gray-800">
        <h1 class="text-xl font-bold text-emerald-400">🏠 Rentas</h1>
        <p class="text-sm text-gray-400 mt-1">{{ auth.user?.correo }}</p>
      </div>
      <nav class="flex-1 p-4 space-y-1">
        <router-link v-for="item in menuItems" :key="item.path" :to="item.path"
          class="flex items-center gap-3 px-3 py-2 rounded-lg text-sm transition-colors"
          :class="$route.path === item.path ? 'bg-emerald-600/20 text-emerald-400' : 'text-gray-400 hover:bg-gray-800 hover:text-gray-200'">
          <span>{{ item.icon }}</span>
          <span>{{ item.label }}</span>
        </router-link>
      </nav>
      <div class="p-4 border-t border-gray-800">
        <button @click="logout" class="w-full px-3 py-2 text-sm text-red-400 hover:bg-gray-800 rounded-lg transition-colors">
          🚪 Cerrar Sesión
        </button>
      </div>
    </aside>

    <!-- Main content -->
    <main class="flex-1 overflow-auto">
      <router-view />
    </main>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from './stores/auth'

const auth = useAuthStore()
const router = useRouter()

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
  }
  return items
})

function logout() {
  auth.logout()
  router.push('/login')
}
</script>
