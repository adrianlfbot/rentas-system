import { createRouter, createWebHistory } from 'vue-router'

const routes = [
  { path: '/login', name: 'Login', component: () => import('./views/Login.vue') },
  { path: '/', name: 'Dashboard', component: () => import('./views/Dashboard.vue'), meta: { auth: true } },
  { path: '/ubicaciones', name: 'Ubicaciones', component: () => import('./views/Ubicaciones.vue'), meta: { auth: true, role: 'Propietario' } },
  { path: '/departamentos', name: 'Departamentos', component: () => import('./views/Departamentos.vue'), meta: { auth: true, role: 'Propietario' } },
  { path: '/contratos', name: 'Contratos', component: () => import('./views/Contratos.vue'), meta: { auth: true, role: 'Propietario' } },
  { path: '/cobranza', name: 'Cobranza', component: () => import('./views/Cobranza.vue'), meta: { auth: true, role: 'Propietario' } },
  { path: '/tablero', name: 'Tablero', component: () => import('./views/Tablero.vue'), meta: { auth: true, role: 'Propietario' } },
  { path: '/tickets', name: 'Tickets', component: () => import('./views/Tickets.vue'), meta: { auth: true } },
  { path: '/usuarios', name: 'Usuarios', component: () => import('./views/Usuarios.vue'), meta: { auth: true, role: 'Propietario' } },
  { path: '/backups', name: 'Backups', component: () => import('./views/Backups.vue'), meta: { auth: true, role: 'Propietario' } },
]

const router = createRouter({ history: createWebHistory(), routes })

router.beforeEach((to, from, next) => {
  const token = localStorage.getItem('token')
  const user = JSON.parse(localStorage.getItem('user') || 'null')
  if (to.meta.auth && !token) return next('/login')
  if (to.meta.role && user?.tipo !== to.meta.role) return next('/')
  next()
})

export default router
