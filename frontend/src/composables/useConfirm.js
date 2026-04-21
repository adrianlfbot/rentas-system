import { ref } from 'vue'

const dialogRef = ref(null)

export function useConfirm() {
  function setRef(ref) { dialogRef.value = ref }

  async function confirm(opts = {}) {
    if (!dialogRef.value) return false
    return await dialogRef.value.open(opts)
  }

  return { setRef, confirm }
}
