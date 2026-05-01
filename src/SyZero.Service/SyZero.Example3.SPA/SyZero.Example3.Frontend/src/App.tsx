import { Navigate, Route, Routes } from 'react-router-dom'
import { MainLayout } from '@/layout/main-layout'
import { HomePage } from '@/pages/home-page'
import { ExamplesPage } from '@/pages/examples-page'

function App() {
  return (
    <Routes>
      <Route
        path="/"
        element={
          <MainLayout>
            <HomePage />
          </MainLayout>
        }
      />
      <Route
        path="/examples"
        element={
          <MainLayout>
            <ExamplesPage />
          </MainLayout>
        }
      />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default App
