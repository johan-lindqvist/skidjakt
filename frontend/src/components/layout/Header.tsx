import { Mountain } from 'lucide-react';

export function Header() {
  return (
    <header className="bg-slate-950 border-b border-slate-800">
      <div className="max-w-7xl mx-auto px-4 py-4 flex items-center gap-3">
        <Mountain className="h-8 w-8 text-sky-400" />
        <div>
          <h1 className="text-2xl font-bold text-white tracking-tight">
            Skidjakt
          </h1>
          <p className="text-sm text-slate-400">Hitta billigaste skidresorna</p>
        </div>
      </div>
    </header>
  );
}
