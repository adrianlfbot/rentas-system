import { ref } from 'vue'

// Ref global compartida entre App.vue (quien monta el dialog) y cualquier vista
const dialogRef = ref(null)

export function useConfirm() {
  // App.vue llama esto con el ref del componente montado
  function registerDialog(r) {
    dialogRef.value = r
  }

  async function confirm(opts = {}) {
    if (!dialogRef.value) {
      console.warn('ConfirmDialog no registrado')
      return false
    }
    return await dialogRef.value.open(opts)
  }

  return { registerDialog, confirm }
}
