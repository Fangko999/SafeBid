"use client"

import { useEffect, useRef } from "react"
import { useAuthStore } from "@/store/authStore"

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const { checkAuth } = useAuthStore()
  const isChecked = useRef(false)

  useEffect(() => {
    if (!isChecked.current) {
      checkAuth()
      isChecked.current = true
    }
  }, [checkAuth])

  return <>{children}</>
}
