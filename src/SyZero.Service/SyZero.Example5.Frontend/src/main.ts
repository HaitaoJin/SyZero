import './style.css'

interface ServiceDefinition {
  key: 'example1' | 'example2'
  url: string
}

interface ServiceInfo {
  serviceName?: string
  version?: string
  port?: number
  timestamp?: string
}

interface ApiEnvelope<T> {
  data: T
}

interface ServiceCallDefinition {
  key: 'example1-to-example2' | 'example2-to-example1'
  url: string
}

interface RemoteServiceResult {
  localService?: string
  remoteService?: string
  remoteVersion?: string
  remotePort?: number
  callTime?: string
  success?: boolean
  message?: string
}

const services: ServiceDefinition[] = [
  {
    key: 'example1',
    url: '/example1/api/SyZero.Example1/Example1/GetServiceInfo',
  },
  {
    key: 'example2',
    url: '/example2/api/SyZero.Example2/Example2/GetServiceInfo',
  },
]

const serviceCalls: ServiceCallDefinition[] = [
  {
    key: 'example1-to-example2',
    url: '/example1/api/SyZero.Example1/Example1/CallExample2Service',
  },
  {
    key: 'example2-to-example1',
    url: '/example2/api/SyZero.Example2/Example2/CallExample1Service',
  },
]

function getElement<T extends Element>(selector: string, root: ParentNode = document): T {
  const element = root.querySelector<T>(selector)
  if (!element) throw new Error(`找不到页面元素：${selector}`)
  return element
}

function unwrap<T>(payload: T | ApiEnvelope<T>): T {
  if (payload && typeof payload === 'object' && 'data' in payload) {
    return (payload as ApiEnvelope<T>).data
  }
  return payload as T
}

async function checkService(service: ServiceDefinition): Promise<boolean> {
  const card = getElement<HTMLElement>(`[data-service="${service.key}"]`)
  const status = getElement<HTMLElement>('.status', card)
  const started = performance.now()

  status.className = 'status loading'
  status.textContent = '连接中'
  getElement<HTMLElement>('[data-field="error"]', card).textContent = ''

  try {
    const response = await fetch(service.url, { headers: { Accept: 'application/json' } })
    if (!response.ok) throw new Error(`HTTP ${response.status}`)
    const data = unwrap<ServiceInfo>(await response.json() as ServiceInfo | ApiEnvelope<ServiceInfo>)

    getElement<HTMLElement>('[data-field="version"]', card).textContent = data.version ?? '—'
    getElement<HTMLElement>('[data-field="port"]', card).textContent = data.port?.toString() ?? '—'
    getElement<HTMLElement>('[data-field="latency"]', card).textContent = `${Math.round(performance.now() - started)} ms`
    status.className = 'status online'
    status.textContent = '正常'
    return true
  } catch (error) {
    status.className = 'status offline'
    status.textContent = '异常'
    getElement<HTMLElement>('[data-field="error"]', card).textContent = error instanceof Error ? error.message : '请求失败'
    return false
  }
}

async function refresh(): Promise<void> {
  const button = getElement<HTMLButtonElement>('#refresh')
  button.disabled = true
  try {
    const results = await Promise.all(services.map(checkService))
    const online = results.filter(Boolean).length
    getElement<HTMLElement>('#summary').textContent = `${online} / ${services.length} 个服务可通过网关访问`
  } finally {
    button.disabled = false
  }
}

function setCallField(card: HTMLElement, field: string, value: string): void {
  getElement<HTMLElement>(`[data-field="${field}"]`, card).textContent = value
}

async function invokeServiceCall(call: ServiceCallDefinition): Promise<boolean> {
  const card = getElement<HTMLElement>(`[data-call="${call.key}"]`)
  const status = getElement<HTMLElement>('.status', card)
  const button = getElement<HTMLButtonElement>('.call-button', card)
  const started = performance.now()

  button.disabled = true
  status.className = 'status loading'
  status.textContent = '调用中'
  setCallField(card, 'message', '正在通过 Gateway 验证服务间调用链路…')

  try {
    const response = await fetch(call.url, { headers: { Accept: 'application/json' } })
    if (!response.ok) throw new Error(`HTTP ${response.status}`)

    const data = unwrap<RemoteServiceResult>(await response.json() as RemoteServiceResult | ApiEnvelope<RemoteServiceResult>)
    const succeeded = data.success === true

    setCallField(card, 'remote-service', data.remoteService ?? '—')
    setCallField(card, 'remote-version', data.remoteVersion ?? '—')
    setCallField(card, 'remote-port', data.remotePort?.toString() ?? '—')
    setCallField(card, 'call-time', data.callTime ?? '—')
    setCallField(card, 'latency', `${Math.round(performance.now() - started)} ms`)
    setCallField(card, 'message', data.message ?? (succeeded ? '服务间调用成功' : '服务间调用失败'))

    status.className = `status ${succeeded ? 'online' : 'offline'}`
    status.textContent = succeeded ? '成功' : '失败'
    return succeeded
  } catch (error) {
    status.className = 'status offline'
    status.textContent = '异常'
    setCallField(card, 'latency', `${Math.round(performance.now() - started)} ms`)
    setCallField(card, 'message', error instanceof Error ? error.message : '请求失败')
    return false
  } finally {
    button.disabled = false
  }
}

async function invokeAllServiceCalls(): Promise<void> {
  const button = getElement<HTMLButtonElement>('#call-all')
  button.disabled = true
  try {
    await Promise.all(serviceCalls.map(invokeServiceCall))
  } finally {
    button.disabled = false
  }
}

getElement<HTMLButtonElement>('#refresh').addEventListener('click', () => void refresh())
getElement<HTMLButtonElement>('#call-all').addEventListener('click', () => void invokeAllServiceCalls())
serviceCalls.forEach((call) => {
  const card = getElement<HTMLElement>(`[data-call="${call.key}"]`)
  getElement<HTMLButtonElement>('.call-button', card).addEventListener('click', () => void invokeServiceCall(call))
})
void refresh()
