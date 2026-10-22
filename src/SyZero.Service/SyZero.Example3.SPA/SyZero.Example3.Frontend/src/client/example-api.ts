import { http } from '@/lib/http'

export interface ServiceInfo {
  serviceName: string
  version: string
  port: number
  timestamp: string
}

export interface Example {
  id: string
  name: string
  description: string
  status: number
  sort: number
  createTime: string
  updateTime: string
}

export const ExampleApi = {
  getServiceInfo: async () => {
    const res = await http.get<ServiceInfo>('/SyZero.Example3/Example3/GetServiceInfo')
    return res.data
  },
  getExample: async (id: number) => {
    const res = await http.get<Example>('/SyZero.Example3/Example3/GetExample', { params: { id } })
    return res.data
  },
}
