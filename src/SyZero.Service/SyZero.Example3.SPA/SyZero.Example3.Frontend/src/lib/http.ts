import axios from 'axios'

/**
 * 全局 axios 实例：
 * - 开发期通过 Vite proxy 转发到 http://localhost:5004
 * - 生产期由 ASP.NET Core 单体宿主同源提供 /api 接口
 */
export const http = axios.create({
  baseURL: '/api',
  timeout: 30_000,
})

http.interceptors.response.use((response) => {
  // SyZero 统一包装：{ code, data }
  const payload = response.data
  if (payload && typeof payload === 'object' && 'code' in payload && 'data' in payload) {
    return { ...response, data: payload.data }
  }
  return response
})
