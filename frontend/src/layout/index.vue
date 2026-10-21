<template>
  <div class="app-wrapper">
    <Sidebar :is-collapse="isCollapse" />

    <div class="main-container">
      <Navbar
        :is-collapse="isCollapse"
        @toggle-collapse="isCollapse = !isCollapse"
      />
      <TagsView />
      <AppMain />
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import Sidebar from './components/Sidebar.vue'
import Navbar from './components/Navbar.vue'
import TagsView from './components/TagsView.vue'
import AppMain from './components/AppMain.vue'

const isCollapse = ref(false)
</script>

<style scoped lang="scss">
.app-wrapper {
  display: flex;
  width: 100%;
  height: 100%;
  overflow: hidden;

  .main-container {
    flex: 1;
    display: flex;
    flex-direction: column;
    overflow: hidden;
    height: 100%;
  }
}
</style>
