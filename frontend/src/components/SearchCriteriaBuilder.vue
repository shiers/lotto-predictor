<template>
  <div class="search-criteria-builder">
    <div class="criteria-header">
      <h3>Search Criteria</h3>
      <button @click="clearCriteria" class="clear-btn">Clear All</button>
    </div>
    
    <div class="criteria-form">
      <div class="form-group">
        <label for="dateRange">Date Range:</label>
        <div class="date-inputs">
          <input
            id="startDate"
            v-model="criteria.startDate"
            type="date"
            class="form-control"
            @change="updateCriteria"
          />
          <span>to</span>
          <input
            id="endDate"
            v-model="criteria.endDate"
            type="date"
            class="form-control"
            @change="updateCriteria"
          />
        </div>
      </div>
      
      <div class="form-group">
        <label for="drawRange">Draw Range:</label>
        <div class="range-inputs">
          <input
            id="minDraw"
            v-model.number="criteria.minDraw"
            type="number"
            placeholder="Min draw"
            class="form-control"
            @input="updateCriteria"
          />
          <span>to</span>
          <input
            id="maxDraw"
            v-model.number="criteria.maxDraw"
            type="number"
            placeholder="Max draw"
            class="form-control"
            @input="updateCriteria"
          />
        </div>
      </div>
      
      <div class="form-group">
        <label>Numbers to Include:</label>
        <div class="number-selection">
          <div
            v-for="num in 40"
            :key="num"
            class="number-chip"
            :class="{ active: criteria.includeNumbers.includes(num) }"
            @click="toggleNumber(num, 'include')"
          >
            {{ num }}
          </div>
        </div>
      </div>
      
      <div class="form-group">
        <label>Numbers to Exclude:</label>
        <div class="number-selection">
          <div
            v-for="num in 40"
            :key="num"
            class="number-chip"
            :class="{ active: criteria.excludeNumbers.includes(num) }"
            @click="toggleNumber(num, 'exclude')"
          >
            {{ num }}
          </div>
        </div>
      </div>
      
      <div class="form-group">
        <label for="powerball">Powerball:</label>
        <select
          id="powerball"
          v-model="criteria.powerball"
          class="form-control"
          @change="updateCriteria"
        >
          <option value="">Any</option>
          <option v-for="num in 10" :key="num" :value="num">{{ num }}</option>
        </select>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, watch } from 'vue'

interface SimpleCriteria {
  startDate: string
  endDate: string
  minDraw: number | undefined
  maxDraw: number | undefined
  includeNumbers: number[]
  excludeNumbers: number[]
  powerball: string
}

const emit = defineEmits<{
  criteriaChanged: [criteria: SimpleCriteria]
}>()

const criteria = reactive<SimpleCriteria>({
  startDate: '',
  endDate: '',
  minDraw: undefined,
  maxDraw: undefined,
  includeNumbers: [],
  excludeNumbers: [],
  powerball: ''
})

const updateCriteria = () => {
  emit('criteriaChanged', { ...criteria })
}

const toggleNumber = (num: number, type: 'include' | 'exclude') => {
  if (type === 'include') {
    const index = criteria.includeNumbers.indexOf(num)
    if (index > -1) {
      criteria.includeNumbers.splice(index, 1)
    } else {
      criteria.includeNumbers.push(num)
      // Remove from exclude if it was there
      const excludeIndex = criteria.excludeNumbers.indexOf(num)
      if (excludeIndex > -1) {
        criteria.excludeNumbers.splice(excludeIndex, 1)
      }
    }
  } else {
    const index = criteria.excludeNumbers.indexOf(num)
    if (index > -1) {
      criteria.excludeNumbers.splice(index, 1)
    } else {
      criteria.excludeNumbers.push(num)
      // Remove from include if it was there
      const includeIndex = criteria.includeNumbers.indexOf(num)
      if (includeIndex > -1) {
        criteria.includeNumbers.splice(includeIndex, 1)
      }
    }
  }
  updateCriteria()
}

const clearCriteria = () => {
  criteria.startDate = ''
  criteria.endDate = ''
  criteria.minDraw = undefined
  criteria.maxDraw = undefined
  criteria.includeNumbers.length = 0
  criteria.excludeNumbers.length = 0
  criteria.powerball = ''
  updateCriteria()
}

// Watch for changes and emit
watch(criteria, () => {
  updateCriteria()
}, { deep: true })
</script>

<style scoped>
.search-criteria-builder {
  background: white;
  border-radius: 8px;
  padding: 1.5rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.criteria-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 1.5rem;
  padding-bottom: 0.5rem;
  border-bottom: 1px solid #e9ecef;
}

.criteria-header h3 {
  margin: 0;
  color: #2c3e50;
}

.clear-btn {
  background: #dc3545;
  color: white;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 4px;
  cursor: pointer;
  font-size: 0.875rem;
  transition: background-color 0.2s;
}

.clear-btn:hover {
  background: #c82333;
}

.form-group {
  margin-bottom: 1.5rem;
}

.form-group label {
  display: block;
  margin-bottom: 0.5rem;
  font-weight: 500;
  color: #495057;
}

.form-control {
  width: 100%;
  padding: 0.5rem;
  border: 1px solid #ced4da;
  border-radius: 4px;
  font-size: 1rem;
}

.form-control:focus {
  outline: none;
  border-color: #3498db;
  box-shadow: 0 0 0 2px rgba(52, 152, 219, 0.2);
}

.date-inputs,
.range-inputs {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.date-inputs input,
.range-inputs input {
  flex: 1;
}

.date-inputs span,
.range-inputs span {
  color: #6c757d;
  font-weight: 500;
}

.number-selection {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(40px, 1fr));
  gap: 0.5rem;
  max-height: 200px;
  overflow-y: auto;
  padding: 0.5rem;
  border: 1px solid #e9ecef;
  border-radius: 4px;
  background: #f8f9fa;
}

.number-chip {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40px;
  height: 40px;
  border: 1px solid #ced4da;
  border-radius: 50%;
  background: white;
  cursor: pointer;
  font-weight: 500;
  transition: all 0.2s;
  user-select: none;
}

.number-chip:hover {
  border-color: #3498db;
  background: #e3f2fd;
}

.number-chip.active {
  background: #3498db;
  color: white;
  border-color: #2980b9;
}

@media (max-width: 768px) {
  .date-inputs,
  .range-inputs {
    flex-direction: column;
    align-items: stretch;
  }
  
  .date-inputs span,
  .range-inputs span {
    text-align: center;
    margin: 0.25rem 0;
  }
  
  .number-selection {
    grid-template-columns: repeat(auto-fill, minmax(35px, 1fr));
  }
  
  .number-chip {
    width: 35px;
    height: 35px;
    font-size: 0.875rem;
  }
}
</style>