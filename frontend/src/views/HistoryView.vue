<template>
  <div class="history-view">
    <div class="header">
      <h1>Prediction History</h1>
      <p>View historical prediction performance and score updates</p>
    </div>
    
    <div class="content">
      <div class="info-message">
        <p>This page shows the complete history of prediction scores and updates.</p>
        <p>Use the Predictions page to view detailed score history for individual predictions.</p>
      </div>
      
      <div class="navigation-links">
        <router-link to="/predictions" class="btn btn-primary">
          View Current Predictions
        </router-link>
        <router-link to="/draws" class="btn btn-secondary">
          View Draw History
        </router-link>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
// This is a basic history view component
// Future enhancements could include:
// - Full prediction history table
// - Score trend charts
// - Performance analytics
</script>

<style scoped>
.history-view {
  padding: 2rem;
  max-width: 1200px;
  margin: 0 auto;
}

.header {
  text-align: center;
  margin-bottom: 2rem;
}

.header h1 {
  color: #2c3e50;
  margin-bottom: 0.5rem;
}

.header p {
  color: #7f8c8d;
  font-size: 1.1rem;
}

.content {
  background: white;
  border-radius: 8px;
  padding: 2rem;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
}

.info-message {
  text-align: center;
  margin-bottom: 2rem;
  padding: 1rem;
  background: #f8f9fa;
  border-radius: 6px;
  border-left: 4px solid #3498db;
}

.info-message p {
  margin: 0.5rem 0;
  color: #5a6c7d;
}

.navigation-links {
  display: flex;
  gap: 1rem;
  justify-content: center;
}

.btn {
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  text-decoration: none;
  font-weight: 500;
  transition: all 0.2s ease;
}

.btn-primary {
  background: #3498db;
  color: white;
}

.btn-primary:hover {
  background: #2980b9;
}

.btn-secondary {
  background: #95a5a6;
  color: white;
}

.btn-secondary:hover {
  background: #7f8c8d;
}
</style>