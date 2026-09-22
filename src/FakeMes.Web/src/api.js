export async function getStations() {
  const res = await fetch('/api/stations')
  if (!res.ok) throw new Error(`stations HTTP ${res.status}`)
  return res.json()
}

export async function createStation(code, name) {
  const res = await fetch('/api/stations', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ code, name })
  })
  const body = await res.json().catch(() => ({}))
  if (!res.ok) {
    throw new Error(body.message || `create station HTTP ${res.status}`)
  }
  return body
}

export async function getTrace(barcode) {
  const res = await fetch(`/api/trace?barcode=${encodeURIComponent(barcode)}`)
  if (!res.ok) throw new Error(`trace HTTP ${res.status}`)
  return res.json()
}

export async function getDashboard(recent = 10) {
  const res = await fetch(`/api/dashboard?recent=${recent}`)
  if (!res.ok) throw new Error(`dashboard HTTP ${res.status}`)
  return res.json()
}
