<script setup>
import { onMounted, ref } from 'vue'
import { getStations, getTrace } from './api'

const barcode = ref('')
const stations = ref([])
const records = ref([])
const loading = ref(false)
const error = ref('')
const searched = ref(false)

onMounted(async () => {
  try {
    stations.value = await getStations()
  } catch (e) {
    error.value = `无法加载工位，请先启动 API：${e.message}`
  }
})

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
</script>

<template>
  <h1>FakeMes 条码追溯</h1>
  <p class="sub">模拟数采写入进站/出站后，在这里按条码查询时间线</p>

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
    <p class="hint">Simulator 控制台里打印的 barcode 可以直接复制过来查</p>
    <p v-if="error" class="error">{{ error }}</p>
  </div>

  <div class="card">
    <h3 style="margin-top: 0">工位主数据</h3>
    <div class="stations">
      <span v-for="s in stations" :key="s.code" class="chip">
        {{ s.code }} · {{ s.name }}
      </span>
      <span v-if="!stations.length" class="hint">暂无</span>
    </div>
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
