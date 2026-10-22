import { useQuery } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { ExampleApi } from '@/client/example-api'

export function HomePage() {
  const { data, isLoading, refetch, isFetching } = useQuery({
    queryKey: ['service-info'],
    queryFn: ExampleApi.getServiceInfo,
  })

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-semibold">SyZero.Example3 SPA 单体宿主示例</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          ASP.NET Core 9 + SyZero 框架 + React 19 + Vite + Tailwind v4
        </p>
      </div>

      <section className="rounded-lg border border-border bg-card p-6">
        <h2 className="mb-3 text-lg font-medium">服务信息</h2>
        {isLoading ? (
          <p className="text-sm text-muted-foreground">加载中…</p>
        ) : data ? (
          <dl className="grid grid-cols-2 gap-3 text-sm sm:grid-cols-4">
            <div>
              <dt className="text-muted-foreground">服务名</dt>
              <dd className="font-medium">{data.serviceName}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">版本</dt>
              <dd className="font-medium">{data.version}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">端口</dt>
              <dd className="font-medium">{data.port}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">时间</dt>
              <dd className="font-medium">{data.timestamp}</dd>
            </div>
          </dl>
        ) : (
          <p className="text-sm text-muted-foreground">无数据</p>
        )}
        <div className="mt-4">
          <Button onClick={() => refetch()} disabled={isFetching}>
            {isFetching ? '刷新中…' : '刷新'}
          </Button>
        </div>
      </section>
    </div>
  )
}
