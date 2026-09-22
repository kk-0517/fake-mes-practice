<script setup>
import { onMounted, onUnmounted, ref } from 'vue'
import { createStation, getDashboard, getStations, getTrace } from './api'

const barcode = ref('')
const stations = ref([])
const records = ref([])
const loading = ref(false)
const error = ref('')
const searched = ref(false)

const todayTrackInCount = ref(0)
const recent = ref([])
const dashboardError = ref('')
const dashboardLoading = ref(false)

const newCode = ref('')
const newName = ref('')
const stationSaving = ref(false)
const stationMsg = ref('')
const stationErr = ref('')

let timer = null

onMounted(async () => {
  try {
    stations.value = await getStations()
  } catch (e) {
    error.value = `无法加载工位，请先启动 API：${e.message}`
  }
  await refreshDashboard()
  timer = setInterval(refreshDashboard, 3000)
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})

async function refreshStations() {
  stations.value = await getStations()
}

async function refreshDashboard() {
  dashboardLoading.value = true
  try {
    const data = await getDashboard(10)
    todayTrackInCount.value = data.todayTrackInCount
    recent.value = data.recent
    dashboardError.value = ''
  } catch (e) {
    dashboardError.value = e.message
  } finally {
    dashboardLoading.value = false
  }
}

async function search() {
  error.value = ''
  searched.value = true
  loading.value = true
  try {
    records.value = await getTrace(barcode.value.trim())
  } catch (e) {
    records.value = []
    error.value = e.message
  } finally {
    loading.value = false
  }
}

function useBarcode(code) {
  barcode.value = code
  search()
}

async function addStation() {
  stationMsg.value = ''
  stationErr.value = ''
  stationSaving.value = true
  try {
    const result = await createStation(newCode.value.trim(), newName.value.trim())
    stationMsg.value = `已新增 ${result.station.code}`
    newCode.value = ''
    newName.value = ''
    await refreshStations()
  } catch (e) {
    stationErr.value = e.message
  } finally {
    stationSaving.value = false
  }
}
</script>

<template>
  <h1>FakeMes 条码追溯</h1>
  <p class="sub">模拟数采写入进站/出站后，在这里看看板并按条码查询</p>

  <div class="card">
    <div class="stat-row">
      <div class="stat">
        <div class="stat-label">今日进站</div>
        <div class="stat-value">{{ todayTrackInCount }}</div>
      </div>
      <button class="ghost" :disabled="dashboardLoading" @click="refreshDashboard">
        {{ dashboardLoading ? '刷新中…' : '刷新看板' }}
      </button>
    </div>
    <p v-if="dashboardError" class="error">看板加载失败：{{ dashboardError }}</p>
    <p class="hint">每 3 秒自动刷新；也可点右侧手动刷新</p>

    <h3>最近 10 条</h3>
    <table v-if="recent.length">
      <thead>
        <tr>
          <th>类型</th>
          <th>工位</th>
          <th>条码</th>
          <th>时间</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(r, idx) in recent" :key="idx">
          <td>
            <span class="tag" :class="r.type === 'In' ? 'in' : 'out'">
              {{ r.type === 'In' ? '进站' : '出站' }}
            </span>
          </td>
          <td>{{ r.stationCode }}</td>
          <td>
            <button class="link" @click="useBarcode(r.barcode)">{{ r.barcode }}</button>
          </td>
          <td>{{ new Date(r.time).toLocaleString() }}</td>
        </tr>
      </tbody>
    </table>
    <p v-else class="hint">暂无记录，先跑 Simulator</p>
  </div>

  <div class="card">
    <div class="row">
      <input
        v-model="barcode"
        placeholder="输入条码，例如 SN143015"
        @keyup.enter="search"
      />
      <button :disabled="loading || !barcode.trim()" @click="search">
        {{ loading ? '查询中…' : '查询' }}
      </button>
    </div>
    <p class="hint">点上方最近记录里的条码，可直接追溯</p>
    <p v-if="error" class="error">{{ error }}</p>
  </div>

  <div class="card">
    <h3 style="margin-top: 0">工位主数据</h3>
    <div class="row" style="margin-bottom: 12px">
      <input v-model="newCode" placeholder="工位码，如 OP20" @keyup.enter="addStation" />
      <input v-model="newName" placeholder="名称，如 测试工位" @keyup.enter="addStation" />
      <button
        :disabled="stationSaving || !newCode.trim() || !newName.trim()"
        @click="addStation"
      >
        {{ stationSaving ? '保存中…' : '新增工位' }}
      </button>
    </div>
    <p v-if="stationMsg" class="ok">{{ stationMsg }}</p>
    <p v-if="stationErr" class="error">{{ stationErr }}</p>
    <div class="stations">
      <span v-for="s in stations" :key="s.code" class="chip">
        {{ s.code }} · {{ s.name }}
      </span>
      <span v-if="!stations.length" class="hint">暂无</span>
    </div>
    <p class="hint">新增后，重启 Simulator，启动时选工位；握手日志里会看到 OnlineRequest / AllowOnline</p>
  </div>

  <div class="card">
    <h3 style="margin-top: 0">进出站记录</h3>
    <table v-if="records.length">
      <thead>
        <tr>
          <th>类型</th>
          <th>工位</th>
          <th>条码</th>
          <th>时间</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="(r, idx) in records" :key="idx">
          <td>
            <span class="tag" :class="r.type === 'In' ? 'in' : 'out'">
              {{ r.type === 'In' ? '进站' : '出站' }}
            </span>
          </td>
          <td>{{ r.stationCode }}</td>
          <td>{{ r.barcode }}</td>
          <td>{{ new Date(r.time).toLocaleString() }}</td>
        </tr>
      </tbody>
    </table>
    <p v-else class="hint">
      {{ searched ? '没有记录' : '输入条码后查询' }}
    </p>
  </div>
</template>
