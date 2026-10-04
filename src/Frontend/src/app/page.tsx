'use client';
import { useEffect, useState } from 'react';

export default function Home() {
  const [health, setHealth] = useState<string>('Loading...');

  useEffect(() => {
    fetch('http://localhost:5000/api/health')
      .then(res => res.json())
      .then(data => setHealth(data.status))
      .catch(err => setHealth('Error fetching health: ' + err.message));
  }, []);

  return (
    <main className="flex min-h-screen flex-col items-center justify-center p-24 bg-slate-900 text-white">
      <h1 className="text-4xl font-bold mb-8 text-emerald-500">SafeBid C2C Auction</h1>
      <div className="p-6 bg-slate-800 rounded-lg shadow-lg border border-slate-700">
        <p className="text-2xl font-mono text-amber-400">{health}</p>
      </div>
    </main>
  );
}
