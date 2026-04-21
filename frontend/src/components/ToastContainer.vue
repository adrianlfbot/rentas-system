<template>
  <teleport to="body">
    <div class="fixed top-5 right-5 z-[9999] flex flex-col gap-2 w-80 max-w-full">
      <transition-group name="toast">
        <div
          v-for="t in toasts"
          :key="t.id"
          class="flex items-start gap-3 px-4 py-3 rounded-xl shadow-lg text-sm font-medium border"
          :class="{
            'bg-emerald-900 border-emerald-600 text-emerald-100': t.type === 'success',
            'bg-red-900 border-red-600 text-red-100':             t.type === 'error',
            'bg-blue-900 border-blue-600 text-blue-100':          t.type === 'info',
            'bg-yellow-900 border-yellow-600 text-yellow-100':    t.type === 'warn',
          }"
        >
          <span class="text-lg leading-none mt-0.5">
            {{ t.type === 'success' ? '✅' : t.type === 'error' ? '❌' : t.type === 'warn' ? '⚠️' : 'ℹ️' }}
          </span>
          <span class="flex-1 whitespace-pre-wrap break-words">{{ t.message }}</span>
        </div>
      </transition-group>
    </div>
  </teleport>
</template>

<script setup>
import { useToast } from '../composables/useToast'
const { toasts } = useToast()
</script>

<style scoped>
.toast-enter-active { transition: all 0.3s ease; }
.toast-leave-active { transition: all 0.3s ease; }
.toast-enter-from  { opacity: 0; transform: translateX(60px); }
.toast-leave-to    { opacity: 0; transform: translateX(60px); }
</style>
