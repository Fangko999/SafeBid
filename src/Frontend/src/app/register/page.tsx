"use client"

import { useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Label } from "@/components/ui/label"
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from "@/components/ui/card"
import { toast } from "sonner"
import { Loader2 } from "lucide-react"

const registerSchema = z.object({
  email: z.string().email("Email không hợp lệ"),
  password: z.string().min(6, "Mật khẩu ít nhất 6 ký tự"),
  confirmPassword: z.string(),
  fullName: z.string().min(2, "Họ tên quá ngắn"),
  phoneNumber: z.string().regex(/^(0|\+?84)[0-9]{9}$/, "Số điện thoại phải bắt đầu bằng 0, 84 hoặc +84 và đủ 10 số"),
}).refine(data => data.password === data.confirmPassword, {
  message: "Mật khẩu không khớp",
  path: ["confirmPassword"],
})

type RegisterValues = z.infer<typeof registerSchema>

export default function RegisterPage() {
  const [isLoading, setIsLoading] = useState(false)

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterValues>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      email: "",
      password: "",
      confirmPassword: "",
      fullName: "",
      phoneNumber: "",
    },
  })

  const onSubmit = async (data: RegisterValues) => {
    setIsLoading(true)
    try {
      const response = await fetch("/api/auth/register", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(data),
      })

      if (response.ok) {
        toast.success("Đăng ký thành công")
        // Typically, you would redirect here
      } else if (response.status === 429) {
        toast.error("Quá nhiều yêu cầu, vui lòng thử lại sau")
      } else {
        const errorData = await response.json().catch(() => ({}))
        toast.error(errorData?.error?.message || "Đã có lỗi xảy ra")
      }
    } catch (error) {
      toast.error("Lỗi kết nối máy chủ")
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-900 p-4 font-sans" data-testid="register-page">
      <Card className="w-full max-w-md bg-slate-800/80 backdrop-blur-md border-slate-700 text-slate-100">
        <CardHeader className="space-y-1">
          <CardTitle className="text-2xl font-bold tracking-tight text-center font-outfit">Đăng Ký Tài Khoản</CardTitle>
          <CardDescription className="text-slate-400 text-center">
            Tham gia nền tảng đấu giá an toàn
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form data-testid="register-form" onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="email">Email</Label>
              <Input
                id="email"
                type="email"
                placeholder="name@example.com"
                data-testid="register-email"
                className="bg-slate-900 border-slate-700 focus-visible:ring-emerald-500"
                {...register("email")}
              />
              {errors.email && (
                <p className="text-sm text-red-500">{errors.email.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="password">Mật khẩu</Label>
              <Input
                id="password"
                type="password"
                data-testid="register-password"
                className="bg-slate-900 border-slate-700 focus-visible:ring-emerald-500"
                {...register("password")}
              />
              {errors.password && (
                <p className="text-sm text-red-500">{errors.password.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="confirmPassword">Xác nhận mật khẩu</Label>
              <Input
                id="confirmPassword"
                type="password"
                data-testid="register-confirm-password"
                className="bg-slate-900 border-slate-700 focus-visible:ring-emerald-500"
                {...register("confirmPassword")}
              />
              {errors.confirmPassword && (
                <p className="text-sm text-red-500">{errors.confirmPassword.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="fullName">Họ và tên</Label>
              <Input
                id="fullName"
                type="text"
                data-testid="register-fullname"
                className="bg-slate-900 border-slate-700 focus-visible:ring-emerald-500"
                {...register("fullName")}
              />
              {errors.fullName && (
                <p className="text-sm text-red-500">{errors.fullName.message}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="phoneNumber">Số điện thoại</Label>
              <Input
                id="phoneNumber"
                type="text"
                data-testid="register-phone"
                className="bg-slate-900 border-slate-700 focus-visible:ring-emerald-500"
                {...register("phoneNumber")}
              />
              {errors.phoneNumber && (
                <p className="text-sm text-red-500">{errors.phoneNumber.message}</p>
              )}
            </div>

            <Button
              type="submit"
              data-testid="submit-btn"
              className="w-full bg-emerald-600 hover:bg-emerald-700 text-white active:scale-95 transition-transform"
              disabled={isLoading}
            >
              {isLoading ? (
                <>
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                  Đang xử lý...
                </>
              ) : (
                "Đăng ký"
              )}
            </Button>
          </form>
          <div data-testid="form-error-summary"></div>
        </CardContent>
      </Card>
    </div>
  )
}
