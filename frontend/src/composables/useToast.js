import { ref } from 'vue'

const toasts = ref([])
let nextId = 0

export function useToast() {
  function toast(message, type = 'success', duration = 4000) {
    const id = nextId++
    toasts.value.push({ id, message, type })
    setTimeout(() => {
      toasts.value = toasts.value.filter(t => t.id !== id)
    }, duration)
  }

  const success = (msg, duration) => toast(msg, 'success', duration)
  const error   = (msg, duration) => toast(msg, 'error', duration)
  const info    = (msg, duration) => toast(msg, 'info', duration)
  const warn    = (msg, duration) => toast(msg, 'warn', duration)

  return { toasts, toast, success, error, info, warn }
}
