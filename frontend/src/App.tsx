import { useState } from 'react'
import { Routes, Route, Navigate } from 'react-router-dom'
import { LoginPage } from './pages/LoginPage'

export function App() {
  const [isAuthenticated, setIsAuthenticated] = useState(false)

  const handleLogin = () => {
    setIsAuthenticated(true)
  }

  return (
    <Routes>
      <Route
        path="/"
        element={
          isAuthenticated ? (
            <Navigate to="/dashboard" replace />
          ) : (
            <LoginPage onLogin={handleLogin} />
          )
        }
      />
      <Route
        path="/dashboard"
        element={
          isAuthenticated ? (
            <div className="flex items-center justify-center min-h-screen bg-gray-100">
              <h1 className="text-3xl font-bold text-gray-800">Dashboard — Coming Soon</h1>
            </div>
          ) : (
            <Navigate to="/" replace />
          )
        }
      />
    </Routes>
  )
}
