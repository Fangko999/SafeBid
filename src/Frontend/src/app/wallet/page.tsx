"use client"

import { useEffect, useState } from "react"
import { useForm } from "react-hook-form"
import { zodResolver } from "@hookform/resolvers/zod"
import { z } from "zod"
import { toast } from "sonner"
import { Loader2, Wallet, Plus, ArrowRightLeft, AlertCircle } from "lucide-react"

import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table"
import { Skeleton } from "@/components/ui/skeleton"
import { Pagination, PaginationContent, PaginationItem, PaginationLink, PaginationNext, PaginationPrevious } from "@/components/ui/pagination"

import { fetchWalletBalance, fetchWalletTransactions, withdrawWithRetry } from "@/services/walletService"

const withdrawSchema = z.object({
  amount: z.coerce.number({ invalid_type_error: "Vui lòng nhập số" }).min(10000, "Tối thiểu 10,000 VND"),
})
type WithdrawValues = z.infer<typeof withdrawSchema>

type Transaction = {
  id: string
  amount: number
  type: string
  status: string
  createdAt: string
}

export default function WalletPage() {
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(false)
  const [balance, setBalance] = useState({ availableBalance: 0, holdAmount: 0 })
  
  const [transactions, setTransactions] = useState<Transaction[]>([])
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  
  const [isWithdrawing, setIsWithdrawing] = useState(false)

  const { register, handleSubmit, reset, formState: { errors } } = useForm<WithdrawValues>({
    resolver: zodResolver(withdrawSchema)
  })

  const loadData = async (currentPage = page) => {
    try {
      setLoading(true)
      setError(false)
      const [balanceData, txData] = await Promise.all([
        fetchWalletBalance(),
        fetchWalletTransactions(currentPage, 10)
      ])
      setBalance(balanceData)
      setTransactions(txData.items || [])
      setTotalPages(txData.totalPages || 1)
      setPage(currentPage)
    } catch {
      setError(true)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    // eslint-disable-next-line react-hooks/exhaustive-deps
    void loadData(1)
  }, [])

  const onSubmit = async (data: WithdrawValues) => {
    try {
      setIsWithdrawing(true)
      await withdrawWithRetry(data.amount)
      toast.success("Rút tiền thành công!")
      reset()
      loadData(1)
    } catch (err: unknown) {
      const error = err as Error
      toast.error(error.message)
    } finally {
      setIsWithdrawing(false)
    }
  }

  const formatVnd = (val: number) => {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(val)
  }

  if (loading) {
    return (
      <div className="container mx-auto p-4 md:p-8 font-sans space-y-6" data-testid="wallet-loading-skeleton">
        <div className="grid md:grid-cols-3 gap-6">
          <Skeleton className="h-48 md:col-span-2 rounded-xl bg-slate-800" />
          <Skeleton className="h-48 rounded-xl bg-slate-800" />
        </div>
        <Skeleton className="h-96 rounded-xl bg-slate-800" />
      </div>
    )
  }

  if (error) {
    return (
      <div className="container mx-auto p-4 md:p-8 flex flex-col items-center justify-center min-h-[50vh] space-y-4">
        <AlertCircle className="h-16 w-16 text-red-500" />
        <h2 className="text-xl font-bold font-outfit text-slate-100">Không tải được số dư</h2>
        <Button onClick={() => loadData(page)} variant="outline">Thử lại</Button>
      </div>
    )
  }

  return (
    <div className="container mx-auto p-4 md:p-8 font-sans space-y-8 bg-slate-950 min-h-screen text-slate-100">
      
      {/* Header */}
      <div>
        <h1 className="text-3xl font-bold font-outfit tracking-tight">Ví điện tử</h1>
        <p className="text-slate-400 mt-2">Quản lý số dư và lịch sử giao dịch của bạn</p>
      </div>

      <div className="grid md:grid-cols-3 gap-6">
        
        {/* Balance Card */}
        <Card className="md:col-span-2 bg-slate-900 border-slate-800 shadow-xl overflow-hidden relative">
          <div className="absolute top-0 right-0 p-8 opacity-10">
            <Wallet size={120} />
          </div>
          <CardHeader>
            <CardTitle className="text-slate-400 text-sm font-medium">Số dư khả dụng</CardTitle>
          </CardHeader>
          <CardContent className="space-y-6">
            <div className="text-4xl md:text-5xl font-bold font-outfit text-emerald-400">
              {formatVnd(balance.availableBalance)}
            </div>
            
            <div className="flex items-center space-x-2 text-slate-400 bg-slate-800/50 w-fit px-4 py-2 rounded-full backdrop-blur-sm">
              <span className="text-sm">Đang đóng băng (Hold):</span>
              <span className="font-semibold text-amber-400">{formatVnd(balance.holdAmount)}</span>
            </div>
          </CardContent>
        </Card>

        {/* Action Card */}
        <Card className="bg-slate-900 border-slate-800 shadow-xl">
          <CardHeader>
            <CardTitle>Rút tiền</CardTitle>
            <CardDescription className="text-slate-400">Rút tiền về tài khoản ngân hàng</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              <div className="space-y-2">
                <Input
                  type="number"
                  placeholder="Số tiền (VND)"
                  {...register("amount")}
                  className="bg-slate-950 border-slate-700"
                />
                {errors.amount && <p className="text-red-500 text-sm">{errors.amount.message}</p>}
              </div>
              <Button 
                type="submit" 
                className="w-full bg-emerald-600 hover:bg-emerald-700 active:scale-95 transition-transform"
                disabled={isWithdrawing}
              >
                {isWithdrawing ? <Loader2 className="mr-2 h-4 w-4 animate-spin" /> : <ArrowRightLeft className="mr-2 h-4 w-4" />}
                Rút tiền
              </Button>
              <Button variant="outline" type="button" className="w-full border-slate-700 text-slate-300 hover:bg-slate-800">
                <Plus className="mr-2 h-4 w-4" /> Nạp tiền
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>

      {/* Transactions List */}
      <Card className="bg-slate-900 border-slate-800 shadow-xl">
        <CardHeader>
          <CardTitle>Lịch sử giao dịch</CardTitle>
        </CardHeader>
        <CardContent>
          {transactions.length === 0 ? (
            <div className="flex flex-col items-center py-12 text-slate-400">
              <Wallet className="h-12 w-12 mb-4 opacity-50" />
              <p>Chưa có giao dịch nào.</p>
            </div>
          ) : (
            <>
              <div className="rounded-md border border-slate-800 overflow-hidden">
                <Table>
                  <TableHeader className="bg-slate-950">
                    <TableRow className="border-slate-800 hover:bg-transparent">
                      <TableHead className="text-slate-400">Thời gian</TableHead>
                      <TableHead className="text-slate-400">Loại</TableHead>
                      <TableHead className="text-slate-400 text-right">Số tiền</TableHead>
                      <TableHead className="text-slate-400 text-right">Trạng thái</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {transactions.map((tx) => (
                      <TableRow key={tx.id} className="border-slate-800 hover:bg-slate-800/50">
                        <TableCell>{new Date(tx.createdAt).toLocaleString('vi-VN')}</TableCell>
                        <TableCell>
                          <Badge variant={tx.type === 'DEPOSIT' ? 'default' : 'secondary'} className={tx.type === 'DEPOSIT' ? 'bg-emerald-500/20 text-emerald-400 hover:bg-emerald-500/30' : 'bg-slate-800 text-slate-300'}>
                            {tx.type}
                          </Badge>
                        </TableCell>
                        <TableCell className={`text-right font-medium ${tx.type === 'DEPOSIT' ? 'text-emerald-400' : 'text-slate-100'}`}>
                          {tx.type === 'DEPOSIT' ? '+' : '-'}{formatVnd(tx.amount)}
                        </TableCell>
                        <TableCell className="text-right">
                          <Badge variant="outline" className="border-slate-700 text-slate-400">
                            {tx.status}
                          </Badge>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </div>
              
              {totalPages > 1 && (
                <div className="mt-4 flex justify-center">
                  <Pagination>
                    <PaginationContent>
                      <PaginationItem>
                        <PaginationPrevious 
                          onClick={() => page > 1 && loadData(page - 1)}
                          className={`cursor-pointer ${page <= 1 ? 'opacity-50 pointer-events-none' : ''}`}
                        />
                      </PaginationItem>
                      
                      {[...Array(totalPages)].map((_, i) => (
                        <PaginationItem key={i}>
                          <PaginationLink 
                            isActive={page === i + 1}
                            onClick={() => loadData(i + 1)}
                            className="cursor-pointer"
                          >
                            {i + 1}
                          </PaginationLink>
                        </PaginationItem>
                      ))}

                      <PaginationItem>
                        <PaginationNext 
                          onClick={() => page < totalPages && loadData(page + 1)}
                          className={`cursor-pointer ${page >= totalPages ? 'opacity-50 pointer-events-none' : ''}`}
                        />
                      </PaginationItem>
                    </PaginationContent>
                  </Pagination>
                </div>
              )}
            </>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
