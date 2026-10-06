'use client';
import { useState, useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';

const auctionSchema = z.object({
  title: z.string().min(1, 'Tên phiên đấu giá không được để trống'),
  categoryId: z.string().min(1, 'Danh mục không được để trống'),
  startPrice: z.coerce.number().min(1, 'Giá khởi điểm phải lớn hơn 0'),
  stepPrice: z.coerce.number().min(1, 'Bước giá phải lớn hơn 0'),
  reservePrice: z.coerce.number().optional(),
  buyNowPrice: z.coerce.number().optional(),
  startTime: z.string().min(1, 'Thời gian bắt đầu không được để trống'),
  endTime: z.string().min(1, 'Thời gian kết thúc không được để trống'),
});

type AuctionFormValues = z.infer<typeof auctionSchema>;

export default function CreateAuctionPage() {
  const [categories, setCategories] = useState<{id: string, name: string}[]>([]);
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<AuctionFormValues>({
    resolver: zodResolver(auctionSchema)
  });

  const [mediaUrls, setMediaUrls] = useState<string[]>([]);

  useEffect(() => {
    const fetchCategories = async () => {
      // Mock fetch categories
      setCategories([{ id: 'cat-1', name: 'Electronics' }]);
    };
    fetchCategories();
  }, []);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    if (!e.target.files) return;
    const files = Array.from(e.target.files);
    
    // Upload each file
    const urls: string[] = [];
    for (const file of files) {
      const formData = new FormData();
      formData.append('file', file);
      try {
        const res = await fetch('/api/media/upload', { method: 'POST', body: formData });
        if (res.ok) {
          const data = await res.json();
          urls.push(data.url);
        }
      } catch (err) {
        console.error(err);
      }
    }
    setMediaUrls(prev => [...prev, ...urls]);
  };

  const onSubmit = async (data: AuctionFormValues) => {
    try {
      const payload = { ...data, mediaUrls };
      const res = await fetch('/api/auctions', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
      });
      if (res.ok) {
        alert("Lưu nháp thành công!");
      } else {
        const err = await res.json();
        alert("Lỗi: " + err.Error);
      }
    } catch (err) {
      console.error(err);
      alert("Đã có lỗi xảy ra");
    }
  };

  return (
    <div className="container mx-auto p-6 max-w-2xl bg-surface rounded-xl shadow-lg border border-slate-700">
      <h1 className="text-2xl font-bold mb-6 text-white font-outfit">Tạo Phiên Đấu Giá Nháp</h1>
      
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div>
          <label className="block text-sm font-medium mb-1">Tên phiên</label>
          <Input {...register('title')} className="w-full" />
          {errors.title && <p className="text-red-500 text-sm mt-1">{errors.title.message}</p>}
        </div>

        <div>
          <label className="block text-sm font-medium mb-1">Danh mục</label>
          <select {...register('categoryId')} className="w-full p-2 rounded-md bg-slate-800 border border-slate-600">
            <option value="">Chọn danh mục</option>
            {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          {errors.categoryId && <p className="text-red-500 text-sm mt-1">{errors.categoryId.message}</p>}
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1">Giá khởi điểm</label>
            <Input type="number" {...register('startPrice')} />
            {errors.startPrice && <p className="text-red-500 text-sm mt-1">{errors.startPrice.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Bước giá</label>
            <Input type="number" {...register('stepPrice')} />
            {errors.stepPrice && <p className="text-red-500 text-sm mt-1">{errors.stepPrice.message}</p>}
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium mb-1">Thời gian bắt đầu</label>
            <Input type="datetime-local" {...register('startTime')} />
            {errors.startTime && <p className="text-red-500 text-sm mt-1">{errors.startTime.message}</p>}
          </div>
          <div>
            <label className="block text-sm font-medium mb-1">Thời gian kết thúc</label>
            <Input type="datetime-local" {...register('endTime')} />
            {errors.endTime && <p className="text-red-500 text-sm mt-1">{errors.endTime.message}</p>}
          </div>
        </div>

        <div className="border-2 border-dashed border-slate-600 rounded-lg p-8 text-center text-slate-400">
          <input type="file" multiple accept="image/*,video/*" onChange={handleFileChange} />
          {mediaUrls.length > 0 && (
            <div className="mt-4 flex gap-2">
              {mediaUrls.map((url, i) => (
                <img key={i} src={url} alt="uploaded" className="w-16 h-16 object-cover rounded" />
              ))}
            </div>
          )}
        </div>

        <div className="pt-4 flex justify-end">
          <Button type="submit" disabled={isSubmitting} className="bg-emerald-600 hover:bg-emerald-700">
            {isSubmitting ? 'Đang lưu...' : 'Lưu nháp'}
          </Button>
        </div>
      </form>
    </div>
  );
}
