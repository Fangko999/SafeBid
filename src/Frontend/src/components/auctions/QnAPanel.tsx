import { useState, useEffect } from 'react';

interface Question {
  id: string;
  maskedAskerName: string;
  content: string;
  createdAt: string;
  answer: string | null;
  answeredAt: string | null;
}

interface QnAPanelProps {
  auctionId: string;
  isSeller: boolean;
  isAuthenticated: boolean;
  initialQuestions?: Question[]; // For testing
}

export function QnAPanel({ auctionId, isSeller, isAuthenticated, initialQuestions }: QnAPanelProps) {
  const [questions, setQuestions] = useState<Question[]>(initialQuestions || []);
  const [loading, setLoading] = useState(!initialQuestions);
  const [newQuestion, setNewQuestion] = useState('');
  const [replyingTo, setReplyingTo] = useState<string | null>(null);
  const [answerText, setAnswerText] = useState('');

  useEffect(() => {
    if (initialQuestions) return;
    
    // Adjust fetch to use the proxy or direct backend URL
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    // For test mock matching, we'll try to just use relative `/api` if Next proxy is configured, 
    // or we'll use a wrapper. Let's use the `/api` relative URL so testing matches exactly.
    
    fetch(`/api/auctions/${auctionId}/questions`)
      .then(res => res.json())
      .then(data => {
        setQuestions(data.items || []);
        setLoading(false);
      })
      .catch(err => {
        console.error(err);
        setLoading(false);
      });
  }, [auctionId, initialQuestions]);

  const submitQuestion = async () => {
    if (!newQuestion.trim()) return;
    try {
      const res = await fetch(`/api/auctions/${auctionId}/questions`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ content: newQuestion })
      });
      if (res.ok) {
        setNewQuestion('');
        if (res.status === 201) {
            const data = await res.json();
            setQuestions(prev => [...prev, { id: data.id, maskedAskerName: "Bạn***", content: newQuestion, createdAt: new Date().toISOString(), answer: null, answeredAt: null }]);
        } else {
            const latest = await fetch(`/api/auctions/${auctionId}/questions`).then(r => r.json());
            setQuestions(latest.items || []);
        }
      } else if (res.status === 429) {
        alert("Bạn đã đặt câu hỏi quá nhanh. Vui lòng đợi một lát.");
      } else {
        alert("Lỗi khi gửi câu hỏi. Vui lòng thử lại sau.");
      }
    } catch (e) {
      console.error(e);
    }
  };

  const submitAnswer = async (questionId: string) => {
    if (!answerText.trim()) return;
    try {
      const res = await fetch(`/api/auctions/${auctionId}/questions/${questionId}/answer`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ answer: answerText })
      });
      if (res.ok) {
        setReplyingTo(null);
        setAnswerText('');
        setQuestions(prev => prev.map(q => q.id === questionId ? { ...q, answer: answerText, answeredAt: new Date().toISOString() } : q));
      } else {
        alert("Lỗi khi gửi câu trả lời.");
      }
    } catch (e) {
      console.error(e);
    }
  };

  if (loading) return <div className="p-4 bg-slate-800 rounded-xl animate-pulse h-32"></div>;

  return (
    <div className="bg-slate-800/80 p-6 rounded-xl border border-slate-700">
      <h3 className="text-xl font-bold mb-4">Hỏi Đáp</h3>
      
      <div className="space-y-4 mb-6">
        {questions.length === 0 ? (
          <p className="text-slate-400">Chưa có câu hỏi nào.</p>
        ) : (
          questions.map(q => (
            <div key={q.id} className="bg-slate-900/50 p-4 rounded-lg border border-slate-700/50">
              <div className="flex justify-between items-start mb-2">
                <span className="font-semibold text-emerald-400">{q.maskedAskerName}</span>
                <span className="text-xs text-slate-500">{new Date(q.createdAt).toLocaleString('vi-VN')}</span>
              </div>
              <p className="text-slate-300 mb-2">{q.content}</p>
              
              {q.answer ? (
                <div className="bg-slate-800 p-3 rounded border border-slate-600/50 mt-3 relative">
                  <div className="absolute -top-2 left-4 w-4 h-4 bg-slate-800 border-l border-t border-slate-600/50 transform rotate-45"></div>
                  <div className="flex justify-between items-start mb-1">
                    <span className="font-semibold text-amber-400 text-sm">Chủ phiên trả lời:</span>
                    <span className="text-xs text-slate-500">{new Date(q.answeredAt!).toLocaleString('vi-VN')}</span>
                  </div>
                  <p className="text-slate-300 text-sm">{q.answer}</p>
                </div>
              ) : isSeller && (
                <div className="mt-3">
                  {replyingTo === q.id ? (
                    <div className="flex gap-2">
                      <input 
                        type="text" 
                        value={answerText}
                        onChange={e => setAnswerText(e.target.value)}
                        placeholder="Nhập câu trả lời..." 
                        className="flex-1 bg-slate-800 border border-slate-600 rounded px-3 py-1 text-sm focus:outline-none focus:border-emerald-500"
                      />
                      <button onClick={() => submitAnswer(q.id)} className="bg-emerald-600 hover:bg-emerald-500 text-white px-3 py-1 rounded text-sm transition-colors">
                        Gửi trả lời
                      </button>
                      <button onClick={() => setReplyingTo(null)} className="text-slate-400 hover:text-slate-300 px-2 py-1 text-sm">
                        Hủy
                      </button>
                    </div>
                  ) : (
                    <button onClick={() => setReplyingTo(q.id)} className="text-emerald-500 hover:text-emerald-400 text-sm font-semibold transition-colors">
                      &#8618; Trả lời
                    </button>
                  )}
                </div>
              )}
            </div>
          ))
        )}
      </div>

      <div className="border-t border-slate-700 pt-4">
        {isAuthenticated ? (
          <div className="flex gap-3">
            <input 
              type="text" 
              value={newQuestion}
              onChange={e => setNewQuestion(e.target.value)}
              placeholder="Nhập câu hỏi của bạn..." 
              className="flex-1 bg-slate-900 border border-slate-700 rounded-lg px-4 py-2 focus:outline-none focus:border-emerald-500 transition-colors"
            />
            <button 
              onClick={submitQuestion}
              disabled={!newQuestion.trim()}
              className="bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 disabled:cursor-not-allowed text-white px-6 py-2 rounded-lg font-semibold transition-colors flex-shrink-0"
            >
              Gửi câu hỏi
            </button>
          </div>
        ) : (
          <p className="text-slate-400 text-center italic">Vui lòng đăng nhập để đặt câu hỏi.</p>
        )}
      </div>
    </div>
  );
}
