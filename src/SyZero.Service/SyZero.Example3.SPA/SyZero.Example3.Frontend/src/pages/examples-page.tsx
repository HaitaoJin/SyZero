import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { Button } from '@/components/ui/button'
import { ExampleApi } from '@/client/example-api'

export function ExamplesPage() {
  const [id, setId] = useState(1)
  const { data, isFetching, refetch } = useQuery({
    queryKey: ['example', id],
    queryFn: () => ExampleApi.getExample(id),
  })

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-semibold">示例数据</h1>
      <div className="flex items-center gap-3">
        <label className="text-sm text-muted-foreground">ID</label>
        <input
          type="number"
          value={id}
          onChange={(e) => setId(Number(e.target.value) || 1)}
          className="h-9 w-32 rounded-md border border-border bg-background px-3 text-sm"
        />
        <Button onClick={() => refetch()} disabled={isFetching}>
          查询
        </Button>
      </div>
      <pre className="overflow-auto rounded-lg border border-border bg-card p-4 text-xs">
        {JSON.stringify(data, null, 2)}
      </pre>
    </div>
  )
}
