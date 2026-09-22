export async function getStations() {
  const res = await fetch('/api/stations')
  if (!res.ok) throw new Error(`stations HTTP ${res.status}`)
  return res.json()
}

export async function getTrace(barcode) {
  const res = await fetch(`/api/trace?barcode=${encodeURIComponent(barcode)}`)
  if (!res.ok) throw new Error(`trace HTTP ${res.status}`)
  return res.json()
}
