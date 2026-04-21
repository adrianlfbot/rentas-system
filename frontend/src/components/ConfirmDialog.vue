<template>
  <teleport to="body">
    <transition name="fade">
      <div v-if="visible" class="fixed inset-0 z-[9998] flex items-center justify-center bg-black/60 p-4">
        <div class="bg-gray-900 border border-gray-700 rounded-xl shadow-2xl w-full max-w-sm p-6">
          <div class="flex items-start gap-3 mb-5">
            <span class="text-2xl">⚠️</span>
            <div>
              <p class="text-white font-semibold text-sm">{{ title }}</p>
              <p class="text-gray-400 text-sm mt-1">{{ message }}</p>
            </div>
          </div>
          <div class="flex gap-2 justify-end">
            <button @click="cancel" class="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg text-sm text-gray-300 transition-colors">
              Cancelar
            </button>
            <button @click="confirm" class="px-4 py-2 bg-red-600 hover:bg-red-700 rounded-lg text-sm text-white font-medium transition-colors">
              {{ confirmLabel }}
            </button>
          </div>
        </div>
      </div>
    </transition>
  </teleport>
</template>

<script setup>
import { ref } from 'vue'

const visible = ref(false)
const title = ref('¿Confirmar acción?')
const message = ref('Esta acción no se puede deshacer.')
const confirmLabel = ref('Eliminar')
let resolveFn = null

function open(opts = {}) {
  title.value = opts.title ?? '¿Confirmar acción?'
  message.value = opts.message ?? 'Esta acción no se puede deshacer.'
  confirmLabel.value = opts.confirmLabel ?? 'Eliminar'
  visible.value = true
  return new Promise(resolve => { resolveFn = resolve })
}

function confirm() { visible.value = false; resolveFn?.(true) }
function cancel()  { visible.value = false; resolveFn?.(false) }

defineExpose({ open })
</script>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
